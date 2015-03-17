using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Evel.interfaces {
    public class SpectrumLoadException : Exception {

        public SpectrumLoadException(string message)
            : base(message) {
        }

    }
}
