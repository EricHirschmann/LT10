using System;
using System.Collections.Generic;
using System.Text;

namespace Evel.interfaces {

    public interface ISpectraContainer {

        string Name { get; set; }
        IModel Model { get; set; }
        List<ISpectrum> Spectra { get; }
        bool Enabled { get; set; }
        int[] Data { get; }
        double[] Weights { get; }
        
        IProject ParentProject { get; }

        List<IParameter> getParameters(ParameterStatus status, bool[] includeFlags, CheckOptions co);

        //generates differences between theory and experience (getDiff in MSB)
        bool getEvaluationArray(object target, double[] diffs);
        double[] getTheoreticalSpectrum(ISpectrum spectrum);
        void getTheoreticalSpectrum(ISpectrum spectrum, ref float[][] curves, 
            ref string[] curveNames, ref float[] differences, bool intensitiesFromSearch);

        void Save(System.Xml.XmlWriter writer, ProjectFileType fileType);
        //void Save(string filePath, bool includeData, bool compressed);

        ISpectrum CreateSpectrum(System.Xml.XmlReader spectrumReader, int bufferStart);
        ISpectrum CreateSpectrum(string path, int bufferStart);

        void AddSpectrum(ISpectrum spectrum, CopyOptions copyOpts);
        void RemoveSpectrum(ISpectrum spectrum);

        IParameter GetParameter(string address);
        string GetParameterAddress(IParameter parameter);

        int ResizeBuffer(int newSize);
        void ResetArrays();

        /// <summary>
        /// Gets all parameters with parameter's name, component id and group within all spectra
        /// </summary>
        /// <param name="parameter"></param>
        /// <returns></returns>
        IEnumerable<IParameter> GetParameters(IParameter parameter, bool sameReferenceGroup);

    }
}
