using System;
using System.Collections.Generic;
using System.Text;
using System.ComponentModel;

namespace Evel.interfaces {

    public struct ProjectFileExtensions {
        public const string NORMAL_PROJECT = ".ltp";
        public const string NORMAL_MODEL = ".ltm";
        public const string COMPRESSED_IS_PROJECT = ".ltpi";
        public const string COMPRESSED_IS_MODEL = ".ltmi";
        public const string COMPRESSED_ES_PROJECT = ".ltpe";
        public const string COMPRESSED_PACK_PROJECT = ".ltpp";
        public static ProjectFileType GetProjectFileType(string extension) {
            if (extension == NORMAL_PROJECT || extension == NORMAL_MODEL)
                return ProjectFileType.Normal;
            if (extension == COMPRESSED_IS_PROJECT || extension == COMPRESSED_IS_MODEL)
                return ProjectFileType.CompressedIS;
            if (extension == COMPRESSED_ES_PROJECT)
                return ProjectFileType.CompressedES;
            if (extension == COMPRESSED_PACK_PROJECT)
                return ProjectFileType.CompressedPack;
            throw new ArgumentException(String.Format("{0} is not recognizable project extension.", extension));
        }

        public static string GetExtension(ProjectFileType fileType) {
            switch (fileType) {
                case ProjectFileType.Normal: return NORMAL_PROJECT;
                case ProjectFileType.CompressedIS: return COMPRESSED_IS_PROJECT;
                case ProjectFileType.CompressedES: return COMPRESSED_ES_PROJECT;
                case ProjectFileType.CompressedPack: return COMPRESSED_PACK_PROJECT;
            }
            throw new ArgumentException(String.Format("{0} is not valid project file type", fileType));
        }
    }

    public enum ProjectFileType {
        Normal = 0x1, //ltp and ltm               first project file structure and copied spectra files into project directory
        CompressedIS = 0x2, //ltpi and ltmi       (Included Spectra) compressed projecect files and spectra copied into project directory. structure the same as in evp
        CompressedES = 0x3, //ltpe only           (Excluded Spectra) compressed project file, where all documents are defined. spectra are not copied and evr contains original paths to them.
        CompressedPack = 0x4 //ltpp only          compressed pack project file with all documents defined and spectra data                     
                                            //whereas evp, evpc and evpp are portable projects, evpl is locally since spectra data is not included in project directory structure
    }

    [Flags]
    public enum SearchFlags {
        Standard = 0x1,
        IncludeInts = 0x2,
        IncludeSourceContribution = 0x4,
        PromptOnly = 0x8
    }

    public enum SearchMode {
        Inactive,
        Preliminary,
        PreliminaryInts,
        Main
    }

    public enum SearchLevel {
        Global,
        Local,
        Preliminary
    }

    public class AsyncFirstSpectraSearchCompletedEventArgs : AsyncCompletedEventArgs {
        public AsyncFirstSpectraSearchCompletedEventArgs(Exception error, bool canceled, object userState) : base(error, canceled, userState) { }
        public bool SearchAgain { get; set; }
        public ICollection<ISpectrum> Spectra { get; set; }
    }

    public delegate void AsyncFirstSpectraSearchCompletedEventHandler(object sender, AsyncFirstSpectraSearchCompletedEventArgs args);
    public delegate void IndependencyFoundEventHandler(object parameterOwner, IParameter parameter);
    public delegate void IndefiniteMatrixEventHandler(object sender, ISpectrum spectrum, ParameterStatus status);

    public interface IProject {

        double Fit { get; set; }
        string Caption { get; }
        string Path { get; }
        string ProjectFile { get; set; }
        string Name { get; }
        string Description { get; }
        //bool Compressed { get; }
        ProjectFileType FileType { get; }
        SearchMode SearchMode { get; }
        SearchFlags Flags { get; set; }
        bool CalculatedValues { get; set; }
        bool IsBusy { get; }
        string ExperimentalMethodName { get; }
        ISpectraContainer this[string name] { get; }
        ISpectraContainer this[int id] { get; }

        event IndependencyFoundEventHandler IndependencyFound;
        event ProgressChangedEventHandler FirstSpectraSearchProgressChanged;
        event AsyncCompletedEventHandler SearchCompleted;
        event AsyncFirstSpectraSearchCompletedEventHandler FirstSpectraSearchCompleted;
        event ProgressChangedEventHandler SearchProgressChanged;
        event IndefiniteMatrixEventHandler IndefiniteMatrixGot;

        List<ISpectraContainer> Containers { get; }
        //List<IBinding> Bindings { get; set; }
        IBindingsManager BindingsManager { get; }

        ISpectraContainer AddSpectraContainer(ISpectraContainer container);
        ISpectraContainer AddSpectraContainer(string containerXmlFilePath);
        ISpectraContainer CreateContainer(IModel model);
        ISpectraContainer CreateContainer(string name, IModel model, ICollection<string> spectraPaths, ICollection<GroupDefinition> groupsDefinition);
        bool RemoveContainer(ISpectraContainer container);
        Type GetSpectraContainerType();
        void Save(string directoryPath, ProjectFileType fileType);
        void Save(string directoryPath);
        void FirstSpectraSearch(List<ISpectrum> spectra);
        void SeriesSearch(ICollection<string> evelListeners, List<ISpectrum> spectra);

        void SeriesSearchAsync(ICollection<string> evelListeners, List<ISpectrum> spectra);
        void FirstSpectraSearchAsync(List<ISpectrum> spectra);
        void SearchAsyncCancel();

        void RestoreSpectrumStartingValues(ISpectrum spectrum, ParameterStatus status);
        void RestoreParameter(ISpectrum spectrum, ParameterLocation location);

        IParameter GetParameter(string address);
        string GetParameterAddress(IParameter parameter);
        //int findBinding(IParameter parameter, out IBinding binding);
        //void removeBinding(int id);
        //void addBinding(IBinding binding);

        ParameterLocation GetParameterLocation(IParameter parameter);
        IParameter GetParameter(ParameterLocation location);

        //debug
        string SaveObjectState(string filePath);

    }
}
