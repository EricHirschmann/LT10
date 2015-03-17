using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Text;
using System.Windows.Forms;

namespace Evel.gui {
    public partial class WizardGuideForm : Form {
        public WizardGuideForm(string title) {
            InitializeComponent();
            this.Text = title;
        }
    }
}
