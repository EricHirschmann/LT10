using System;
using Evel.share;
using System.Collections.Generic;

namespace Evel.interfaces {

    public class ParameterDefaultPattern {
        public string name;
        public double multi;
        public double value;
        public double min;
        public double max;
        public double defaultValue;
        public int groupKind;
        public bool positive;
        public ParameterDefaultPattern() {
            name = "";
            multi = 0;
            value = 0;
            min = Double.NegativeInfinity;
            max = Double.PositiveInfinity;
            defaultValue = Double.NaN;
            groupKind = 0;
            positive = false;
        }
    }
  
    [Flags]
    public enum CheckOptions {
        RefreshDelta = 0x01,
        SetDefaultValues = 0x02,
        NoReferencedDelta = 0x04,
        None = 0x08
    }

    public delegate double CalculateParameterValueHandler(IComponent component, IParameter parameter);

    public interface IModel {

        //CalculateParameterValueHandler CalculateParameterValue { get; }
        string Name { get; }
        string DeltaFileName { get; }
        string Description { get; }
        GroupDefinition[] GroupsDefinition { get; set; }
        Type SpectrumType { get; }
        Type ProjectType { get; }
        HashSet<ParameterDefaultPattern> defaultPatterns { get; }

        //void convert(ref ValuesDictionary[] dictionary, IParameterSet parameters);
        void convert(List<ICurveParameters> curveParameters, IParameterSet parameters);
        void checkParameter(IParameter parameter, ISpectrum spectrum, CheckOptions options);
        void loadParameterDefaultPatterns();

        /// <summary>
        /// 
        /// </summary>
        /// <param name="status"></param>
        /// <param name="parameters"></param>
        /// <param name="includeFlags">
        ///     includeFlags[0] - includeInts
        ///     includeFlags[1] - includeContrib
        ///     includeFlags[2] - promptOnly
        /// </param>
        /// <returns></returns>
        //List<IParameter> getParameters(ParameterStatus status, IParameterSet parameters, bool[] includeFlags);
        //IEnumerable<IParameter> getParameters(ParameterStatus status, IParameterSet parameters, bool[] includeFlags, CheckOptions co);
        IEnumerable<IParameter> getParameters(ParameterStatus status, ISpectrum spectrum, bool[] includeFlags, CheckOptions co);

        /// <summary>
        /// sets parameters in buffer if buffer != null and returns searchable parameter count in parameter set
        /// </summary>
        /// <param name="ps">parameter set</param>
        /// <param name="bufferStart">position in parameters buffer array. miningless if a == null</param>
        /// <param name="a">parameters buffer array. if a is not null function copies parameters into that buffer
        /// beginning from bufferStart</param>
        /// <param name="ai">constraints buffer array. if not null function sets true if parameter at
        /// particular position should be included into search, false otherwise.</param>
        /// <param name="f">indlude flags determining conditions under which parameter is included into search
        /// or not. must not be null if ai != null
        ///     includeFlags[0] - includeInts
        ///     includeFlags[1] - includeContrib
        ///     includeFlags[2] - promptOnly
        /// </param>
        /// <param name="status">parameters with this status will be included into search. meaningless if ai == null</param>        
        /// <returns>searchable parameer count in ps parameter set</returns>
        int setparams(IParameterSet ps, IParameter[] a, bool[] ai, bool[] f, ParameterStatus status, ISpectrum spectrum);
    }



}
