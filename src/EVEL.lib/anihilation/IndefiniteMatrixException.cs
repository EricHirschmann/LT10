using System;
using System.Collections.Generic;
using System.Text;
using Evel.engine.algorythms;
using Evel.interfaces;

namespace Evel.engine.anh {
    public class IndefiniteMatrixException : SearchException {

        public IndefiniteMatrixException(string message) : base(message) { }

        public IndefiniteMatrixException(string message, ISpectrum spectrum) : base(message, spectrum) { }

    }
}
