using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.Drawing;

namespace Evel.gui {
    public class ObjectToolStripMenuItem : ToolStripMenuItem {

        private object _bindedObject;

        public ObjectToolStripMenuItem(object bindedObject) : base(bindedObject.ToString()) {
            this._bindedObject = bindedObject;
        }

        public ObjectToolStripMenuItem(object bindedObject, string text)
            : base(text) {
            this._bindedObject = bindedObject;
        }

        public Object BindedObject {
            get { return this._bindedObject; }
        }

        protected override void OnPaint(PaintEventArgs e) {
            base.OnPaint(e);
            Rectangle rect = new Rectangle(e.ClipRectangle.X, e.ClipRectangle.Y + 1, e.ClipRectangle.Width - 32, e.ClipRectangle.Height - 2);
            rect.Offset(30, 0);
            if (Selected) {
                e.Graphics.FillRectangle(new SolidBrush(Color.FromArgb(184, 191, 211)), rect);
                DefaultGroupGUI.DrawHeaderContent(Text, e.Graphics, Font, rect, false, SystemBrushes.HighlightText);
                //DataGridParameterView.DrawHeaderContent(((ToolStripItem)sender).Text, e.Graphics, ((ToolStripItem)sender).Font, rect, false, SystemBrushes.HighlightText);
            } else {
                e.Graphics.FillRectangle(new SolidBrush(System.Drawing.Color.WhiteSmoke), rect);
                DefaultGroupGUI.DrawHeaderContent(Text, e.Graphics, Font, rect, false, SystemBrushes.ControlText);
            }
        }

    }
}
