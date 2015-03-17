using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Text;
using System.Windows.Forms;

namespace Evel.gui {
    public partial class SaveBeforeFitDialog : Form {
        public SaveBeforeFitDialog() {
            InitializeComponent();
        }

        private void button_Click(object sender, EventArgs e) {
            if (checkBox1.Checked) {
                if (sender == button1)
                    MainForm.savebeforefitting = 2;
                else
                    MainForm.savebeforefitting = 0;
                MainForm.WriteRegistry();
            }            
        }
    }
}
