using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using Evel.interfaces;
using System.IO;
using System.Xml;

namespace Evel.engine {

    public abstract class SpectrumBase : ISpectrum {


        //protected int[] _experimentalSpectrum;
        //protected double[] _weights;

        protected string _name;
        protected string _path;
        protected string _title;
        protected double _fit = double.PositiveInfinity;
        protected IParameterSet _parameters;
        //private IModel _model;
        protected ISpectraContainer _container;
        protected double[] _constants;
        //protected int _effectEndChannel;
        protected int _rangeArea;
        protected int _statistic;
        protected int _dataLength;
        public int _dataBufferStart, _dataBufferStop;
        public int[] _thresholds;
        protected int _thresholdsCompression; //zysk w ilości kanałów przy wyznaczaniu tablicy różnic
        //private static System.Globalization.NumberFormatInfo numberFormatInfo = null;

        #region ISpectrum Members

        //public IModel Model {
        //    get { return this._model; }
        //}

        protected void Initialize() {
            this._parameters["ranges"].Components[0]["stop"].OnValueChange += new EventHandler(SpectrumBase_OnValueChange);
        }

        void SpectrumBase_OnValueChange(object sender, EventArgs e) {
            if (this._parameters["ranges"].Components[0]["stop"].Value >= this._dataLength)
                this._parameters["ranges"].Components[0]["stop"].Value = this._dataLength - 2;
            setThresholdsCompression();
        }

        public double[] Constants {
            get { return this._constants; }
        }

        public ISpectraContainer Container {
            get { return this._container; }
        }

        public int RangeArea {
            get {
                return _rangeArea;
            }
        }

        public int Statistic {
            get {
                return this._statistic;
            }
        }

        public int ThresholdsCompression {
            get {
                return this._thresholdsCompression;
            }
        }

        public int DataLength {
            get { return this._dataLength; }
        }

        public int[] Thresholds {
            get { return this._thresholds; }
        }

        //private delegate double PDelegate(double counts);
        //private delegate double SDelegate(int channel);

        //public int[] ExperimentalSpectrum {
        //    get { return this._experimentalSpectrum; }
        //    set {
        //        this._experimentalSpectrum = value;
        //        this._weights = new double[_experimentalSpectrum.Length];
        //        calculateWeigths();
        //    }
        //}

        //private void calculateWeigths() {
        //    int i, j;
        //    double sigma, avg;
        //    double sigmai;
        //    double px = 1;
        //    avg = sigma = sigmai = 0;
        //    for (j = _experimentalSpectrum.Length - 1; j >= _experimentalSpectrum.Length - 100; j--)
        //        avg += _experimentalSpectrum[j];
        //    avg /= 100;
        //    for (j = _experimentalSpectrum.Length - 1; j >= _experimentalSpectrum.Length - 100; j--)
        //        sigma += (_experimentalSpectrum[j] - avg) * (_experimentalSpectrum[j] - avg);
        //    sigma = Math.Sqrt(0.01 * sigma);

        //    for (i = _experimentalSpectrum.Length - 1; i > 0; i--) {
        //        if (i > 100) {
        //            avg = avg - 0.01 * (_experimentalSpectrum[i] - _experimentalSpectrum[i - 100]);
        //            sigmai = 0;
        //            for (j = i; j >= i - 99; j--)
        //                sigmai += (_experimentalSpectrum[j] - avg) * (_experimentalSpectrum[j] - avg);
        //            sigmai = Math.Sqrt(0.01 * sigmai);
        //            px = -sigma / sigmai + 1;
        //            //px = px * px * px * px * px * px * px * px * px * px;
        //            px = Math.Abs(px);
        //        }
                
        //        _weights[i] = px * (Math.Sqrt(_experimentalSpectrum[i]) - sigmai) + sigmai;
        //    }
        //}

        //public double[] Weights {
        //    get { return this._weights; }
        //}

        public string Name {
            get { return this._name; }
            set { this._name = value; }
        }

        public IParameterSet Parameters {
            get { return this._parameters; }
            set { this._parameters = value; }
        }

        public string Path {
            get { return this._path; }
            set { this._path = value; }
        }

        public string Title {
            get { return this._title; }
            set { this._title = value; }
        }

        public double Fit {
            get { return this._fit; }
            set { this._fit = value; }
        }

        public abstract void prepareToSearch(SearchLevel sl, PrepareOptions po);

        public abstract void normalizeAfterSearch(SearchLevel sl, PrepareOptions po, bool flagOnly);

        public virtual void writeData(string path) {
            using (TextWriter writer = new StreamWriter(path)) {
                int i,j;
                writer.WriteLine(this._title);
                for (i = 0; i < this._constants.Length; i++)
                    writer.WriteLine(this._constants[i]);
                j=1;
                for (i = this._dataBufferStart; i <= this._dataBufferStop; i++, j++) {
                    writer.Write("{0,15}", this.Container.Data[i]-1);
                    if (j == 5) {
                        j = 0;
                        writer.WriteLine();
                    }
                }
            }
        }

        public virtual void writeToXml(XmlWriter writer, bool includeData, bool fullPath) {
            writer.WriteStartElement("spectrum");
            writer.WriteAttributeString("name", this._name);
            if (!double.IsNaN(this._fit) && !double.IsInfinity(this._fit))
                writer.WriteAttributeString("fit", this._fit.ToString());
            writer.WriteStartElement("constants");
            writer.WriteAttributeString("count", this._constants.Length.ToString());
            for (int i = 0; i < this._constants.Length; i++)
                writer.WriteAttributeString(String.Format("v{0}", i + 1), this._constants[i].ToString());
            writer.WriteEndElement(); //constants

            writer.WriteStartElement("ps");
            foreach (IGroup group in this._parameters) {
                writeGroupNode(writer, group);
            }
            writer.WriteEndElement(); //parameters
            writer.WriteStartElement("data");
            writer.WriteAttributeString("length", this._dataLength.ToString());
            if (includeData) {
                writer.WriteAttributeString("type", "data");
                //writer.WriteString(setExperimentalSpectrum());
                writeExperimentalSpectrum(writer);
            } else {
                writer.WriteAttributeString("type", "file");
                if (fullPath)
                    writer.WriteString(this._path);
                else if (this._path != null)
                    writer.WriteString(String.Format("spectra\\{0}", System.IO.Path.GetFileName(this._path)));
                else
                    writer.WriteString(String.Format("spectra\\{0}.txt", this._name));
            }
            writer.WriteEndElement(); //data
            writer.WriteEndElement(); //spectrum
        }

        public void copy(IParameterSet parameters, GroupDefinition definition, CopyOptions options) {
            int g, rg, c, p;
            for (g = 0; g < Parameters.GroupCount; g++) {
                if (Parameters[g].Definition == definition) {
                rg = 0;
                while (rg < parameters.GroupCount)
                    if (parameters[rg].Definition != definition) rg++;
                    else break;

                //if (Parameters[g].Definition == parameters[rg].Definition) {
                    if (Parameters[g] is ContributedGroup && parameters[rg] is ContributedGroup) {
                        IParameter contribution = ((ContributedGroup)Parameters[g]).contribution;
                        if ((options & CopyOptions.Value) > 0)
                            contribution.Value = ((ContributedGroup)parameters[rg]).contribution.Value;
                        if ((options & CopyOptions.Status) > 0)
                            contribution.Status = ((ContributedGroup)parameters[rg]).contribution.Status;
                        if ((options & CopyOptions.ReferenceGroup) > 0)
                            contribution.ReferenceGroup = ((ContributedGroup)parameters[rg]).contribution.ReferenceGroup;

                    }
                    if (Parameters[g].Components.Size != parameters[rg].Components.Size)
                        Parameters[g].Components.Size = parameters[rg].Components.Size;
                    for (c = 0; c < Parameters[g].Components.Size; c++) {
                        for (p = 0; p < Parameters[g].Components[c].Size; p++) {
                            if ((options & CopyOptions.Value) > 0)
                                Parameters[g].Components[c][p].Value = parameters[rg].Components[c][p].Value;
                            if ((options & CopyOptions.Status) > 0)
                                Parameters[g].Components[c][p].Status = parameters[rg].Components[c][p].Status;
                            if ((options & CopyOptions.ReferenceGroup) > 0)
                                Parameters[g].Components[c][p].ReferenceGroup = parameters[rg].Components[c][p].ReferenceGroup;
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Performs copy of values from parameters if 
        /// and only if parameters has the same parameter 
        /// names and the same component counts in each group
        /// </summary>
        /// <param name="parameters">new parameters that will be copied</param>       
        public void copy(IParameterSet parameters, ParameterStatus status, bool copyAll) {
            try {
                int g, c, p;
                for (g = 1; g < Parameters.GroupCount; g++) {
                    if (Parameters[g].Definition == parameters[g].Definition) {
                        if (Parameters[g] is ContributedGroup && parameters[g] is ContributedGroup) {
                            IParameter contribution = ((ContributedGroup)Parameters[g]).contribution;
                            ((ContributedGroup)Parameters[g]).MemoryInt = ((ContributedGroup)parameters[g]).MemoryInt;
                            if ((contribution.Status & status) == status && copyAll) {
                                contribution.Value = ((ContributedGroup)parameters[g]).contribution.Value;
                            }

                        }
                        if (copyAll && Parameters[g].Components.Size == parameters[g].Components.Size) {
                            for (c = 0; c < Parameters[g].Components.Size; c++) {
                                for (p = 0; p < Parameters[g].Components[c].Size; p++) {
                                    if ((Parameters[g].Components[c][p].Status & status) == status) {
                                        Parameters[g].Components[c][p].Value = parameters[g].Components[c][p].Value;
                                    }
                                }
                            }
                        }
                    }
                }
            } catch {
                throw new ArgumentException("Spectrum differs from destination spectrum that is about to copy");
            }
        }

        /// <summary>
        /// Resizes components in group groupName
        /// </summary>
        /// <param name="source"></param>
        /// <param name="groupName"></param>
        public void copy(ISpectrum source, string groupName, CopyOptions options) {
            if (!this.Parameters.ContainsGroup(groupName) || !source.Parameters.ContainsGroup(groupName))
                return;
            IGroup thisGroup = this.Parameters[groupName];
            IGroup srcGroup = source.Parameters[groupName];
            if ((thisGroup is ContributedGroup) && (srcGroup is ContributedGroup)) {
                IParameter thisContribution = ((ContributedGroup)thisGroup).contribution;
                IParameter srcContribution = ((ContributedGroup)srcGroup).contribution;
                if ((options & CopyOptions.Value) == CopyOptions.Value)
                    thisContribution.Value = srcContribution.Value;
                if ((options & CopyOptions.Status) == CopyOptions.Status) {
                    thisContribution.Status = srcContribution.Status;
                    if ((srcContribution.Status & ParameterStatus.Common) == ParameterStatus.Common) {
                        if (srcContribution.ReferencedParameter == null)
                            thisContribution.ReferencedParameter = srcContribution;
                        else
                            thisContribution.ReferencedParameter = srcContribution.ReferencedParameter;
                    }
                }

            }
            thisGroup.Components.Size = srcGroup.Components.Size;
            for (int compId = 0; compId < thisGroup.Components.Size; compId++)
                for (int parameterId = 0; parameterId < thisGroup.Components[compId].Size; parameterId++) {
                    if ((options & CopyOptions.Value) == CopyOptions.Value)
                        thisGroup.Components[compId][parameterId].Value = srcGroup.Components[compId][parameterId].Value;
                    if ((options & CopyOptions.Status) == CopyOptions.Status) {
                        thisGroup.Components[compId][parameterId].Status = srcGroup.Components[compId][parameterId].Status;
                        if ((srcGroup.Components[compId][parameterId].Status & ParameterStatus.Common) == ParameterStatus.Common) {
                            if (srcGroup.Components[compId][parameterId].ReferencedParameter == null)
                                thisGroup.Components[compId][parameterId].ReferencedParameter = srcGroup.Components[compId][parameterId];
                            else
                                thisGroup.Components[compId][parameterId].ReferencedParameter = srcGroup.Components[compId][parameterId].ReferencedParameter;
                        }
                    }


                }
        }

        /// <summary>
        /// Performs copy of values defined in spectrumElement
        /// </summary>
        public void copy(XmlReader reader, ParameterStatus status) {
            //reader.Read();
            //reader.ReadToFollowing("group");
            IGroup group = null;
            while (reader.Read()) {
                switch (reader.Name) {
                    case "group":
                        while (reader.MoveToNextAttribute()) {
                            switch (reader.Name) {
                                case "name": group = Parameters[reader.Value]; break;
                                case "area": ((ContributedGroup)group).groupArea = Double.Parse(reader.Value); break;
                            }
                        }
                        reader.MoveToElement();
                        break;
                    case "contribution":
                        IParameter contribution = ((ContributedGroup)group).contribution;
                        if ((contribution.Status & status) == status) {
                            contribution.Value = Double.Parse(reader.Value);
                        }
                        break;
                    case "components":
                        int componentId = 0;
                        reader.ReadToDescendant("component");
                        do {
                            reader.ReadToDescendant("parameter");
                            int parameterId = 0;
                            do {
                                double parameterValue = group.Components[componentId][parameterId].Value;
                                ParameterStatus parameterStatus = 0;
                                while (reader.MoveToNextAttribute()) {
                                    switch (reader.Name) {
                                        case "value": parameterValue = Double.Parse(reader.Value); break;
                                        case "status": parameterStatus = (ParameterStatus)(Int32.Parse(reader.Value)); break;
                                    }
                                }
                                reader.MoveToElement();
                                if ((status & parameterStatus) == status)
                                    group.Components[componentId][parameterId].Value = parameterValue;
                                parameterId++;
                            } while (reader.ReadToNextSibling("parameter"));
                            componentId++;
                        } while (reader.ReadToNextSibling("component"));
                        break;
                }
            }
        }

        //protected double regressionAngle(int startChannel, int channelCount) {
        //    double xx, xy, x, y;
        //    xx = xy = x = y = 0;
        //    for (int i = startChannel; i < startChannel + channelCount && i < _experimentalSpectrum.Length; i++) {
        //        xx += i * i;
        //        x += i;
        //        xy += i * _experimentalSpectrum[i];
        //        y += _experimentalSpectrum[i];
        //    }
        //    return (channelCount * xy - x * y) / (channelCount * xx - x * x);
        //}

        //protected void setEffectEndChannel() {
        //    //find maximum from wich search will begin
        //    int channel = 0;
        //    int channelWithMax = 0;
        //    for (channel = 0; channel < _experimentalSpectrum.Length - 1; channel++)
        //        if (_experimentalSpectrum[channelWithMax] < _experimentalSpectrum[channel])
        //            channelWithMax = channel;
        //    channel = channelWithMax;
        //    int stop = _experimentalSpectrum.Length - 1;
        //    int plainLength = (int)(_experimentalSpectrum.Length / 80);
        //    while (Math.Abs(regressionAngle(channel, plainLength)) > 1e-3 && channel < stop)
        //        channel++;
        //    _effectEndChannel = channel;
        //}

        //public int EffectEndChannel {
        //    get {
        //        return this._effectEndChannel;
        //    }
        //}

        //public virtual IEnumerable<IParameter> getParameters(ParameterStatus status) {
        //    foreach (IGroup group in Parameters)
        //        foreach (IComponent component in group.Components)
        //            foreach (IParameter parameter in component)
        //                if ((parameter.Status & status) == status)
        //                    yield return parameter;
        //}

        #region marquardt fit minimalization

        public int BufferStartPos {
            get { return _dataBufferStart; }
            set { this._dataBufferStart = value; }
        }

        public int BufferEndPos {
            get { return _dataBufferStop; }
            set { this._dataBufferStop = value; }
        }

        #endregion

        #endregion ISpectrum Members

        //protected virtual string setExperimentalSpectrum() {
        //    StringBuilder result = new StringBuilder(this._title + "\r\n");
        //    int i;
        //    for (i = 0; i < this._constants.Length; i++)
        //        result.AppendFormat("{0}\r\n", this._constants[i]);
        //    //foreach (IGroup group in this._parameters) {
        //    //    try {
        //    //        string bs = group.Components[0][0].Value.ToString();
        //    //        string keyValue = group.Components[0]["key value"].Value.ToString();
        //    //        result.AppendFormat("{0}\r\n{1}\r\n", bs, keyValue);
        //    //        break;
        //    //    } catch { }
        //    //}
        //    //for (int i = 1; i < this.ExperimentalSpectrum.Length; i++)
        //    //    result += String.Format("{0}\t", this.ExperimentalSpectrum[i] - 1);
        //    for (i=this._dataBufferStart; i<=this._dataBufferStop; i++)
        //        result.AppendFormat("{0}\t", _container.Data[i] - 1);
        //    return result.ToString();
        //}

        protected virtual void writeExperimentalSpectrum(XmlWriter writer) {
            StringBuilder result = new StringBuilder(this._title + "\r\n");
            int i,j;
            for (i = 0; i < this._constants.Length; i++)
                result.AppendFormat("{0}\r\n", this._constants[i]);
            //foreach (IGroup group in this._parameters) {
            //    try {
            //        string bs = group.Components[0][0].Value.ToString();
            //        string keyValue = group.Components[0]["key value"].Value.ToString();
            //        result.AppendFormat("{0}\r\n{1}\r\n", bs, keyValue);
            //        break;
            //    } catch { }
            //}
            //for (int i = 1; i < this.ExperimentalSpectrum.Length; i++)
            //    result += String.Format("{0}\t", this.ExperimentalSpectrum[i] - 1);
            j = 1;
            for (i = this._dataBufferStart; i <= this._dataBufferStop; i++, j++) {
                result.AppendFormat("{0}\t", _container.Data[i] - 1);
                if (j == 10) {
                    j = 0;
                    result.AppendLine();
                }
            }
            writer.WriteString(result.ToString());
        }

        public void setThresholds() {
            const int level = 100;
            //double level;
            int i = 0, j = this._dataBufferStart, sum;
            List<int> thresholds = new List<int>();
            //find max
            for (i = this._dataBufferStart + 1; i <= this._dataBufferStop; i++)
                if (this._container.Data[i] > this._container.Data[j])
                    j = i;
            //level = 0.0015 * (double)this._container.Data[j];
            for (i = j; i <= this._dataBufferStop; ) {
                if (thresholds.Count > 0) //dopóki liczba zliczeń jest wyższa niż LEVEL nie zapamiętuj indeksów
                    thresholds.Add(i);
                if (this._container.Data[i] < level) {
                    if (thresholds.Count == 0) //tylko jeśli do tej pory nie zapamiętano żadnego indeksu
                        thresholds.Add(i);
                    sum = 0;
                    do {
                        sum += this._container.Data[i];
                        i++;
                    } while (sum < level && i <= this._dataBufferStop);
                } else
                    i++;
                if (thresholds.Count > 0)
                    thresholds.Add(i);
            }
            thresholds.Add(this._dataBufferStop + 1);
            this._thresholds = new int[thresholds.Count];
            thresholds.CopyTo(this._thresholds);
            setThresholdsCompression();
        }

        protected void setThresholdsCompression() {
            int i, j;
            int stop = (int)this._parameters["ranges"].Components[0]["stop"].Value;
            this._thresholdsCompression = 0;
            for (i = 0; i < _thresholds.Length - 1; i += 2)
                if ((j = _thresholds[i]) <  this._dataBufferStart + stop) {
                    j++;
                    for (; j < _thresholds[i + 1] && j < this._dataBufferStart + stop; j++)
                        this._thresholdsCompression++;
                } else break;
        }

        /// <summary>
        /// Analyses data from stream holding spectrum data and additional lines with title and constants. If buffer is not null data is copied into the buffer
        /// </summary>
        /// <param name="reader">Stream reader with spectrum data</param>
        /// <param name="buffer">Array to be hold the spectrum counts or null if only data length is needed</param>
        /// <param name="skipToData">true if jump to data by reading first 4 lines. false if reader is already at correct position in stream</param>
        /// <returns>Data length</returns>
        public static int getSpectrumData(TextReader reader, bool skipToData, int[] buffer, int bufferStart, out int statistic) {
            int count = 0;
            int chint;
            char ch;
            bool number = false;
            byte numbPos = 0;
            bool delimiterFound = false;
            statistic = 0;
            if (skipToData)
                for (int i = 0; i < 4; i++)
                    reader.ReadLine();
            while ((chint = reader.Read()) != -1) {
                ch = (char)chint;
                if (Char.IsDigit((char)ch)) {
                    number = true;
                    if (buffer != null) {
                        if (numbPos++ == 0)
                            buffer[bufferStart + count] = 0;
                        if (!delimiterFound) {
                            buffer[bufferStart + count] *= 10;
                            buffer[bufferStart + count] += Convert.ToInt32(ch.ToString());
                        }
                    }
                } else if (ch == '.') {
                    number = true;
                    delimiterFound = true;
                } else {
                    if (number) {
                        if (buffer != null) {
                            buffer[bufferStart + count]++; //ensure that there is no channel with 0 counts
                            statistic += buffer[bufferStart + count];
                        }
                        count++;
                    }
                    numbPos = 0;
                    number = false;
                    delimiterFound = false;
                }
            }
            if (number) {
                if (buffer != null) {
                    buffer[bufferStart + count]++;
                    statistic += buffer[bufferStart + count];
                }
                count++;
            }
            return count;
        }

        public static void getSpectrumHeader(TextReader reader, out double[] constants, out string title) {
            int i;
            string line;
            title = reader.ReadLine();
            
            constants = new double[3];
            for (i = 0; (i < 3) && (line = reader.ReadLine()) != null; i++)
                constants[i] = double.Parse(line);
        }

        public static int getExperimentalSpectrum(TextReader reader, out double[] constants, out string title, int[] buffer, int bufferStart, out int statistic) {
            getSpectrumHeader(reader, out constants, out title);
            return getSpectrumData(reader, false, buffer, bufferStart, out statistic);
        }

        /// <param name="path">Path to spectrum file</param>
        /// <param name="groupDefinitions">
        /// if SpectrumConstants Group present parameterNames.Length must be less or equal to constant count in text file, which contains experimental spectrum (first lines despite of very first line holding title of spectrum)
        /// </param>
        public SpectrumBase(string path, ISpectraContainer container, int bufferStart) { // ICollection<GroupDefinition> groupsDefinition) {
            //if (SpectrumBase.numberFormatInfo == null) {
            //    SpectrumBase.numberFormatInfo = new System.Globalization.NumberFormatInfo();
            //    numberFormatInfo.NumberDecimalSeparator = ".";
            //}
            _rangeArea = 0;
            _parameters = new ParameterSet();
            //this._model = model;
            this._container = container;
            this.Path = path;
            this.Name = System.IO.Path.GetFileNameWithoutExtension(path);
            TextReader tr = new StreamReader(path);
            //getExperimentalSpectrum(tr, out _constants, out this._title, null, 0);
            this._dataBufferStart = bufferStart;
            this._dataLength = getExperimentalSpectrum(tr, out this._constants, out this._title, container.Data, bufferStart, out _statistic);
            this._dataBufferStop = bufferStart + this._dataLength - 1;
            //getSpectrumHeader(tr, out _constants, out this._title);
            //ExperimentalSpectrum = getExperimentalSpectrum(tr, out _constants, out this._title, false);
            //setEffectEndChannel();
            foreach (GroupDefinition gd in container.Model.GroupsDefinition) { // groupsDefinition) {
                //raw group
                if ((gd.Type & GroupType.Raw) == GroupType.Raw) {
                    IGroup group = Parameters.addGroup(new RawGroup(gd, this));
                    if (gd.SetDefaultComponents != null)
                        gd.SetDefaultComponents(group, this, null);
                    if ((gd.Type & GroupType.SpectrumConstants) == GroupType.SpectrumConstants) {
                        if (gd.parameters.Length > 0)
                            group.Components.Size = 1;
                        for (int i = 0; i < gd.parameters.Length; i++) {
                            if (i < _constants.Length)
                                group.Components[0][gd.parameters[i].Name].Value = _constants[i];
                        }
                    }
                } else {
                    //contributed group
                    if ((gd.Type & GroupType.Contributet) == GroupType.Contributet) {
                        IGroup group = new ContributedGroup(gd, this);
                        if (gd.SetDefaultComponents != null)
                            gd.SetDefaultComponents(group, this, null);
                        Parameters.addGroup(group);
                    }
                }
            }
            setThresholds();
            Initialize();
        }

        public override string ToString() {
            return this._name;
        }

        #region xml

        protected void writeGroupNode(XmlWriter writer, IGroup group) {
            //group
            writer.WriteStartElement("group");
            writer.WriteAttributeString("name", group.Definition.name);

            //type attribute
            writer.WriteAttributeString("type", ((int)group.Definition.Type).ToString());
            //kind attribute
            writer.WriteAttributeString("kind", group.Definition.kind.ToString());
            //fixedComponents
            //depreceated fixedComponents. component count instead (0 means unlimited component count
            //writer.WriteAttributeString("fixedComponents", group.Definition.componentCount.ToString());
            writer.WriteAttributeString("defCompCount", group.Definition.componentCount.ToString());            
            //area
            if (group is ContributedGroup) {
                ContributedGroup cgroup = (ContributedGroup)group;
                writer.WriteAttributeString("area", cgroup.groupArea.ToString());
                if ((group.Definition.Type & GroupType.CalcContribution) != GroupType.CalcContribution) {
                    writer.WriteStartElement("cb");
                    //if (cgroup.contribution.Expression != null)
                    //    writer.WriteAttributeString("expression", cgroup.contribution.Expression.ToString());
                    //else
                    writer.WriteAttributeString("v", cgroup.contribution.Value.ToString());
                    writer.WriteAttributeString("e", cgroup.contribution.Error.ToString());
                    writer.WriteAttributeString("s", ((int)(cgroup.contribution.Status & ~ParameterStatus.Binding)).ToString());
                    if (cgroup.contribution.ReferenceGroup != 0)
                        writer.WriteAttributeString("r", cgroup.contribution.ReferenceGroup.ToString());
                    if (!double.IsInfinity(cgroup.contribution.Maximum))
                        writer.WriteAttributeString("maximum", cgroup.contribution.Maximum.ToString());
                    if (!double.IsInfinity(cgroup.contribution.Minimum))
                        writer.WriteAttributeString("minimum", cgroup.contribution.Minimum.ToString());
                    writer.WriteEndElement(); //contribution
                }
            }
            //components
            writer.WriteStartElement("cs");
            foreach (IComponent component in group.Components) {
                writer.WriteStartElement("c");
                if (component is ExtComponent)
                    if (((ExtComponent)component).IntInCounts > 0.0)
                        writer.WriteAttributeString("A", ((ExtComponent)component).IntInCounts.ToString());
                foreach (IParameter parameter in component) {
                    writer.WriteStartElement("p");
                    //if (parameter.Expression != null)
                    //    writer.WriteAttributeString("expression", parameter.Expression.ToString());
                    //else
                    writer.WriteAttributeString("v", parameter.Value.ToString());
                    writer.WriteAttributeString("e", parameter.Error.ToString());
                    writer.WriteAttributeString("s", ((int)(parameter.Status & ~ParameterStatus.Binding)).ToString());
                    if (parameter.ReferenceGroup != 0)
                        writer.WriteAttributeString("r", parameter.ReferenceGroup.ToString());
                    if (!double.IsInfinity(parameter.Maximum))
                        writer.WriteAttributeString("maximum", parameter.Maximum.ToString());
                    if (!double.IsInfinity(parameter.Minimum))
                        writer.WriteAttributeString("minimum", parameter.Minimum.ToString());
                    writer.WriteEndElement(); //parameter
                }
                writer.WriteEndElement(); //component
            }
            //unique parameters
            writer.WriteStartElement("c");
            writer.WriteAttributeString("unique", "True");
            foreach (IParameter parameter in group.GroupUniqueParameters) {
                writer.WriteStartElement("p");
                //if (parameter.Expression != null)
                //    writer.WriteAttributeString("expression", parameter.Expression.ToString());
                //else
                writer.WriteAttributeString("v", parameter.Value.ToString());
                writer.WriteAttributeString("e", parameter.Error.ToString());
                writer.WriteAttributeString("s", ((int)(parameter.Status & ~ParameterStatus.Binding)).ToString());
                if (parameter.ReferenceGroup != 0)
                    writer.WriteAttributeString("r", parameter.ReferenceGroup.ToString());
                writer.WriteEndElement(); //parameter
            }
            writer.WriteEndElement(); //unique component

            writer.WriteEndElement(); //components
            writer.WriteEndElement(); //group
        }

        /// <summary>
        /// Zwraca grupe odczytana przez obiekt klasy XmlReader ze strumienia xml. Jesli lista groups==null
        /// nowa grupa jest tworzona. w przeciwnym wypadku parametry sa wpisywane do jednej z istniejacych
        /// grup o ile zostala znaleziona grupa o identycznej definicji do tej, ktora odczytal obiekt klasy XmlReader
        /// </summary>
        /// <param name="reader"></param>
        /// <param name="groups"></param>
        /// <returns></returns>
        public static IGroup getGroup(XmlReader reader, ISpectrum owner, IModel model) {
            IGroup group = null;
            //GroupDefinition definition = new GroupDefinition();
            //try {
            reader.Read();
            string groupName = "";
            int kind = -1;
            GroupType groupType = 0;
            byte componentCount = 1;


            while (reader.MoveToNextAttribute()) {
                switch (reader.Name) {
                    case "name": groupName = reader.Value; break;
                    case "type": groupType = (GroupType)Int32.Parse(reader.Value); break;
                    case "kind": kind = Int32.Parse(reader.Value); break;
                    case "fixedComponents": componentCount = (byte)(Boolean.Parse(reader.Value) ? 1 : 0); break;
                    case "defCompCount": componentCount = byte.Parse(reader.Value); break;
                }
            }
            reader.MoveToElement();

            if ((kind == -1) || (kind >= model.GroupsDefinition.Length)) throw new SpectrumLoadException("Invalid model file - error in group definition");

            GroupDefinition definition = model.GroupsDefinition[kind];
            definition.name = groupName;
            definition.Type = groupType;
            definition.kind = kind;
            definition.componentCount = componentCount;


            ////jesli ma sie odbyc tylko import parametrow nalezy znalezc odpowiednia grupe wsrod listy groups. jestli zadna z nich nie odpowiada definicji import jest anulowany
            //if (groups != null) {
            //    for (int i=0; i<groups.Count; i++)
            //        if (groups[i].Definition == definition) {
            //            group = groups[i];
            //            group.Components.Size = 0;
            //        }           
            ////jesli grupa ma byc utworzona - konstrukcja odpowiednich typow grup,
            //} else {
                if ((definition.Type & GroupType.Raw) == GroupType.Raw) {
                    group = new RawGroup(definition, owner);
                } else {
                    if ((definition.Type & GroupType.Contributet) == GroupType.Contributet) {
                        group = new ContributedGroup(definition, owner);
                    } else
                        throw new Exception();
                }
            //}
            if (group != null) {
                double value;
                IParameter parameter;
                while (reader.Read()) {
                    switch (reader.Name) {
                        case "cb":
                            parameter = ((ContributedGroup)group).contribution;
                            while (reader.MoveToNextAttribute()) {
                                switch (reader.Name) {
                                    case "v":
                                        if (double.TryParse(reader.Value, out value))
                                            parameter.Value = value;
                                        else
                                            parameter.Value = 0;
                                        break;
                                    case "e":
                                        if (double.TryParse(reader.Value, out value))
                                            parameter.Error = value;
                                        else
                                            parameter.Error = 0;
                                        break;
                                    case "s": 
                                        parameter.Status = (ParameterStatus)Int32.Parse(reader.Value);
                                        parameter.Status &= ~ParameterStatus.Binding;
                                        break;
                                    case "r": parameter.ReferenceGroup = Int32.Parse(reader.Value); break;
                                    case "maximum": parameter.Maximum = double.Parse(reader.Value); break;
                                    case "minimum": parameter.Minimum = double.Parse(reader.Value); break;
                                }
                            }
                            break;
                        case "cs": //components
                            int compId = 0;

                            while (reader.Read()) {
                                if (reader.Name == "c") { //component
                                    int parId = 0;
                                    bool isUnique = false;
                                    double compArea = double.NaN;
                                    while (reader.MoveToNextAttribute()) {
                                        switch (reader.Name) {
                                            case "unique": isUnique = Boolean.Parse(reader.Value); break;
                                            case "A": compArea = double.Parse(reader.Value); break;
                                        }
                                    }
                                    if (!isUnique)
                                        group.Components.Size++;
                                    if (!double.IsNaN(compArea))
                                        if (group.Components[compId] is ExtComponent)
                                            ((ExtComponent)group.Components[compId]).IntInCounts = compArea;
                                    while (reader.Read()) {
                                        if (reader.Name == "p") { //parameter
                                            parameter = (isUnique) ? group[parId] : group.Components[compId][parId];
                                            while (reader.MoveToNextAttribute()) {
                                                switch (reader.Name) {
                                                    case "v": //value
                                                        if (double.TryParse(reader.Value, out value))
                                                            parameter.Value = value;
                                                        else
                                                            parameter.Value = 0;
                                                        break;
                                                    case "e": //error
                                                        if (double.TryParse(reader.Value, out value))
                                                            parameter.Error = value;
                                                        else
                                                            parameter.Error = 0;
                                                        break;
                                                    case "s": //status
                                                        parameter.Status = (ParameterStatus)Int32.Parse(reader.Value);
                                                        parameter.Status &= ~ParameterStatus.Binding;
                                                        break;
                                                    case "r": //refgroup
                                                        parameter.ReferenceGroup = Int32.Parse(reader.Value);
                                                        break;
                                                    case "maximum": parameter.Maximum = double.Parse(reader.Value); break;
                                                    case "minimum": parameter.Minimum = double.Parse(reader.Value); break;
                                                }
                                            }
                                            reader.MoveToElement();
                                            parId++;
                                        } else {
                                            break;
                                        }
                                    }
                                    compId++;
                                } else {
                                    break;
                                }
                            }
                            break;
                    }
                }
            }
            return group;
        }

        double ExtractValue(object valueHolder) {
            return ((IParameter)valueHolder).Value;
        }

        /// <summary>
        /// Spectrum constructor
        /// </summary>
        /// <param name="spectrumNode"></param>
        /// <param name="root">Not necessary when including data</param>
        //public SpectrumBase(XmlNode spectrumNode, string root) {
        //    if (spectrumNode.Attributes["name"] != null)
        //        this.Name = spectrumNode.Attributes["name"].Value;
        //    this._parameters = new ParameterSet();
        //    XmlNode currentNode = spectrumNode.FirstChild;
        //    while (currentNode!= null) {
        //        switch (currentNode.Name) {
        //            case "parameters":
        //                XmlNode groupNode = currentNode.FirstChild;
        //                while (groupNode != null) {
        //                    this._parameters.addGroup(getGroup(groupNode));
        //                    groupNode = groupNode.NextSibling;
        //                }
        //                break;
        //            case "data":
        //                double[] consts;
        //                TextReader tr;
        //                switch (currentNode.Attributes["type"].Value) {
        //                    case "file":
        //                        this._path = currentNode.InnerText.Trim();
        //                        this._path = System.IO.Path.Combine(root, this._path);
        //                        tr = new StreamReader(this._path);
        //                        this._experimentalSpectrum = getExperimentalSpectrum(tr, out consts, out this._title);
        //                        break;
        //                    case "data":
        //                        string data = currentNode.InnerText.Trim();
        //                        UnicodeEncoding uniEncoding = new UnicodeEncoding();
        //                        byte[] bytes = uniEncoding.GetBytes(data);
        //                        using (MemoryStream ms = new MemoryStream()) {
        //                            foreach (byte b in bytes)
        //                                if (b != 0)
        //                                    ms.WriteByte(b);
        //                            ms.Seek(0, SeekOrigin.Begin);
        //                            tr = new StreamReader(ms);
        //                            this._experimentalSpectrum = getExperimentalSpectrum(tr, out consts, out this._title);
        //                        }
        //                        break;
        //                }
        //                break;
        //        }
        //        currentNode = currentNode.NextSibling;
        //    }
        //}

        public SpectrumBase(XmlReader spectrumReader, string root, ISpectraContainer container, int bufferStart) {
            //if (SpectrumBase.numberFormatInfo == null) {
            //    SpectrumBase.numberFormatInfo = new System.Globalization.NumberFormatInfo();
            //    numberFormatInfo.NumberDecimalSeparator = ".";
            //}
            _rangeArea = 0;
            try {
                //this._model = model;
                this._container = container;
                spectrumReader.Read();
                while (spectrumReader.MoveToNextAttribute()) {
                    switch (spectrumReader.Name) {
                        case "name": this.Name = spectrumReader.Value; break;
                        case "fit": this._fit = double.Parse(spectrumReader.Value); break;
                    }
                }

                this._parameters = new ParameterSet();
                //XmlNode currentNode = spectrumNode.FirstChild;
                //spectrumReader.ReadStartElement("spectrum"); //proceed to parameters node
                while (spectrumReader.Read()) {
                    switch (spectrumReader.Name) {
                        case "constants":
                            int constIndex = 0;
                            spectrumReader.MoveToNextAttribute();
                            if (spectrumReader.Name == "count")
                                this._constants = new double[Int32.Parse(spectrumReader.Value)];
                            else
                                throw new Exception("Error in model file. Spectrum constants node has missing or invalid \"count\" attribute");
                            while (spectrumReader.MoveToNextAttribute()) {
                                this._constants[constIndex++] = double.Parse(spectrumReader.Value);
                            }
                            //spectrumReader.MoveToElement();
                            break;
                        case "ps": //parameters
                            while (spectrumReader.Read()) { //groups
                                if (spectrumReader.Name == "group") {
                                    XmlReader groupReader = spectrumReader.ReadSubtree();
                                    this._parameters.addGroup(getGroup(groupReader, this, this.Container.Model));
                                    groupReader.Close();
                                } else {
                                    break;
                                }
                            }
                            break;
                        case "data":
                            TextReader tr;
                            while (spectrumReader.MoveToNextAttribute()) {
                                if (spectrumReader.Name == "type") {
                                    switch (spectrumReader.Value) {
                                        case "file":
                                            spectrumReader.Read();
                                            this._path = spectrumReader.Value.Trim();
                                            //if root != null then spectrumReader.Value holds relative path. otherwise it is a full path defined in CompressedES project file
                                            if (root != null)
                                                this._path = System.IO.Path.Combine(root, this._path);
                                            if (!System.IO.File.Exists(this._path))
                                                throw new SpectrumLoadException(String.Format("Couldn't find spectrum file {0}", this._path));
                                            using (tr = new StreamReader(this._path))
                                                getExperimentalSpectrum(tr, out this._constants, out this._title, container.Data, bufferStart, out _statistic);
                                            //    getSpectrumData(tr, true, container.Data, bufferStart);
                                            setThresholds();
                                            //getExperimentalSpectrum(tr, out _constants, out this._title, container.Data, bufferStart);
                                            //setEffectEndChannel();
                                            break;
                                        case "data":
                                            this._path = null;
                                            spectrumReader.Read();
                                            string data = spectrumReader.Value.Trim();
                                            UnicodeEncoding uniEncoding = new UnicodeEncoding();
                                            byte[] bytes = uniEncoding.GetBytes(data);
                                            using (MemoryStream ms = new MemoryStream()) {
                                                foreach (byte b in bytes)
                                                    if (b != 0)
                                                        ms.WriteByte(b);
                                                ms.Seek(0, SeekOrigin.Begin);
                                                using (tr = new StreamReader(ms))
                                                    getExperimentalSpectrum(tr, out this._constants, out this._title, container.Data, bufferStart, out _statistic);
                                                    //getSpectrumData(tr, false, container.Data, bufferStart);
                                                
                                                //getExperimentalSpectrum(tr, out _constants, out this._title, container.Data, bufferStart);
                                                //setEffectEndChannel();
                                            }
                                            setThresholds();
                                            break;
                                    }
                                } else if (spectrumReader.Name == "length") {
                                    this._dataLength = int.Parse(spectrumReader.Value);
                                    this._dataBufferStart = bufferStart;
                                    this._dataBufferStop = bufferStart + this._dataLength - 1;
                                }
                            }

                            break;
                    }
                }
                Initialize();
            } catch (SpectrumLoadException ex) {
                throw ex;
            } catch (Exception ex) {
                throw new SpectrumLoadException(String.Format("Error while loading spectrum:\n{0}", ex.Message));
            }
        }


        #endregion xml

        #region IDisposable Members

        public void Dispose() {
            int g;
            for (g = 0; g < this._parameters.GroupCount; g++)
                this._parameters[g].Dispose();
        }

        #endregion
    }
}
