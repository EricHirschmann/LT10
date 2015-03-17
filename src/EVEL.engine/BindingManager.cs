using System;
using System.Collections.Generic;
using System.Text;
using Evel.interfaces;

namespace Evel.engine {
    public class BindingsManager : List<Binding>, IBindingsManager {

        private IProject _parent;

        public BindingsManager(IProject parent)
            : base() {
            this._parent = parent;
        }

        public IEnumerable<Binding> GetBindings(ISpectraContainer container) {
            for (int i = 0; i < this.Count; i++)
                if (this[i].ContainsContainer(container))
                    yield return this[i];
        }

        #region List

        new public void Add(Binding binding) {
            base.Add(binding);
            if (binding is ParameterBinding)
                foreach (IParameter parameter in ((ParameterBinding)binding).Parameters) {
                    ParameterLocation pl = _parent.GetParameterLocation(parameter);
                    ISpectraContainer container = _parent.Containers[pl.docId];
                    foreach (IParameter p in container.GetParameters(parameter, true)) {
                        p.Status = ((ParameterBinding)binding).Source.Status;
                        p.ReferencedParameter = ((ParameterBinding)binding).Source;
                    }
                }
        }

        new public void Remove(Binding binding) {
            this.RemoveAt(IndexOf(binding));
        }

        new public void RemoveAt(int id) {
            Binding binding = this[id];
            if (binding is ParameterBinding) {
                foreach (IParameter parameter in ((ParameterBinding)binding).Parameters) {
                    ParameterLocation pl = _parent.GetParameterLocation(parameter);
                    ISpectraContainer container = _parent.Containers[pl.docId];
                    IParameter topParameter = null;
                    for (int specId = 0; specId < container.Spectra.Count; specId++) {
                        pl.specId = specId;
                        IParameter p = _parent.GetParameter(pl);
                        p.Status &= ~ParameterStatus.Binding;
                        p.ReferencedParameter = topParameter;
                        if (specId == 0) topParameter = p;
                    }
                }
            }
            binding.Dispose();
            base.RemoveAt(id);
        }

        public bool Contains(ISpectraContainer container, string groupName) {
            for (int i = 0; i < Count; i++)
                if (this[i] is GroupBinding)
                    if (((GroupBinding)this[i]).ContainsBinding(container, groupName))
                        return true;
            return false;
        }

        public bool Contains(ISpectraContainer container) {
            for (int i = 0; i < Count; i++)
                if (this[i] is GroupBinding)
                    if (((GroupBinding)this[i]).ContainsContainer(container))
                        return true;
            return false;
        }

        #endregion List

    }
}
