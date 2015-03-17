using System;
using System.Collections.Generic;
using System.Text;
using Evel.interfaces;

namespace Evel.engine {
    public class Component : IComponent {

        private List<IParameter> _parameters;
        private object _parent;

        public object Parent {
            get { return this._parent; }
        }

        public IParameter this[int index] {
            get { return _parameters[index]; }
        }

        public IParameter this[string paramName] {
            get {
                foreach (IParameter par in _parameters)
                    if (String.Equals(par.Definition.Name, paramName, StringComparison.CurrentCultureIgnoreCase))
                        return par;
                throw new Exception(String.Format("Component doesn't contain parameter named \"{0}\"", paramName));
            }
        }

        public IEnumerator<IParameter> GetEnumerator() {
            return _parameters.GetEnumerator();
        }

        public int Size {
            get { return _parameters.Count; }
        }

        protected Component(object parent) {
            this._parameters = new List<IParameter>();
            this._parent = parent;
        }

        //public Component(string[] parameterNames) : this() {
        //    foreach (string parameterName in parameterNames) {
        //        _parameters.Add(new Parameter(parameterName));
        //    }
        //}

        public Component(ParameterDefinition[] parameters, bool createUnique, object parent)
            : this(parent) {
            foreach (ParameterDefinition parameterDef in parameters) {
                if ((parameterDef.Properties & ParameterProperties.GroupUnique) != ParameterProperties.GroupUnique || createUnique)
                    _parameters.Add(new Parameter(parameterDef, this));
            }
        }

        public bool ContainsParameter(string parameterName) {
            foreach (IParameter par in _parameters)
                if (String.Equals(par.Definition.Name, parameterName, StringComparison.CurrentCultureIgnoreCase))
                    return true;
            return false;
        }


        #region IDisposable Members

        public void Dispose() {
            int p;
            for (p = 0; p < this._parameters.Count; p++) {
                if (this._parameters[p].HasReferenceValue)
                    this._parameters[p].ReferencedParameter.ReferencedValues--;
            }
        }

        #endregion
    }
}
