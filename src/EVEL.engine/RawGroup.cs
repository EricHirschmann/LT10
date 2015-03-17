using System;
using System.Collections.Generic;
using System.Text;
using Evel.interfaces;

namespace Evel.engine {

    public class RawGroup : IGroup {

        protected Components _components;
        protected GroupDefinition _definition;
        protected ISpectrum _owningSpectrum;
        protected IComponent _groupUniqueParameters;

        protected RawGroup() { }

        public RawGroup(GroupDefinition definition, ISpectrum owningSpectrum) {
            this._definition = definition;
            this._components = CreateComponents(0);
            this._owningSpectrum = owningSpectrum;
            CreateUniqueComponent(definition);
        }

        protected void CreateUniqueComponent(GroupDefinition definition) {
            int uniqueCount = 0;
            foreach (ParameterDefinition def in definition.parameters)
                if ((def.Properties & ParameterProperties.GroupUnique) == ParameterProperties.GroupUnique)
                    uniqueCount++;
            ParameterDefinition[] uniqueParameters = new ParameterDefinition[uniqueCount];
            int uniqueId = 0;
            foreach (ParameterDefinition def in definition.parameters)
                if ((def.Properties & ParameterProperties.GroupUnique) == ParameterProperties.GroupUnique)
                    uniqueParameters[uniqueId++] = def;
            _groupUniqueParameters = new Component(uniqueParameters, true, this);
        }

        #region IGroup Members

        public IComponents Components {
            get { return this._components; }
        }

        public GroupDefinition Definition {
            get { return this._definition; }
            set {
                int c, p;
                this._definition = value;
                Components newcomps = CreateComponents(this._components.Size);
                for (c = 0; c < newcomps.Size && c < this._components.Size; c++)
                    for (p = 0; p < newcomps[c].Size && p < this._components[c].Size; p++) {
                        newcomps[c][p].Value = this._components[c][p].Value;
                        newcomps[c][p].Status = this._components[c][p].Status;
                    }
                if (value.componentCount != 0 && newcomps.Size != value.componentCount)
                    newcomps.Size = value.componentCount;
                this._components.Dispose();
                this._components = newcomps;
                CreateUniqueComponent(value);
            }
        }

        protected virtual Components CreateComponents(int count) {
            return new Components(this, count);
        }

        public ISpectrum OwningSpectrum {
            get { return this._owningSpectrum; } 
        }

        public IComponent GroupUniqueParameters {
            get { return this._groupUniqueParameters; }
        }

        public IParameter this[int uniqueParameterIndex] {
            get { return this._groupUniqueParameters[uniqueParameterIndex]; }
        }

        public IParameter this[string uniqueParameterName] {
            get { return this._groupUniqueParameters[uniqueParameterName]; }
        }

        public virtual IParameter GetParameter(string address) {
            string[] coords = address.Split(ProjectBase.AddressDelimiters, StringSplitOptions.RemoveEmptyEntries);
            string parameterName = coords[3];
            int compId = 0;
            IParameter parameter = null;
            if (coords.Length==5)  // && coords[coords.Length-1].Contains("#")) {
                compId = Int32.Parse(coords[4])-1;
            if (Components[compId].ContainsParameter(parameterName))
                parameter = Components[compId][parameterName];
            else
                parameter = GroupUniqueParameters[parameterName];
            return parameter;
        }

        public string GetParameterAddress(IParameter parameter) {
            string parameterName = parameter.Definition.Name;
            if (parameter.Parent is IComponent) {
                IComponent parentComponent = (IComponent)parameter.Parent;
                if (parentComponent.Parent is IComponents)
                    if (((IComponents)parentComponent.Parent).Size > 1) {

                        parameterName = String.Format("{0}#{1}", parameterName, ((IComponents)parentComponent.Parent).IndexOf(parentComponent) + 1);
                    }
            }
            return parameterName;
        }

        #endregion

        public override string ToString() {
            return Definition.name;
        }

        #region IDisposable Members

        public virtual void Dispose() {
            this._components.Dispose();
        }

        #endregion
    }
}
