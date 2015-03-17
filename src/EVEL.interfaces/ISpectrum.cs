using System;
using System.Collections.Generic;
using System.Text;
using System.Xml;

namespace Evel.interfaces {

    [Flags]
    public enum CopyOptions {
        Value = 0x1,
        Status = 0x2,
        ReferenceGroup = 0x4
    }

    [Flags]
    public enum PrepareOptions {
        None = 0x0,
        GlobalArea = 0x1,
        PromptIntensities = 0x2,
        ComponentIntensities = 0x4,
        All = 0xFFFF
    }

    public interface ISpectrum : IDisposable {

        string Name { get; set; }
        string Path { get; set; }
        string Title { get; set; }
        double Fit { get; set; }
        int RangeArea { get; }
        int Statistic { get; }
        //int[] ExperimentalSpectrum { get; set; }
        //double[] Weights { get; }
        IParameterSet Parameters { get; set; }
        ISpectraContainer Container { get; }
        double[] Constants { get; }
        //int EffectEndChannel { get; }
        int BufferStartPos { get; set; }
        int BufferEndPos { get; set; }
        int DataLength { get; }
        int[] Thresholds { get; }
        int ThresholdsCompression { get; }

        void setThresholds();

        void writeToXml(XmlWriter writer, bool includeData, bool fullPath);
        void writeData(string path);

        /// <summary>
        /// Copies spectra from parameters and resizes all components if single component size are the same
        /// in both models
        /// </summary>
        /// <param name="parameters"></param>
        void copy(IParameterSet parameters, GroupDefinition definition, CopyOptions options);
        void copy(IParameterSet parameters, ParameterStatus status, bool copyAll);
        void copy(XmlReader reader, ParameterStatus status);
        void copy(ISpectrum source, string groupName, CopyOptions options);
        

        void prepareToSearch(SearchLevel sl, PrepareOptions po);
        void normalizeAfterSearch(SearchLevel sl, PrepareOptions po, bool flagOnly);

        //IGroup getGroup(XmlReader reader, List<IGroup> groups);
        //IEnumerable<IParameter> getParameters(ParameterStatus status);

    }
}
