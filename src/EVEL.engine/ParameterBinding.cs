using System;
using System.Collections.Generic;
using System.Text;
using Evel.interfaces;
using System.Xml;

namespace Evel.engine {
    public class ParameterBinding : Binding {

        protected IProject _parentProject;

        private IParameter[] parameters;
        private IParameter source;

        public override IProject Parent {
            get { return this._parentProject; }
        }

        public IParameter[] Parameters {
            get { return this.parameters; }
        }

        public IParameter Source {
            get { return source; }
        }

        public ParameterBinding(List<IParameter> parameters, IProject parent, string name) {
            this.parameters = null;
            this.Name = name;
            this._parentProject = parent;
            setParameters(parameters);
        }

        public ParameterBinding(XmlReader reader, IProject parent, string name) {
            this.parameters = null;
            this.Name = name;
            this._parentProject = parent;
            List<IParameter> bp = new List<IParameter>();
            while (reader.Read()) {
                if (reader.Name == "parameter" && reader.HasAttributes) {
                    while (reader.MoveToNextAttribute()) {
                        switch (reader.Name) {
                            case "address":
                                try
                                {
                                    bp.Add(parent.GetParameter(reader.Value));
                                }
                                catch (Exception) { }
                                break;
                        }
                    }
                    reader.MoveToElement();
                } else break;
            }
            setParameters(bp);
        }

        private void setContainers() {
            List<ISpectraContainer> tcontainers = new List<ISpectraContainer>();
            ISpectraContainer container;
            for (int i = 0; i < this.parameters.Length; i++) {
                container = this._parentProject.Containers[this._parentProject.GetParameterLocation(this.parameters[i]).docId];
                if (!tcontainers.Contains(container))
                    tcontainers.Add(container);
            }
            this.Containers = new ISpectraContainer[tcontainers.Count];
            tcontainers.CopyTo(this.Containers);
        }

        public void setParameters(List<IParameter> parameters) {
            int i;
            //release parameters if there are already some
            if (this.parameters != null) {
                for (i = 0; i < this.parameters.Length; i++) {
                    if (!parameters.Contains(this.parameters[i])) {
                        this.parameters[i].ReferencedParameter = null;
                        this.parameters[i].Status &= ~ParameterStatus.Binding;
                    }
                }
            } else
                this.source = new Parameter(parameters[0].Definition, this);

            //set new parameter set
            this.parameters = new IParameter[parameters.Count];
            for (i = 0; i < parameters.Count; i++) {
                //parameters[i].BindingParameter = true;
                parameters[i].Status |= ParameterStatus.Binding;
                this.parameters[i] = parameters[i];
                //if (i > 0)
                if (i == 0) {
                    source.Value = parameters[0].Value;
                    source.Status = parameters[0].Status;
                }
                parameters[i].ReferencedParameter = source;
            }
            setContainers();
        }

        public override void Dispose() {
            foreach (IParameter parameter in parameters) {
                parameter.ReferencedParameter = null;
                parameter.Status &= ~ParameterStatus.Binding;
            }
        }

        public bool ContainsParameter(IParameter parameter) {
            foreach (IParameter p in parameters)
                if (p == parameter) return true;
            return false;
        }

        public override void WriteXml(XmlWriter writer) {
            writer.WriteStartElement("binding");
            if (HasName)
                writer.WriteAttributeString("name", _name);
            writer.WriteAttributeString("type", "parameter");
            foreach (IParameter p in this.parameters) {
                writer.WriteStartElement("parameter");
                writer.WriteAttributeString("address", _parentProject.GetParameterAddress(p));
                writer.WriteEndElement(); //parameter
            }
            writer.WriteEndElement(); //binding
        }

    }
}
