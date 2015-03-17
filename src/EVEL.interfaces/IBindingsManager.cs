using System;
using System.Collections.Generic;
using System.Text;

namespace Evel.interfaces {
    public interface IBindingsManager : IList<Binding> {

        //void removeBinding(int id);
        //void addBinding(IBinding binding);

        bool Contains(ISpectraContainer container, string groupName);
        bool Contains(ISpectraContainer container);

        new void Remove(Binding binding);
        new void RemoveAt(int i);

        new void Add(Binding binding);

        IEnumerable<Binding> GetBindings(ISpectraContainer container);

    }
}
