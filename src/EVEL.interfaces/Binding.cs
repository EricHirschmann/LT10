using System;
using System.Collections.Generic;
using System.Text;
using System.Xml;

namespace Evel.interfaces {
    public abstract class Binding : IDisposable {

        public ISpectraContainer[] Containers;
        protected string _name = String.Empty;
        public abstract IProject Parent { get; }
        public abstract void WriteXml(XmlWriter writer);
        public abstract void Dispose();
        public string Name { 
            get { return this._name; }
            set {
                if (value == null || value == "") this._name = String.Empty;
                else this._name = value;
            }
        }
        public bool HasName {
            get { return this._name != null && this._name != String.Empty; }
        }

        public bool ContainsContainer(ISpectraContainer container) {
            for (int i = 0; i < this.Containers.Length; i++)
                if (this.Containers[i] == container) return true;
            return false;
        }
    }
}
