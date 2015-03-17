using System;
using System.Collections.Generic;
using System.Text;
using Evel.interfaces;
using System.IO;
using System.Xml;
using System.Reflection;
using System.ComponentModel;
using System.Threading;
using System.IO.Compression;

namespace Evel.engine {

    public struct SpectraContainerDescription {
        public string name;
        public IModel model;
        public string[] spectraPaths;
        public ICollection<GroupDefinition> groupsDefinition;
    }

    public abstract class ProjectBase : IProject {

        public static char[] AddressDelimiters = new char[] { '[', ';', '#', ']' };

        //protected bool _compressed;
        protected ProjectFileType _fileType;
        protected bool _isBusy;
        protected bool _cancel;
        protected string _caption;
        //protected string _path;
        protected string _projectFile;
        protected string _name;
        protected string _description;
        protected SearchMode _searchMode;
        protected List<ISpectraContainer> _containers;
        protected SearchFlags _flags;
        protected bool _calculatedValues;
        //protected List<IBinding> _bindings;
        protected BindingsManager _bindingsManager;

        private delegate void SearchEventHandler(List<ISpectrum> spectra); //ICollection<ISpectrum> spectra);
        private delegate void SeriesSearchEventHandler(ICollection<string> evelListeners, List<ISpectrum> spectra);

        protected bool Canceled {
            get { return this._cancel; }
        }

        //public void FirstSpectraSearchAsync(ICollection<ISpectrum> spectra) {
        public void FirstSpectraSearchAsync(List<ISpectrum> spectra) {
            _isBusy = true;
            _cancel = false;
            SearchEventHandler seh = new SearchEventHandler(FirstSpectraSearch);
            seh.BeginInvoke(spectra, null, null);         
        }

        public void SeriesSearchAsync(ICollection<string> evelListeners, List<ISpectrum> spectra) {
            _isBusy = true;
            _cancel = false;
            SeriesSearchEventHandler sseh = new SeriesSearchEventHandler(SeriesSearch);
            sseh.BeginInvoke(evelListeners, spectra, null, null);
        }

        public void SearchAsyncCancel() {
            _cancel = true;

        }



        public bool IsBusy {
            get { return this._isBusy; }
        }

        public SearchFlags Flags {
            get { return this._flags; }
            set { this._flags = value; }
        }

        public SearchMode SearchMode { get { return this._searchMode; } }

        #region constructors

        public ProjectBase() {
            this._containers = new List<ISpectraContainer>();
            this._flags = SearchFlags.Standard; // = new Dictionary<string, bool>();
            this._calculatedValues = true;
            //this._bindings = new List<IBinding>();
            this._bindingsManager = new BindingsManager(this);
            this._searchMode = SearchMode.Inactive;
        }

        public ProjectBase(string caption, SpectraContainerDescription[] descriptions)
            : this() {
            this._caption = caption;
            foreach (SpectraContainerDescription desc in descriptions) {
                Assembly ass = Assembly.GetAssembly(this.GetSpectraContainerType());
                string typeName = this.GetSpectraContainerType().ToString();
                BindingFlags bindingFlags = BindingFlags.CreateInstance;
                object[] args = new object[] { this, desc.name, desc.model, desc.spectraPaths, desc.groupsDefinition };
                SpectraContainerBase container = (SpectraContainerBase)ass.CreateInstance(typeName, true, bindingFlags, null, args, null, null);
                if (container != null) {
                    AddSpectraContainer(container);
                }
            }
        }

        public ProjectBase(string fileName)
            : this() {
            SpectraContainerBase container;
            object[] args = new object[3];
            args[0] = this;
            Stream stream;
            //jesli rozszerzeniu pliku projektu to evpc projekt zostal zapisany w plikach skompresowanych i
            //taki rodzaj strumienia musi byc wykorzystany
            //if (this._compressed = System.IO.Path.GetExtension(fileName) == ".evpc")
            this._fileType = ProjectFileExtensions.GetProjectFileType(System.IO.Path.GetExtension(fileName));
            if (this._fileType == ProjectFileType.Normal)
                stream = new FileStream(fileName, FileMode.Open);
            else
                stream = new GZipStream(new FileStream(fileName, FileMode.Open), CompressionMode.Decompress);
                
            try {
                int spectraCount = -1;
                //this._path = System.IO.Path.GetDirectoryName(fileName);
                this._projectFile = fileName;
                XmlReaderSettings settings = new XmlReaderSettings();
                settings.IgnoreWhitespace = true;
                using (XmlReader reader = XmlReader.Create(stream, settings)) {

                    reader.Read(); //declaration
                    reader.Read(); //project
                    if (reader.HasAttributes) {
                        while (reader.MoveToNextAttribute()) {
                            switch (reader.Name) {
                                case "caption": this._caption = reader.Value; break;
                                case "calculatedValues": this._calculatedValues = Boolean.Parse(reader.Value); break;
                                case "spectraCount": spectraCount = int.Parse(reader.Value); break;
                            }
                        }
                        reader.MoveToElement();
                    }
                    //List<IParameter> bindingParameters = new List<IParameter>();
                    //reader.ReadStartElement("project");
                    while (reader.Read()) {
                        switch (reader.Name) {
                            case "models":
                                bool includedModelsData = reader.GetAttribute("defs") == "included";
                                while (reader.Read()) {
                                    if (reader.Name == "model") {
                                        BindingFlags bindingFlags = BindingFlags.CreateInstance;
                                        Assembly ass = Assembly.GetAssembly(this.GetSpectraContainerType());
                                        string typeName = this.GetSpectraContainerType().ToString();
                                        
                                        XmlReader containerReader;

                                        if (includedModelsData) {
                                            containerReader = reader.ReadSubtree();
                                            args[1] = containerReader;
                                            args[2] = null;
                                            try {
                                                container = (SpectraContainerBase)ass.CreateInstance(typeName, true, bindingFlags, null, args, null, null);
                                                if (container != null)
                                                    AddSpectraContainer(container);
                                            } finally {
                                                containerReader.Close();
                                            }
                                        } else {
                                            Stream conStream;
                                            reader.MoveToFirstAttribute();
                                            string containerFileName = reader.Value; // containerElement.Attributes["file"].Value;
                                            containerFileName = System.IO.Path.Combine(this.Path, containerFileName);
                                            containerReader = getXmlReader(containerFileName, this._fileType, out conStream);
                                            try {
                                                //args = new object[] { this, containerReader, System.IO.Path.GetDirectoryName(containerFileName) };
                                                args[1] = containerReader;
                                                args[2] = System.IO.Path.GetDirectoryName(containerFileName);
                                                container = (SpectraContainerBase)ass.CreateInstance(typeName, true, bindingFlags, null, args, null, null);
                                                if (container != null) 
                                                    AddSpectraContainer(container);
                                            } finally {
                                                containerReader.Close();
                                                conStream.Close();
                                            }
                                            reader.MoveToElement();
                                        }
                                        
                                    } else
                                        break;
                                }
                                    break;
                            case "bindings":
                                //if (reader.ReadToFollowing("bindings")) {
                                    while (reader.Read()) {
                                        if (reader.Name == "binding") {
                                            string name = reader.GetAttribute("name");
                                            switch (reader.GetAttribute("type")) {
                                                case "parameter":
                                                    _bindingsManager.Add(new ParameterBinding(reader, this, name));
                                                    break;
                                                case "group":
                                                    _bindingsManager.Add(new GroupBinding(reader, this, name));
                                                    break;
                                            }
                                        } else break;
                                    }
                                break;
                        }
                    }
                }
            } finally {
                if (stream != null)
                    stream.Close();
            }
        }

        #endregion constructors


        public static XmlReader getXmlReader(string filePath, ProjectFileType fileType, out Stream stream) {
            stream = new FileStream(filePath, FileMode.Open);
            if (fileType == ProjectFileType.Normal)
                return XmlReader.Create(stream);
            else {
                stream = new GZipStream(stream, CompressionMode.Decompress);
                return XmlReader.Create(stream);
            }
        }

        public static XmlWriter getXmlWriter(string filePath, ProjectFileType fileType, out Stream stream) {
            stream = new FileStream(filePath, FileMode.Create);
            if (fileType == ProjectFileType.Normal)
                return XmlWriter.Create(stream);
            else {
                stream = new GZipStream(stream, CompressionMode.Compress);
                return XmlWriter.Create(stream);
            }
        }

        public static XmlReader getXmlReader(string filePath, out Stream stream) {
            return getXmlReader(filePath, ProjectFileExtensions.GetProjectFileType(System.IO.Path.GetExtension(filePath)), out stream);
        }


        #region IProject Members

        #region abstracts

        public abstract event AsyncFirstSpectraSearchCompletedEventHandler FirstSpectraSearchCompleted;
        public abstract event IndependencyFoundEventHandler IndependencyFound;
        public abstract event ProgressChangedEventHandler FirstSpectraSearchProgressChanged;
        public abstract event AsyncCompletedEventHandler SearchCompleted;
        public abstract event ProgressChangedEventHandler SearchProgressChanged;
        public abstract event IndefiniteMatrixEventHandler IndefiniteMatrixGot;

        public abstract void SeriesSearch(ICollection<string> evelListeners, List<ISpectrum> spectra);

        public abstract void FirstSpectraSearch(List<ISpectrum> spectra);

        public abstract Type GetSpectraContainerType();

        public abstract ISpectraContainer CreateContainer(string name, IModel model, ICollection<string> spectraPaths, ICollection<GroupDefinition> groupsDefinition);

        public abstract ISpectraContainer CreateContainer(IModel model);

        public abstract string Description { get; }

        public abstract string ExperimentalMethodName { get; }

        public abstract double Fit { get; set; }

        public abstract string Name { get; }

        public abstract string SaveObjectState(string filePath);

        public abstract void RestoreSpectrumStartingValues(ISpectrum spectrum, ParameterStatus status);

        public abstract void RestoreParameter(ISpectrum spectrum, ParameterLocation location);

        #endregion abstracts

        public bool CalculatedValues {
            get { return _calculatedValues; }
            set { _calculatedValues = value; }
        }

        //public List<IBinding> Bindings {
        //    get { return this._bindings; }
        //    set { this._bindings = value; }
        //}

        public IBindingsManager BindingsManager {
            get { return this._bindingsManager; }
        }

        public string Caption {
            get { return this._caption; }
        }

        public string Path {
            //get { return this._path; }
            //set { this._path = value; }
            get {
                if (this._projectFile != null)
                    return System.IO.Path.GetDirectoryName(this._projectFile);
                else
                    return null;
            }
        }

        public virtual List<ISpectraContainer> Containers {
            get { return this._containers; }
        }

        public virtual ISpectraContainer AddSpectraContainer(ISpectraContainer container) {
            Type sct = GetSpectraContainerType();
            if (container.GetType() != sct)
                throw new ArgumentException(String.Format("Invalid spectra container class: {0}. Expecting {1}", container.GetType(), sct));
            else {
                this._containers.Add(container);
                return container;
            }
        }

        public virtual ISpectraContainer AddSpectraContainer(string containerXmlFilePath) {
            Assembly ass = Assembly.GetAssembly(this.GetSpectraContainerType());
            string typeName = this.GetSpectraContainerType().ToString();
            BindingFlags bindingFlags = BindingFlags.CreateInstance;
            object[] args = new object[] { this, containerXmlFilePath };
            SpectraContainerBase container = (SpectraContainerBase)ass.CreateInstance(typeName, true, bindingFlags, null, args, null, null);
            if (container != null) {
                AddSpectraContainer(container);
            }
            return container;
        }

        public virtual bool RemoveContainer(ISpectraContainer container) {
            return this._containers.Remove(container);
        }

        public virtual string ProjectFile {
            get {
                return this._projectFile;
                //if (this.Path != null) {
                //    string projectFile = System.IO.Path.Combine(this.Path, this._caption);
                //    projectFile = System.IO.Path.ChangeExtension(projectFile, ProjectFileExtensions.GetExtension(this._fileType));
                //    return projectFile;
                //} else
                //    return null;
            }
            set { this._projectFile = value; }
        }

        public ProjectFileType FileType {
            get { return this._fileType; }
        }

        public virtual void Save(string filePath) {
            this.Save(filePath, this._fileType);
        }

        public virtual void Save(string filePath, ProjectFileType fileType) {
            this._projectFile = filePath;
            this._fileType = fileType;
            this._caption = System.IO.Path.GetFileName(filePath);
            string modelsDirectory = System.IO.Path.Combine(this.Path, "models");
            string containerFile, containerDirectory, spectraDirectory, destFileName;
            
            if (!System.IO.Directory.Exists(this.Path) && (fileType == ProjectFileType.Normal || fileType == ProjectFileType.CompressedIS))
                System.IO.Directory.CreateDirectory(this.Path);

            Stream stream = null;
            if (fileType != ProjectFileType.Normal)
                stream = new GZipStream(new FileStream(ProjectFile, FileMode.Create), CompressionMode.Compress);
            else
                stream = new FileStream(ProjectFile, FileMode.Create);
            try {
                using (XmlWriter writer = XmlWriter.Create(stream)) {
                    writer.WriteStartElement("project");

                    writer.WriteAttributeString("caption", this.Caption);
                    writer.WriteAttributeString("name", this.Caption);
                    writer.WriteAttributeString("calculatedValues", this.CalculatedValues.ToString());
                    int i, scount = 0;
                    for (i=0; i<this.Containers.Count; i++)
                        scount+=this.Containers[i].Spectra.Count;
                    writer.WriteAttributeString("spectraCount", scount.ToString());
                    string[] tmpStrings = this.GetType().ToString().Split(new char[] { '.' });
                    string className = tmpStrings[tmpStrings.Length - 1];
                    writer.WriteAttributeString("class", className);
                    writer.WriteStartElement("models");
                    if (fileType == ProjectFileType.CompressedES || fileType == ProjectFileType.CompressedPack)
                        writer.WriteAttributeString("defs", "included");
                    else
                        writer.WriteAttributeString("defs", "files");
                    if (!System.IO.Directory.Exists(modelsDirectory))
                        System.IO.Directory.CreateDirectory(modelsDirectory);
                    foreach (ISpectraContainer container in this._containers) {
                        writer.WriteStartElement("model");
                        Stream conStream = null;
                        if (fileType == ProjectFileType.Normal || fileType == ProjectFileType.CompressedIS) {
                            containerDirectory = System.IO.Path.Combine(modelsDirectory, container.Name);
                            if (!System.IO.Directory.Exists(containerDirectory))
                                System.IO.Directory.CreateDirectory(containerDirectory);
                            containerFile = System.IO.Path.Combine(containerDirectory, container.Name);
                            containerFile = System.IO.Path.ChangeExtension(containerFile, (fileType == ProjectFileType.CompressedIS) ? ProjectFileExtensions.COMPRESSED_IS_MODEL : ProjectFileExtensions.NORMAL_MODEL);
                            writer.WriteAttributeString("file", String.Format("models/{0}/{1}", container.Name, System.IO.Path.GetFileName(containerFile)));
                            try {
                                using (XmlWriter conWriter = getXmlWriter(containerFile, this._fileType, out conStream))
                                    container.Save(conWriter, this._fileType);
                            } finally {
                                conStream.Close();
                            }
                            //copy or create spectra files
                            spectraDirectory = System.IO.Path.Combine(containerDirectory, "spectra");
                            if (!System.IO.Directory.Exists(spectraDirectory))
                                System.IO.Directory.CreateDirectory(spectraDirectory);
                            foreach (ISpectrum spectrum in container.Spectra) {
                                destFileName = System.IO.Path.Combine(spectraDirectory, spectrum.Name);
                                if (spectrum.Path != null) {                                    
                                    destFileName = System.IO.Path.ChangeExtension(destFileName, System.IO.Path.GetExtension(spectrum.Path));
                                    try {
                                        System.IO.File.Copy(spectrum.Path, destFileName, false);
                                    } catch (Exception) {
                                    }
                                } else {
                                    destFileName = System.IO.Path.ChangeExtension(destFileName, ".txt");
                                    spectrum.writeData(destFileName);
                                }
                            }

                        } else
                            container.Save(writer, this._fileType);
                        
                        //XML model node
                        writer.WriteEndElement(); //model

                    }
                    writer.WriteEndElement(); //models

                    //bindings
                    writer.WriteStartElement("bindings");
                    foreach (Binding b in _bindingsManager) {
                        b.WriteXml(writer);
                    }
                    writer.WriteEndElement(); //bindings

                    writer.WriteEndElement(); //project

                    writer.Flush();
                    writer.Close();
                }
            } finally {
                stream.Close();
            }
            //remove document folders not included in project (renamed or removed)
            foreach (string docDir in Directory.GetDirectories(modelsDirectory)) {
                bool present = false;
                foreach (ISpectraContainer container in this.Containers) {
                    if (container.Name.Equals(System.IO.Path.GetFileNameWithoutExtension(docDir))) {
                        present = true;
                        break;
                    }
                }
                if (!present)
                    Directory.Delete(docDir, true);
            }
        }
      
        public IParameter GetParameter(string address) {
            string[] coords = address.Split(AddressDelimiters, StringSplitOptions.RemoveEmptyEntries);
            return ((List<ISpectraContainer>)Containers)[Int32.Parse(coords[0].Substring(1))-1].GetParameter(address);
        }

        public string GetParameterAddress(IParameter parameter) {
            ISpectraContainer container;
            IGroup group;

            if (parameter.Parent is Evel.interfaces.IComponent) {
                Evel.interfaces.IComponent component = (Evel.interfaces.IComponent)parameter.Parent;
                if (component.Parent is IComponents)
                    group = ((IComponents)component.Parent).Parent;
                else
                    group = (IGroup)component.Parent;
            } else {
                group = (IGroup)parameter.Parent;
            }
            container = group.OwningSpectrum.Container;
            return String.Format("[d{0};{1}]", ((List<ISpectraContainer>)Containers).IndexOf(container) + 1, container.GetParameterAddress(parameter));
        }

        //public int findBinding(IParameter parameter, out IBinding binding) {
        //    int i;
        //    foreach (IBinding b in _bindingsManager) {
        //        if (b is ParameterBinding) {
        //            for (i = 0; i < ((ParameterBinding)b).Parameters.Length; i++)
        //                if (((ParameterBinding)b).Parameters[i] == parameter) {
        //                    binding = b;
        //                    return i;
        //                }
        //        }
        //    }
        //    binding = null;
        //    return -1;
        //}

        //public void removeBinding(int id) {
        //    IBinding binding = _bindings[id - 1];
        //    if (_bindings[id - 1] is ParameterBinding) {
        //        foreach (IParameter parameter in ((ParameterBinding)binding).Parameters) {
        //            ParameterLocation pl = GetParameterLocation(parameter);
        //            ISpectraContainer container = Containers[pl.docId];
        //            IParameter topParameter = null;
        //            for (int specId = 0; specId < container.Spectra.Count; specId++) {
        //                pl.specId = specId;
        //                IParameter p = GetParameter(pl);
        //                p.Status &= ~ParameterStatus.Binding;
        //                p.ReferencedParameter = topParameter;
        //                if (specId == 0) topParameter = p;
        //            }
        //        }
        //    }
        //    _bindings[id - 1].Dispose();
        //    _bindings.RemoveAt(id - 1);
        //}

        //public void addBinding(IBinding binding) {
        //    _bindings.Add(binding);
        //    if (binding is ParameterBinding)
        //    foreach (IParameter parameter in ((ParameterBinding)binding).Parameters) {
        //        ParameterLocation pl = GetParameterLocation(parameter);
        //        ISpectraContainer container = Containers[pl.docId];
        //        foreach (IParameter p in container.GetParameters(parameter)) {
        //            p.Status = ((ParameterBinding)binding).Source.Status;
        //            p.ReferencedParameter = ((ParameterBinding)binding).Source;
        //        }
        //    }
        //}

        public ParameterLocation GetParameterLocation(IParameter parameter) {
            int docId, specId, groupId, compId, parId;
            ParameterLocation pl = new ParameterLocation();
            for (docId = 0; docId < Containers.Count; docId++) {
                pl.docId = docId;
                for (specId = 0; specId < Containers[docId].Spectra.Count; specId++) {
                    pl.specId = specId;
                    for (groupId = 0; groupId < Containers[docId].Spectra[specId].Parameters.GroupCount; groupId++) {
                        pl.groupId = groupId;
                        if (Containers[docId].Spectra[specId].Parameters[groupId] is ContributedGroup) {
                            if (((ContributedGroup)Containers[docId].Spectra[specId].Parameters[groupId]).contribution == parameter) {
                                pl.compId = -1;
                                pl.parId = -1;
                                pl.parName = parameter.Definition.Name;
                                return pl;
                            }
                        }
                        for (compId = 0; compId < Containers[docId].Spectra[specId].Parameters[groupId].Components.Size; compId++) {
                            pl.compId = compId;
                            for (parId = 0; parId < Containers[docId].Spectra[specId].Parameters[groupId].Components[compId].Size; parId++)
                                if (Containers[docId].Spectra[specId].Parameters[groupId].Components[compId][parId] == parameter) {
                                    pl.parId = parId;
                                    pl.parName = parameter.Definition.Name;
                                    return pl;
                                }
                        }

                    }
                }
            }
            return 0;
        }

        public IParameter GetParameter(ParameterLocation location) {
            if (location.compId != -1)
                return Containers[location.docId].Spectra[location.specId].Parameters[location.groupId].Components[location.compId][location.parId];
            else if (location.parName == "contribution")
                return ((ContributedGroup)Containers[location.docId].Spectra[location.specId].Parameters[location.groupId]).contribution;
            else
                return null;

        }

        public ISpectraContainer this[string name] {
            get {
                for (int i = 0; i < this._containers.Count; i++)
                    if (this._containers[i].Name == name) return this._containers[i];
                throw new ArgumentException(String.Format("No document named {0}", name));
            }
        }
        
        public ISpectraContainer this[int id] {
            get {
                return this._containers[id];
            }
        }

        #endregion

        #region net

        protected bool isListenersValid(ICollection<string> evelListeners) {
            if (evelListeners == null) return false;
            return evelListeners.Count > 0;
        }

        #endregion

        #region static

        public static void getParameterInfo(IParameter parameter, out string doc, out string group, out string compId) {
            if (parameter.Parent is Evel.engine.ContributedGroup) {
                compId = "-1";
                IGroup gr = (Evel.engine.ContributedGroup)parameter.Parent;
                group = gr.Definition.name;
                doc = gr.OwningSpectrum.Container.Name;
            } else {
                Evel.interfaces.IComponent comp = (Evel.interfaces.IComponent)parameter.Parent;
                IComponents comps = (IComponents)comp.Parent;
                compId = ((comps.Size > 1) ? comps.IndexOf(comp) + 1 : 0).ToString();
                group = comps.Parent.Definition.name;
                doc = comps.Parent.OwningSpectrum.Container.Name;
            }
        }

        #endregion static

    }



}
