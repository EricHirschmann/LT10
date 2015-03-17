using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Evel.interfaces;

namespace Evel.engine.algorythms {
    public class SearchException : Exception {

        private string _code;
        private ISpectrum _spectrum;

        public SearchException(string code) : this(code, null) { }
            

        public SearchException(string code, ISpectrum spectrum)
            : base(code) {
            this._code = code;
            this._spectrum = spectrum;
        }

        public ISpectrum Spectrum {
            get { return this._spectrum; }
        }

        public string ExceptionCode {
            get { return this._code; }
        }

        ///SE0001: AnhSpectraContainer.getEvaluationArray(object, double[]) w celu uniknięcia dzielenia przez zero w wyliczaniu różnicy
        ///SE0002: AnhSpectraContainer.getTheoreticalSpectrum(ISpectrum spectrum) - singular matrix!
        

    }
}
