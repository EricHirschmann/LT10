using System;
using System.Collections.Generic;
using System.Text;
using System.Windows.Forms;
using Evel.interfaces;

namespace Evel.gui.interfaces {

    public delegate object CustomValueExtractor(ISpectrum spectrum);

    public class DataGridViewCustomValueCell : DataGridViewTextBoxCell {

        private CustomValueExtractor valueExtractor;

        public DataGridViewCustomValueCell(CustomValueExtractor valueExtractor) :
            base() {
            this.valueExtractor = valueExtractor;
        }

        protected override object GetValue(int rowIndex) {
            base.GetValue(rowIndex);
            if (OwningRow is DataGridViewSpectrumRow)
                return valueExtractor(((DataGridViewSpectrumRow)OwningRow).Spectrum);
            else
                return 0;
        }

    }
}
