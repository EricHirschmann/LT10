using System;
using System.Collections.Generic;
using System.Text;
using Evel.interfaces;

namespace Evel.engine {
    public class ParameterSet : IParameterSet {

        private List<IGroup> _groups;
        private int _bufferStart, _bufferStop;

        #region IParameterSet Members

        public int BufferStart {
            get { return this._bufferStart; }
            set { this._bufferStart = value; }
        }

        public int BufferStop {
            get { return this._bufferStop; }
            set { this._bufferStop = value; }
        }

        public IEnumerator<IGroup> GetEnumerator() {
            return _groups.GetEnumerator();
        }

        public IGroup addGroup(IGroup group) {
            _groups.Add(group);
            return group;
        }

        public bool removeGroup(IGroup group) {
            return _groups.Remove(group);
        }

        public IGroup this[string groupName] {
            get {
                for (int i = 1; i < _groups.Count; i++) {
                    if (String.Equals(_groups[i].Definition.name, groupName, StringComparison.CurrentCultureIgnoreCase))
                        return _groups[i];
                }
                if (String.Equals(_groups[0].Definition.name, groupName, StringComparison.CurrentCultureIgnoreCase))
                    return _groups[0];
                throw new ArgumentException(String.Format("Parameter set doesn't contain group named \"{0}\"", groupName));
            }
        }

        public bool ContainsGroup(string groupName) {
            for (int i = 0; i < _groups.Count; i++) 
                if (String.Equals(_groups[i].Definition.name, groupName, StringComparison.CurrentCultureIgnoreCase))
                    return true;
            return false;
        }

        public IGroup this[int index] {
            get {
                try {
                    return _groups[index];
                } catch {
                    return null;
                }
            }
        }

        public int GroupCount {
            get {
                return _groups.Count;
            }
        }

        public int IndexOf(IGroup group) {
            return _groups.IndexOf(group);
        }

        #endregion

        public ParameterSet() {
            _groups = new List<IGroup>();
        }
    }
}
