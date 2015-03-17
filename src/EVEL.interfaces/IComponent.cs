using System;
using System.Collections.Generic;
using System.Text;

namespace Evel.interfaces {
    public interface IComponent : IDisposable {

        //string[] ParameterNames { get; }
        int Size { get; }
        IParameter this[int index] { get; }
        IParameter this[string parameterName] { get; }

        IEnumerator<IParameter> GetEnumerator();

        bool ContainsParameter(string parameterName);

        object Parent { get; }

    }
}
