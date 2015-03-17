using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using Evel.interfaces;

namespace Evel.gui {
    public class ContainerToolStripMenuItem : ToolStripMenuItem {
        private ISpectraContainer _container;

        public ISpectraContainer SpectraContainer {
            get { return this._container; }
            set { this._container = value; }
        }
    }
}
