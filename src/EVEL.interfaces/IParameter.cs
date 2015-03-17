using System;
using System.Collections.Generic;
using System.Text;
using MathExpressions;

namespace Evel.interfaces {

    public delegate double User2SearchConversion(ref double value, ref double min, ref double max);
    public delegate double Search2UserConversion(ref double value, ref double min, ref double max);

    [Flags]
    public enum ParameterStatus {
        Local = 0x01,
        Common = 0x02,
        Free = 0x04,
        Fixed = 0x08,
        None = 0x10,
        Binding = 0x20
    }

    [Flags]
    public enum ParameterProperties {
        Readonly = 0x1,
        IsDependency = 0x2,
        Hidden = 0x4,
        KeyValue = 0x8,
        GroupUnique = 0x10,
        Unsearchable = 0x20,
        ComponentIntensity = 0x40
    }

    public struct ParameterDefinition {
        public CalculateParameterValueHandler CalculateParameterValue;
        public string Name;
        public string Header;
        public ParameterStatus BindedStatus;
        public ParameterProperties Properties;
        public ParameterStatus DefaultStatus;
        public ParameterDefinition(string Name, ParameterStatus DefaultStatus)
            : this(Name, Name, ParameterStatus.None, DefaultStatus, null, 0) { }

        public ParameterDefinition(string Name, string Header, ParameterStatus DefaultStatus)
            : this(Name, Header, ParameterStatus.None, DefaultStatus, null, 0) { }

        public ParameterDefinition(string Name, ParameterProperties Properties, ParameterStatus BindedStatus)
            : this(Name, Name, BindedStatus, BindedStatus, null, Properties) { }

        public ParameterDefinition(string Name, string Header, ParameterProperties properties, ParameterStatus BindedStatus, CalculateParameterValueHandler CalculateParameterValue)
            : this(Name, Header, BindedStatus, ParameterStatus.Local | ParameterStatus.Fixed, CalculateParameterValue, properties) { }

        public ParameterDefinition(string Name, ParameterProperties properties, ParameterStatus BindedStatus, CalculateParameterValueHandler CalculateParameterValue)
            : this(Name, Name, BindedStatus, ParameterStatus.Local | ParameterStatus.Fixed, CalculateParameterValue, properties) { }

        public ParameterDefinition(string Name, string Header)
            : this(Name, Header, ParameterStatus.None, ParameterStatus.Local | ParameterStatus.Fixed, null, 0) { }

        public ParameterDefinition(string Name) 
            : this(Name, Name, ParameterStatus.None, ParameterStatus.Local | ParameterStatus.Fixed, null, 0) { }

        public ParameterDefinition(string Name, string Header, ParameterStatus DefaultStatus, ParameterProperties Properties)
            : this(Name, Header, ParameterStatus.None, DefaultStatus, null, Properties) { }

        public ParameterDefinition(string Name, ParameterStatus DefaultStatus, ParameterProperties Properties) 
            : this(Name, Name, ParameterStatus.None, DefaultStatus, null, Properties) { }
        public ParameterDefinition(string Name, string Header, ParameterStatus BindedStatus, ParameterStatus DefaultStatus, 
            CalculateParameterValueHandler CalculateParameterValue, ParameterProperties properties) {
            this.Name = Name;
            if (Header == Name)
                this.Header = String.Format("[p text='{0}']", Name);
            else
                this.Header = Header;
            this.BindedStatus = BindedStatus;
            this.CalculateParameterValue = CalculateParameterValue;
            this.DefaultStatus = DefaultStatus;
            this.Properties = properties;
        }
        public override string ToString() {
            return Name;
        }
    }

    public interface IParameter {

        event EventHandler OnValueChange;
        //string Name { get; }
        //IComponent OwningComponent { get; }
        object Parent { get; }
        ParameterDefinition Definition { get; }
        double Value { get; set; }
        double SearchValue { get; set; }
        double Error { get; set; }
        double SearchError { get; set; }
        double Delta { get; set; }
        double SearchDelta { get; set; }
        double Minimum { get; set; }
        double Maximum { get; set; }
        bool HasReferenceValue { get; }
        short ReferencedValues { get; set; }
        ParameterStatus Status { get; set; }
        IParameter ReferencedParameter { get; set; }
        bool BindingParameter { get; }
        //Expression Expression { get; set; }
        int ReferenceGroup { get; set; }
        void Backup();
        void SaveBackup();

    }
}
