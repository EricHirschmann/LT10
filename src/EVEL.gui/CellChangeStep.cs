using System;
using System.Collections.Generic;
using System.Text;
using System.Windows.Forms;

namespace Evel.gui {
    class CellChangeStep : ChangeStep {

        private DataGridViewCell cell;
        private object value;

        public CellChangeStep(DataGridViewCell cell, string name, GroupTabPage holder)
            : base(name, holder) {
            this.cell = cell;
            this.value = cell.Value;
        }

        public override void Commit() {
            object tmpValue = this.cell.Value;
            this.cell.Value = this.value;
            this.value = tmpValue;
            this.cell.DataGridView.Invalidate();
        }

        public override bool Equals(object obj) {
            if (obj is CellChangeStep) {
                return ((CellChangeStep)obj).cell == this.cell && ((CellChangeStep)obj).value.Equals(this.value);
            } else
                return false;
        }

        public override int GetHashCode() {
            return base.GetHashCode();
        }

    }
}
