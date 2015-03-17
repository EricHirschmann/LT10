using System;
using System.Collections.Generic;
using System.Text;

namespace Evel.interfaces {

    
    public class StatusChangeEventArgs : EventArgs {
        public IGroup group;
        public ISpectrum spectrum;
        public IParameter parameter;
        public ParameterStatus status;
        public List<ISpectrum> spectra;
        public StatusChangeEventArgs(IGroup group, ISpectrum spectrum, List<ISpectrum> spectra, IParameter parameter, ParameterStatus status)
            : base() {
            this.group = group;
            this.spectrum = spectrum;
            this.parameter = parameter;
            this.status = status;
            this.spectra = spectra;
        }
    }

    public delegate void DefaultComponentsFormHandler(IGroup sender, ISpectrum spectrum, EventArgs args);
    public delegate void StatusChangeHandler(Object sender, StatusChangeEventArgs args);

    /// <summary>
    /// "Contributet" groups contains contribution parameter 
    /// which defines contribution of components this group is owner of.
    /// "Parameters" - group of parameters without any mathematical relations. 
    /// Parameters in this kind of groups are only logicaly related.
    /// "Hidden" - same as raw. Parameters in such groups will not be editable.
    /// "CalcContribution" - contribution of this group is calculated.
    /// </summary>
    [Flags]
    public enum GroupType {
        Contributet = 0x01,
        Raw = 0x02,
        Hidden = 0x04,
        CalcContribution = 0x08,
        SpectrumConstants = 0x10
    }

    public struct GroupDefinition {
        private GroupType _type;
        public int kind;
        public string name;
        public int defaultSortedParameter;
        public GroupType Type {
            get { return this._type; }
            set {
                //either contributet or raw flag allowed
                if ((value & (GroupType.Contributet | GroupType.Raw)) == (GroupType.Contributet | GroupType.Raw))
                    throw new ArgumentException("Group cannot be Contributet and Raw");

                //CalcContribution flag cannot cast without Contributet flag
                if (((value & GroupType.CalcContribution) == GroupType.CalcContribution) && ((value & GroupType.Contributet) != GroupType.Contributet))
                    throw new ArgumentException("Groups with calculated contribution (CalcContribution) must be Contributet groups");
                
                //Contributet cannot be Hidden
                if ((value & (GroupType.Contributet | GroupType.Hidden)) == (GroupType.Contributet | GroupType.Hidden))
                    throw new ArgumentException("Contributet groups cannot be Hidden");

                //SpectrumConstants is flag for Raw
                if (((value & GroupType.SpectrumConstants) == GroupType.SpectrumConstants) && ((value & GroupType.Raw) != GroupType.Raw))
                    throw new ArgumentException("SpectrumConstants cannot cast without Raw flag");

                //SpectrumConstants may have Hidden flag only
                if (((value & GroupType.SpectrumConstants) == GroupType.SpectrumConstants) &&
                    !((value == (GroupType.SpectrumConstants | GroupType.Raw)) || 
                    (value == (GroupType.SpectrumConstants | GroupType.Hidden | GroupType.Raw))))
                    throw new ArgumentException("SpectrumConstants group may cast either alone or with Hidden flag. Any other configurations are forbidden");
                this._type = value;
            }
        }
        //public string[] parameterNames;
        //public bool[] fixedParameters;
        public ParameterDefinition[] parameters;
        //public bool fixedComponentCount;
        //public bool allowMultiple;

        public byte componentCount; //0 - unlimited
        public DefaultComponentsFormHandler SetDefaultComponents;
        public StatusChangeHandler StatusChanged;

        public static bool operator ==(GroupDefinition o1, GroupDefinition o2) {
            return o1.parameters.Length == o2.parameters.Length && o1._type == o2._type && o1.kind == o2.kind && o1.name == o2.name && (o1.componentCount == o2.componentCount);
        }

        public static bool operator !=(GroupDefinition o1, GroupDefinition o2) {
            return o1.parameters.Length != o2.parameters.Length || o1._type != o2._type || o1.kind != o2.kind || o1.name != o2.name || (o1.componentCount != o2.componentCount);
        }

        public override int GetHashCode() {
            return base.GetHashCode();
        }

        public override bool Equals(object obj) {
            return base.Equals(obj);
        }

    }

    public interface IGroup : IDisposable {

        IComponents Components { get; }
        IComponent GroupUniqueParameters { get; }
        GroupDefinition Definition { get; set; }
        ISpectrum OwningSpectrum { get; }
        IParameter this[string uniqueParameterName] { get; }
        IParameter this[int uniqueParameterIndex] { get; }

        IParameter GetParameter(string address);
        string GetParameterAddress(IParameter parameter);

    }
}
