using System;
using System.Collections.Generic;
using Evel.interfaces;

namespace Evel.engine {
    public class Components : IComponents {

        protected List<IComponent> _components;

        protected IGroup _parentGroup;

        //public int ComponentSize;

        protected ComponentIntsState _intenstitiesState;

        public ComponentIntsState IntensitiesState {
            get {
                if (_components.Count > 0) {
                    if (_components[0][0].HasReferenceValue)
                        return ((Components)(((Component)_components[0][0].ReferencedParameter.Parent).Parent))._intenstitiesState;
                    else
                        return this._intenstitiesState;
                } else
                    return ComponentIntsState.Normed;
            }
            set {
                if (_components.Count > 0) {
                    if (_components[0][0].HasReferenceValue)
                        ((Components)(((Component)_components[0][0].ReferencedParameter.Parent).Parent))._intenstitiesState = value;
                    this._intenstitiesState = value;
                }
            }
        }

        public IGroup Parent {
            get { return this._parentGroup; }
        }

        public Components(IGroup parentGroup) {
            this._intenstitiesState = ComponentIntsState.Normed;
            this._parentGroup = parentGroup;
            this._components = new List<IComponent>();
        }

        public Components(IGroup parentGroup, int componentCount)
            : this(parentGroup) {
            this.Size = componentCount;
        }

        public IComponent this[int index] {
            get {
                if (index >= _components.Count)
                    throw new Exception(String.Format("Index is out of range in component owned by group \"{0}\"\n(Attempt to get component with index {1}, while component count in group \"{0}\" is {2})", _parentGroup.Definition.name, index, _parentGroup.Components.Size));
                return _components[index]; }
        }

        public int IndexOf(IComponent component) {
            return _components.IndexOf(component);
        }

        public int Size {
            get { return _components.Count; }
            set {
                if (value > _components.Count) {
                    while (value > _components.Count) {
                        _components.Add(createNewComponent(_parentGroup.Definition.parameters));
                    }
                } else {
                    while (value < _components.Count) {
                        IComponent c = _components[_components.Count - 1];
                        _components.Remove(c);
                        c.Dispose();
                    }
                }
            }
        }

        public IEnumerator<IComponent> GetEnumerator() {
            return this._components.GetEnumerator();
        }

        protected virtual IComponent createNewComponent(ParameterDefinition[] parameters) {
            return new Component(parameters, false, this);
        }

        protected void QSortChange(int c1, int c2) {
            double remValue;
            ParameterStatus remStatus;
            for (int i = 0; i < this._components[c1].Size; i++) {
                remValue = this._components[c1][i].Value;
                remStatus = this._components[c1][i].Status;
                this._components[c1][i].Value = this._components[c2][i].Value;
                this._components[c2][i].Value = remValue;
                if (!(c1 == 0 && i == 0))
                    this._components[c1][i].Status = this._components[c2][i].Status;
                if (!(c1 == 0 && i == 0)) 
                    this._components[c2][i].Status = remStatus;
                
            }
        }

        /// <summary>
        /// Recursively sorts components by sortId'th parameter
        /// </summary>
        /// <param name="left">first index of the array to be sorted</param>
        /// <param name="right">last index of the array to be sorted</param>
        /// <param name="sortId">index of parameter by which components are sorted</param>
        private void QSort(int left, int right, int sortId) {
            if (left < right) {
                int m = left;
                for (int i = left + 1; i < right; i++) {
                    if (!(this._components[i][sortId].HasReferenceValue || this._components[left][sortId].HasReferenceValue)) {
                        //(this._components[i][sortId].ReferencedValues == 0 && this._components[left][sortId].ReferencedValues == 0)) {
                        if (this._components[i][sortId].Value < this._components[left][sortId].Value)
                            QSortChange(++m, i);
                        //} else
                        //    m++;
                    }
                }
                if (left != m)
                    QSortChange(left, m);
                QSort(left, m, sortId);
                QSort(m + 1, right, sortId);
            }
        }

        public virtual void Sort(int id) {
            //_components.Sort(new Comparison<IComponent>(delegate(IComponent c1, IComponent c2) {
            //    return c1[id].Value.CompareTo(c2[id].Value);
            //}));
            //if ((_components[0][0].Definition.Properties & ParameterProperties.ComponentIntensity) > 0) {
            //    _components[0][0].Status = ParameterStatus.Local | ParameterStatus.Free;
            //    _components[0][0].Error = 0;
            //}
            QSort(0, this._components.Count, id);
        }

        #region IDisposable Members

        public virtual void Dispose() {
            int c;
            for (c = 0; c < this._components.Count; c++)
                this._components[c].Dispose();
        }

        #endregion
    }
}
