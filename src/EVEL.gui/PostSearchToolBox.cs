using System;
using System.Collections.Generic;
using System.Text;
using System.Windows.Forms;
using Evel.gui.interfaces;
using System.ComponentModel;

namespace Evel.gui {
    public abstract class PostSearchToolBox : ToolBox {

        public abstract void RunPostSearchEvent(object sender, AsyncCompletedEventArgs args);

        public PostSearchToolBox(TabPage groupTabPage) : base(groupTabPage) { }

    }
}
