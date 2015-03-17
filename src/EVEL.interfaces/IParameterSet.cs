using System;
using System.Collections.Generic;
using System.Text;

namespace Evel.interfaces {

    public interface IParameterSet {

        IGroup this[int index] { get; }
        IGroup this[string groupName] { get; }

        int GroupCount { get; }

        /// <returns>Added group</returns>
        IGroup addGroup(IGroup group);

        /// <returns>true if group has been removed</returns>
        bool removeGroup(IGroup group);

        IEnumerator<IGroup> GetEnumerator();

        bool ContainsGroup(string groupName);

        int IndexOf(IGroup group);

        int BufferStart { get; set; }
        int BufferStop { get; set; }
    }
}
