using System;
using System.Collections.Generic;
using System.Text;
using Evel.share;

namespace Evel.interfaces {

    public enum ComponentIntsState {
        Normed,
        PreparedToSearch
    }

    public interface IComponents : IDisposable {

        IComponent this[int index] { get; }

        /// <summary>
        /// gets or sets Component count
        /// </summary>
        int Size { get; set; }

        int IndexOf(IComponent component);

        IGroup Parent { get; }

        IEnumerator<IComponent> GetEnumerator();

        ComponentIntsState IntensitiesState { get; set; }

        /// <summary>
        /// Sort components by id-th component parameter
        /// </summary>
        /// <param name="id"></param>
        void Sort(int id);

    }
}
