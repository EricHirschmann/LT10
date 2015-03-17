using System;
using System.Collections.Generic;
using Evel.interfaces;
using System.IO;
using System.Reflection;
using System.Xml;
using System.IO.Compression;
using System.Threading;

namespace Evel.engine {

    public abstract class SpectraContainerBase : ISpectraContainer {

        public const string EXP_LITERAL = "Experimental spectrum";
        public const string TH_LITERAL = "Theoretical spectrum";

        protected IModel _model;
        protected List<ISpectrum> _spectra;
        protected string _name;
        protected IProject _parentProject;
        protected bool _enabled;
        public int[] _data;
        public double[] _weights;

        public static event System.ComponentModel.ProgressChangedEventHandler OpenProgressChanged;
        public static event System.ComponentModel.ProgressChangedEventHandler SaveProgressChanged;

        public SpectraContainerBase(IProject parentProject, IModel model) {
            this._model = model;
            this._parentProject = parentProject;
            this._spectra = new List<ISpectrum>();
            this._enabled = true;
        }

        public SpectraContainerBase(IProject parentProject, string name, IModel model, ICollection<string> spectraPaths, ICollection<GroupDefinition> groupsDefinition) {
            int dataLength = 0;
            int bufferStart = 0;
            if (model.SpectrumType.GetInterface("ISpectrum")==null) {
                throw new SpectrumLoadException("invalid spectrumType. spectrumType must be implementation of ISpectrum");
            }
            this._name = name;
            this._parentProject = parentProject;
            this._enabled = true;
            Assembly ass = Assembly.GetAssembly(model.SpectrumType);
            model.GroupsDefinition = (GroupDefinition[])groupsDefinition;
            this._model = model;
            _spectra = new List<ISpectrum>();
            int stat;
            foreach (string path in spectraPaths) {
                using (TextReader reader = new StreamReader(path)) {
                    dataLength += SpectrumBase.getSpectrumData(reader, true, null, -1, out stat);
                }
            }
            this._data = new int[dataLength];
            this._weights = new double[dataLength];
            foreach (string path in spectraPaths) {
                string typeName = model.SpectrumType.ToString();
                BindingFlags bindingFlags = BindingFlags.CreateInstance;
                object[] args = new object[] { path, this, bufferStart };
                SpectrumBase spectrum = (SpectrumBase)ass.CreateInstance(typeName, true, bindingFlags, null, args, null, null);
                if (spectrum != null) {
                    _spectra.Add(spectrum);
                }
                //spectrum.getExperimentalSpectrum(
                bufferStart = spectrum.BufferEndPos + 1;
                //progress++;
                if (OpenProgressChanged != null) {
                    OpenProgressChanged(this, null); //new System.ComponentModel.ProgressChangedEventArgs((int)Math.Round(100 * ((double)progress / (double)spectraPaths.Count)), null));
                }
            }
        }

        public SpectraContainerBase(IProject parentProject, XmlReader reader, string modelDirectory) {
            this._parentProject = parentProject;
            ReadXml(reader, modelDirectory);
            InitializeBindings();
        }

        #region referencing methods

        private IParameter getParameter(ref ParameterLocation location) {
            if (location.compId == -1)
                return ((ContributedGroup)_spectra[location.specId].Parameters[location.groupId]).contribution;
            else
                return _spectra[location.specId].Parameters[location.groupId].Components[location.compId][location.parId];
        }

        private void createReferences(ref ParameterLocation location) {
            int i, j;
            int refGroup = 0;
            int parametersLeft = _spectra.Count;
            IParameter rp, p;
            ParameterLocation pl = location;
            while (parametersLeft > 0) {
                for (i = 0; i < Spectra.Count; i++) {
                    location.specId = i;
                    rp = getParameter(ref location);
                    //rp = Spectra[i].Parameters[groupId].Components[componentId][parameterId];
                    if ((rp.Status & ParameterStatus.Common | ParameterStatus.Free) == (ParameterStatus.Common | ParameterStatus.Free) &&
                       rp.ReferenceGroup == refGroup && !rp.HasReferenceValue) {
                        parametersLeft--;
                        for (j = i + 1; j < Spectra.Count; j++) {
                            pl.specId = j;
                            p = getParameter(ref pl);
                            //if ((p = Spectra[j].Parameters[groupId].Components[componentId][parameterId]).ReferenceGroup == refGroup &&
                            //    !p.HasReferenceValue) {
                            if (p.ReferenceGroup == refGroup && !p.HasReferenceValue) {
                                p.ReferencedParameter = rp;
                                parametersLeft--;
                            }
                        }
                    } else if (rp.ReferenceGroup >= refGroup &&
                       (rp.Status & ParameterStatus.Local) == ParameterStatus.Local)
                        parametersLeft--;
                }
                refGroup++;
            }
        }

        protected void InitializeBindings() {
            
            //int refGroup;
            //int parametersLeft;
            ParameterLocation l = default(ParameterLocation);
            //int groupId, componentId, parameterId;
            if (Spectra.Count > 1) {
                //ISpectrum firstSpectrum = Spectra[0];
                //IParameter rp, p;
                for (l.groupId = 0; l.groupId < Spectra[0].Parameters.GroupCount; l.groupId++) {
                    for (l.compId = 0; l.compId < Spectra[0].Parameters[l.groupId].Components.Size; l.compId++) {
                        //location.compId = componentId;
                        for (l.parId = 0; l.parId < Spectra[0].Parameters[l.groupId].Components[l.compId].Size; l.parId++) {
                            //location.parId = parameterId;
                            createReferences(ref l);
                            //refGroup = 0;
                            //parametersLeft = Spectra.Count;
                            //while (parametersLeft > 0) {
                            //    for (int i = 0; i < Spectra.Count; i++) {
                            //        rp = Spectra[i].Parameters[groupId].Components[componentId][parameterId];
                            //        if ((rp.Status & ParameterStatus.Common | ParameterStatus.Free) == (ParameterStatus.Common | ParameterStatus.Free) &&
                            //           rp.ReferenceGroup == refGroup && !rp.HasReferenceValue) {
                            //            parametersLeft--;
                            //            for (int j = i + 1; j < Spectra.Count; j++)
                            //                if ((p = Spectra[j].Parameters[groupId].Components[componentId][parameterId]).ReferenceGroup == refGroup &&
                            //                    !p.HasReferenceValue) {
                            //                    p.ReferencedParameter = rp;
                            //                    parametersLeft--;
                            //                }
                            //        } else if (rp.ReferenceGroup >= refGroup &&
                            //           (rp.Status & ParameterStatus.Local) == ParameterStatus.Local)
                            //            parametersLeft--;
                            //    }
                            //    refGroup++;
                            //}
                        }
                    }
                    if (Spectra[0].Parameters[l.groupId] is ContributedGroup) {
                        l.compId = -1;
                        createReferences(ref l);
                        //for (int spectrumId = Spectra.Count - 1; spectrumId > 0; spectrumId--) {
                        //    ((ContributedGroup)Spectra[spectrumId].Parameters[groupId]).contribution.ReferencedParameter = ((ContributedGroup)Spectra[0].Parameters[groupId]).contribution;
                        //}
                    }
                }
            }
        }

        #endregion referencing methods

        protected void ReadXml(XmlReader reader, string modelDirectory) {
            int dataBufferPos = 0;
            object[] args = new object[4];
            args[2] = this;
            //reader.Read(); //declaration
            //reader.Read(); //root --> spectra node
            if (!reader.ReadToFollowing("spectra")) return;
            //XmlDocument doc = new XmlDocument();
            //doc.Load(filePath);
            //this._name = doc.DocumentElement.Attributes["name"].Value;
            string modelClassName = "";
            this._enabled = true;
            this._data = null;
            while (reader.MoveToNextAttribute()) {
                switch (reader.Name) {
                    case "name": this._name = reader.Value; break;
                    case "class": modelClassName = reader.Value; break;
                    case "enabled": this._enabled = Convert.ToBoolean(reader.Value); break;
                    case "dataLength":
                        this._data = new int[int.Parse(reader.Value)];
                        this._weights = new double[this._data.Length];
                        break;
                }
            }
            if (this._data == null) throw new SpectrumLoadException("Project files are damaged or this version is no more supported (missing dataLength node)");
            //string modelClassName = doc.DocumentElement.Attributes["class"].Value; //doc.GetElementsByTagName("modelclass")[0].Attributes["name"].Value;
            this._model = AvailableAssemblies.getModel(modelClassName);
            if (this._model.SpectrumType.GetInterface("ISpectrum") == null) {
                throw new SpectrumLoadException("invalid spectrumType. spectrumType must be implementation of ISpectrum");
            }
            Assembly ass = Assembly.GetAssembly(this._model.SpectrumType);
            _spectra = new List<ISpectrum>();
            string typeName = this._model.SpectrumType.ToString();
            //int progress = 0;
            //int spectraCount = Directory.GetFiles(Path.Combine(Path.GetDirectoryName(filePath), "spectra")).Length;
            while (reader.Read()) {  //spectrum
                if (reader.Name == "spectrum") {
                    XmlReader spectrumReader = reader.ReadSubtree();
                    BindingFlags bindingFlags = BindingFlags.CreateInstance;
                    // = new object[] { spectrumReader, modelDirectory, this, dataBufferPos };
                    args[0] = spectrumReader;
                    args[1] = modelDirectory;
                    args[3] = dataBufferPos;
                    SpectrumBase spectrum = (SpectrumBase)ass.CreateInstance(typeName, true, bindingFlags, null, args, null, null);
                    if (spectrum != null) {
                        _spectra.Add(spectrum);
                    }
                    dataBufferPos = spectrum.BufferEndPos + 1;
                } else {
                    break;
                }
                if (OpenProgressChanged != null)
                    OpenProgressChanged(this, null); //new System.ComponentModel.ProgressChangedEventArgs((int)Math.Round(100 * ((double)++progress / (double)Spectra.Count)), parentProject));
            }
        }

        //public void ImportParameters(XmlReader reader, List<GroupDefinition> definitions) {
        //    int i;
        //    int currentSpectrum = 0;
        //    List<IGroup> groups = new List<IGroup>();
        //    while (reader.ReadToFollowing("spectrum")) {
        //        if (reader.ReadToFollowing("ps")) {
        //            groups.Clear();
        //            for (i = 0; i < _spectra[currentSpectrum].Parameters.GroupCount; i++)
        //                if (definitions.Contains(_spectra[currentSpectrum].Parameters[i].Definition))
        //                    groups.Add(_spectra[currentSpectrum].Parameters[i]);
        //            _spectra[currentSpectrum].getGroup(reader.ReadSubtree(), groups);
        //        }

        //    }
        //}

        /// <summary>
        /// Resizes data buffer, fills it with data and returns position next to last filled
        /// </summary>
        /// <param name="newSize"></param>
        /// <returns></returns>
        public int ResizeBuffer(int newSize) {
            int[] newBuffer;
            if (this._data.Length != newSize) {
                newBuffer = new int[newSize];
            } else {
                newBuffer = this._data;
            }

            int currentBufferPos = 0, s, ch;
            for (s = 0; s < this._spectra.Count; s++) {
                for (ch = 0; ch < this._spectra[s].DataLength; ch++)
                    newBuffer[currentBufferPos + ch] = this._data[this._spectra[s].BufferStartPos + ch];
                this._spectra[s].BufferStartPos = currentBufferPos;
                currentBufferPos = this._spectra[s].BufferStartPos + this._spectra[s].DataLength;
                this._spectra[s].BufferEndPos = currentBufferPos - 1;
            }
            if (newBuffer != this._data)
                this._data = newBuffer;
            return currentBufferPos;
            //StreamReader reader;
            //for (s = 0; s < this._spectra.Count; s++) {
            //    this._spectra[s].BufferStartPos = i;
            //    if (this._spectra[s].Path != null) {
            //        using (reader = new StreamReader(this._spectra[s].Path))
            //            i += SpectrumBase.getSpectrumData(reader, true, this._data, i);
            //    } else
            //        throw new NotImplementedException("This feature will be implemented soon");
            //    this._spectra[s].BufferEndPos = i - 1;
            //    this._spectra[s].setThresholds();
            //}
            //return i;
        }

        //public virtual void Save(string filePath, bool includeData, bool compressed) {
        public virtual void Save(XmlWriter writer, ProjectFileType fileType) {
            //Stream stream;
            //if (compressed)
            //    stream = new GZipStream(new FileStream(filePath, FileMode.Create), CompressionMode.Compress);
            //else
            //    stream = new FileStream(filePath, FileMode.Create);
            //try {
            //    using (XmlWriter writer = XmlWriter.Create(stream)) {
                    writer.WriteStartElement("spectra");
                    writer.WriteAttributeString("name", this._name);
                    writer.WriteAttributeString("class", this._model.DeltaFileName);
                    writer.WriteAttributeString("enabled", this._enabled.ToString());
                    writer.WriteAttributeString("dataLength", this._data.Length.ToString());
                    foreach (ISpectrum spectrum in this._spectra) {
                        spectrum.writeToXml(writer, fileType == ProjectFileType.CompressedPack, fileType == ProjectFileType.CompressedES);
                        if (SaveProgressChanged != null)
                            SaveProgressChanged(this, null); //new System.ComponentModel.ProgressChangedEventArgs(this._spectra.IndexOf(spectrum), null));
                    }
                    writer.WriteEndElement(); //spectra
                    writer.Flush();
                    //writer.Close();
            //    }
            //} finally {
            //    stream.Close();
            //}
        }

        public virtual List<IParameter> getParameters(ParameterStatus status, bool[] includeFlags, CheckOptions co) {
            List<IParameter> result = new List<IParameter>();
            foreach (ISpectrum spectrum in Spectra) {
                result.AddRange(Model.getParameters(status, spectrum, includeFlags, co));
            }
            return result;
        }

        #region ISpectraContainer Members

        public abstract void ResetArrays();

        public int[] Data {
            get { return this._data; }
        }

        public double[] Weights {
            get { return this._weights; }
        }

        public IProject ParentProject {
            get { return this._parentProject; }
        }

        public string Name {
            get { return this._name; }
            set { this._name = value; }
        }

        public bool Enabled {
            get { return this._enabled; }
            set { this._enabled = value; }
        }

        public IModel Model {
            get { return this._model; }
            set {
                this._model = value;
                int s, g;
                for (s = 0; s < this._spectra.Count; s++)
                    for (g = 0; g < this._spectra[s].Parameters.GroupCount && g < value.GroupsDefinition.Length; g++)
                        this._spectra[s].Parameters[g].Definition = value.GroupsDefinition[g];
            }
        }

        public List<ISpectrum> Spectra {
            get { return this._spectra; }
        }

        /// <summary>
        /// When implemented in deriving class, builds theoretical spectrum.
        /// </summary>
        /// <param name="spectrum">Spectrum object for which theoretical spectrum is going to be build</param>
        /// <returns>theoretical spectrum held in double[] array</returns>
        public virtual double[] getTheoreticalSpectrum(ISpectrum spectrum) {
            throw new NotImplementedException();
        }

        /// <summary>
        /// When implemented in deriving class, builds array of differences between theoretical and experimental spectra
        /// </summary>
        /// <param name="object">Spectrum object for which theoretical spectrum is going to be build</param>
        /// <returns>differences between theory and experience</returns>
        public virtual bool getEvaluationArray(object target, double[] diffs) {
            throw new NotImplementedException();
        }

        //public virtual void getTheoreticalSpectrum(ISpectrum spectrum, ref double[] theoreticalCurve, ref double[] differences, bool intensitiesFromSearch) {
        //public virtual void getTheoreticalSpectrum(ISpectrum spectrum, ref Dictionary<string, double[]> theoreticalCurves, ref double[] differences, bool intensitiesFromSearch) {
        public virtual void getTheoreticalSpectrum(ISpectrum spectrum, ref float[][] curves, ref string[] curveNames, ref float[] differences, bool intensitiesFromSearch) {
            throw new NotImplementedException();
        }

        /// <summary>
        /// When implemented in deriving class, creates instance of spectrum which type corresponds to this container type
        /// i.e. AnhSpectraContainer creates AnhSpectrum
        /// </summary>
        /// <returns>Spectrum instance</returns>
        public virtual ISpectrum CreateSpectrum(System.Xml.XmlReader spectrumReader, int bufferStart) {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Implemented in inheriting classes
        /// </summary>
        /// <param name="path"></param>
        /// <returns></returns>
        public virtual ISpectrum CreateSpectrum(string path, int bufferStart) {
            throw new NotImplementedException();
        }

        public virtual void AddSpectrum(ISpectrum spectrum, CopyOptions copyOpts) {
            ISpectrum lastSpectrum = Spectra.Count > 0 ? Spectra[0] : null;
            if (lastSpectrum != null) {
                Spectra.Add(spectrum);
                for (int i = 1; i < lastSpectrum.Parameters.GroupCount; i++) {
                    //CopyOptions options = CopyOptions.Status | CopyOptions.Value;
                    if (spectrum.Parameters[i].Definition.SetDefaultComponents != null && (copyOpts & CopyOptions.Value) == 0) {
                        spectrum.Parameters[i].Definition.SetDefaultComponents(spectrum.Parameters[i], spectrum, new EventArgs());
                        //options &= ~CopyOptions.Value;
                    }
                    spectrum.copy(lastSpectrum, lastSpectrum.Parameters[i].Definition.name, copyOpts);
                }
                
            }
        }


        /// <summary>
        /// Updates references for common parameters
        /// </summary>
        //public virtual void refreshReferences() {
            //ISpectrum topSpectrum = Spectra[0];
            //for (int groupId = 0; groupId < topSpectrum.Parameters.GroupCount; groupId++) {
            //    if (topSpectrum.Parameters[groupId] is ContributedGroup) {
            //        IParameter topContribution = ((ContributedGroup)topSpectrum.Parameters[groupId]).contribution;
            //        foreach (ISpectrum spectrum in Spectra) {
            //            if (spectrum == topSpectrum) {
            //                topContribution.ReferencedParameter = null;
            //            } else {
            //                IParameter contribution = ((ContributedGroup)spectrum.Parameters[groupId]).contribution;
            //                if ((contribution.Status & ParameterStatus.Common) == ParameterStatus.Common)
            //                    contribution.ReferencedParameter = topContribution;
            //            }

            //        }
            //    }
            //    for (int componentId = 0; componentId < topSpectrum.Parameters[groupId].Components.Size; componentId++) {
            //        for (int parameterId = 0; parameterId < topSpectrum.Parameters[groupId].Components[componentId].Size; parameterId++) {
            //            IParameter topParameter = topSpectrum.Parameters[groupId].Components[componentId][parameterId];
            //            foreach (ISpectrum spectrum in Spectra) {

            //                if (spectrum == topSpectrum) {
            //                    topParameter.ReferencedParameter = null;
            //                } else {
            //                    IParameter parameter = spectrum.Parameters[groupId].Components[componentId][parameterId];
            //                    if ((parameter.Status & ParameterStatus.Common) == ParameterStatus.Common)
            //                        parameter.ReferencedParameter = topParameter;
            //                }

            //            }
            //        }
            //    }
            //}
        //}

        public virtual void RemoveSpectrum(ISpectrum spectrum) {
            Spectra.Remove(spectrum);
            spectrum.Dispose();
            //refreshReferences();
        }

        public List<string> getinfos() {
            List<string> result = new List<string>();
            //for (int groupId = 0; groupId < Spectra[0].Parameters.GroupCount; groupId++) {
            for (int g = 0; g < Spectra[0].Parameters.GroupCount; g++) {
                //headers
                string line = "\t\t";
                result.Add(String.Format("-----{0}-----", Spectra[0].Parameters[g].Definition.name.ToUpper()));
                int compId = 1;
                foreach (IComponent component in Spectra[0].Parameters[g].Components) {
                    foreach (IParameter parameter in component) {
                        string n = parameter.Definition.Name;
                        if (n.Length>5)
                            n =  n.Substring(0, 5);
                        line += String.Format("{0}{1}\t\t\t", n, compId);
                    }
                    compId++;
                }
                result.Add(line);
                //statuses
                line = "\t\t";
                foreach (IComponent component in Spectra[0].Parameters[g].Components) {
                    foreach (IParameter parameter in component) {
                        line += String.Format("{0}\t\t", parameter.Status);
                    }
                    
                }
                result.Add(line);
                //values
                foreach (ISpectrum spectrum in Spectra) {
                    line = String.Format("{0}\t\t", spectrum.Name);
                    foreach (IComponent component in spectrum.Parameters[g].Components) {
                        foreach (IParameter parameter in component) {
                            line += String.Format("{0:F04}±{1:E01}({2})\t", parameter.Value, parameter.Error, (int)parameter.Status);
                        }
                    }
                    result.Add(line);
                }             
                ////deltas
                //foreach (ISpectrum spectrum in Spectra) {
                //    line = String.Format("\t\t\t\t", spectrum.Name);
                //    foreach (IComponent component in spectrum.Parameters[g].Components) {
                //        foreach (IParameter parameter in component) {
                //            line += String.Format("{0:F5}\t", parameter.Delta);
                //        }
                //    }
                //    result.Add(line);
                //}
                
            }
            return result;
        }

        public IParameter GetParameter(string address)
        {
                string[] coords = address.Split(ProjectBase.AddressDelimiters, StringSplitOptions.RemoveEmptyEntries);
                int spectrumId = Int32.Parse(coords[1].Substring(1))-1;
                int groupId = Int32.Parse(coords[2].Substring(1));
                return Spectra[spectrumId].Parameters[groupId].GetParameter(address);
        }


        public string GetParameterAddress(IParameter parameter) {
            string spectrumId = "";
            IGroup group;

            if (parameter.Parent is IComponent) {
                IComponent component = (IComponent)parameter.Parent;
                if (component.Parent is IComponents)
                    group = ((IComponents)component.Parent).Parent;
                else
                    group = (IGroup)component.Parent;
            } else {
                group = (IGroup)parameter.Parent;
            }
            int s = 0;
            while (spectrumId == "" && s < Spectra.Count) {
                foreach (IGroup gr in Spectra[s].Parameters)
                    if (gr == group) {
                        spectrumId = (s + 1).ToString();
                        break;
                    }
                s++;
            }
            return String.Format("s{0};g{1};{2}", spectrumId, Spectra[s-1].Parameters.IndexOf(group), group.GetParameterAddress(parameter));
        }

        public IEnumerable<IParameter> GetParameters(IParameter parameter, bool sameReferenceGroup) {
            ParameterLocation pl = ParentProject.GetParameterLocation(parameter);
            IParameter p;
            for (int s = 0; s<Spectra.Count; s++) {
                pl.specId = s;
                if ((p = ParentProject.GetParameter(pl)).ReferenceGroup == parameter.ReferenceGroup || !sameReferenceGroup)
                yield return p;
            }
        }

        #endregion

        public override string ToString() {
            return this._name;
        }
    }
}
