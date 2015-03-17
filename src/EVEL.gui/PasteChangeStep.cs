using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Drawing;

namespace Evel.gui {
    class PasteChangeStep : ChangeStep {

        private List<object> values;
        private List<Point> cells;
        GroupTabPage page;

        public PasteChangeStep(GroupTabPage tabPage, string name, GroupTabPage holder)
            : base(name, holder) {
            this.values = new List<object>();
            this.cells = new List<Point>();
            this.page = tabPage;
        }

        public void AddCell(Point point, object value) {
            values.Add(value);
            cells.Add(point);
        }

        public override void Commit() {
            object tmpValue;
            for (int i = 0; i < cells.Count; i++) {
                tmpValue = this.page.grid[this.cells[i].X, this.cells[i].Y].Value; // this.cell.Value;
                this.page.grid[this.cells[i].X, this.cells[i].Y].Value = this.values[i];
                this.values[i] = tmpValue;
            }
            this.page.grid.Invalidate();
        }

    }
}
