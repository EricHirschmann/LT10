using System;
using System.Collections.Generic;
using System.Text;
using Evel.interfaces;
using System.Xml;

namespace Evel.engine {
    public class GroupBinding : Binding {

        IProject _parentProject;
        public string[] Groups;
        

        #region Helpers

        public bool ContainsGroup(string name) {
            for (int i = 0; i < Groups.Length; i++)
                if (Groups[i] == name) return true;
            return false;
        }

        public bool ContainsBinding(ISpectraContainer container, string groupName) {
            for (int i = 0; i < Containers.Length; i++)
                if (Containers[i] == container) {
                    return ContainsGroup(groupName);
                }
            return false;
        }

        #endregion Helpers

        #region IBinding Members

        public override IProject Parent {
            get { return this._parentProject; }
        }


        public override void WriteXml(System.Xml.XmlWriter writer) {
            int i;
            writer.WriteStartElement("binding");
            if (HasName)
                writer.WriteAttributeString("name", _name);
            writer.WriteAttributeString("type", "group");
            writer.WriteStartElement("groups");
            for (i = 0; i < Groups.Length; i++) {
                writer.WriteStartElement("group");
                writer.WriteAttributeString("name", Groups[i]);
                writer.WriteEndElement();
            }
            writer.WriteEndElement(); //groups

            writer.WriteStartElement("documents");
            for (i = 0; i < Containers.Length; i++) {
                writer.WriteStartElement("document");
                writer.WriteAttributeString("name", Containers[i].Name);
                writer.WriteEndElement();
            }
            writer.WriteEndElement(); //documents
            writer.WriteEndElement(); //binding
        }

        #endregion

        #region Construction

        public void SetParticipants(List<ISpectraContainer> containers, List<string> groups) {
            this.Groups = new string[groups.Count];
            this.Containers = new ISpectraContainer[containers.Count];
            groups.CopyTo(this.Groups);
            containers.CopyTo(this.Containers);
        }

        public GroupBinding(XmlReader reader, IProject parent, string name) {
            this.Name = name;
            this._parentProject = parent;
            List<string> gr = new List<string>();
            List<ISpectraContainer> sc = new List<ISpectraContainer>();
            bool finished = false;
            while (reader.Read()) {
                switch (reader.Name) {
                    case "groups":
                        while (reader.Read())
                            if (reader.Name == "group")
                                gr.Add(reader.GetAttribute("name"));
                            else {
                                break;
                            }
                        break;
                    case "documents":
                        while (reader.Read())
                            if (reader.Name == "document")
                                sc.Add(parent[reader.GetAttribute("name")]);
                            else {
                                break;
                            }
                        break;
                    default: finished = true; break;
                }
                if (finished) break;
            }
            SetParticipants(sc, gr);
            adjustComponentSizes();
        }

        public GroupBinding(List<ISpectraContainer> documents, List<string> groupNames, IProject parent, string name) {
            this.Name = name;
            SetParticipants(documents, groupNames);
            adjustComponentSizes();
        }

        private void adjustComponentSizes() {
            int i, j, k, biggestContainerId = 0;
            for (i=0; i<Groups.Length; i++) {
                for (j = 1; j < Containers.Length; j++)
                    if (Containers[j].Spectra[0].Parameters[Groups[i]].Components.Size > Containers[biggestContainerId].Spectra[0].Parameters[Groups[i]].Components.Size)
                        biggestContainerId = j;
                for (j = 0; j < Containers.Length; j++) {
                    if (biggestContainerId == j) continue;
                    for (k = 0; k < Containers[j].Spectra.Count; k++)
                        if (Containers[j].Spectra[k].Parameters[Groups[i]].Components.Size != Containers[biggestContainerId].Spectra[0].Parameters[Groups[i]].Components.Size)
                            Containers[j].Spectra[k].Parameters[Groups[i]].Components.Size = Containers[biggestContainerId].Spectra[0].Parameters[Groups[i]].Components.Size;
                }

            }
        }

        #endregion construction

        #region IDisposable Members

        public override void Dispose() {
            //create grouptab pages in each document and remove shared group tab pages

        }

        #endregion
    }
}
