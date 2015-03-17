using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Evel.interfaces {

    public enum EvelAssemblyType {
        ProjectAssembly,
        ModelAssembly,
        SpectrumAssembly
    }

    public interface IEvelAssembly {

        EvelAssemblyType AssemblyType { get; }

    }
}
