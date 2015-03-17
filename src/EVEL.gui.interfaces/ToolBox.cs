using System;
using System.Collections.Generic;
using System.Text;
using System.Windows.Forms;

namespace Evel.gui.interfaces {
    public class ToolBox : GroupBox {

        protected TabPage groupTabPage;

        public ToolBox(TabPage groupTabPage)
            : base() {
            this.groupTabPage = groupTabPage;
        }

    }
}
