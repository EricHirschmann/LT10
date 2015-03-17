using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using Evel.interfaces;

namespace Evel.gui.interfaces {

    public class DataGridViewSpectrumRow : DataGridViewRow {
        private ISpectrum _spectrum;
        public ISpectrum Spectrum {
            get { return this._spectrum; }
        }
        public DataGridViewSpectrumRow(ISpectrum spectrum)
            : base() {
            this._spectrum = spectrum;
        }
    }

}
