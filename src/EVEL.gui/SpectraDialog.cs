using System;
using System.Windows.Forms;
using Evel.interfaces;
using System.Collections.Generic;

namespace Evel.gui {
    public partial class SpectraDialog : Form {
        public SpectraDialog() {
            InitializeComponent();
        }

        public SpectraDialog(ISpectraContainer spectraContainer) : this() {
            foreach (ISpectrum spectrum in spectraContainer.Spectra)
                //spectraList.Items.Add(spectrum.Path);
                spectraList.Items.Add(spectrum);
            button1.Enabled = spectraList.Items.Count > 0;
        }

        public SpectraDialog(ICollection<string> paths) : this() {
            if (paths == null) {
                toolStripButton1_Click(toolStripButton1, null);
            } else
                foreach (string p in paths)
                    spectraList.Items.Add(p);
            button1.Enabled = spectraList.Items.Count > 0;
        }

        //public string[] SpectraFiles {
        //    get {
        //        if (spectraList.Items.Count == 0) return null;
        //        else {
        //            string[] result = new string[spectraList.Items.Count];
        //            for (int i=0; i<spectraList.Items.Count; i++) {
        //                result[i] = spectraList.Items[i].ToString();
        //            }
        //            return result;
        //        }
        //    }
        //    set {
        //        if (value != null) {
        //            foreach (string s in value) {
        //                spectraList.Items.Add(s);
        //            }
        //        }
        //    }
        //}

        private void toolStripButton1_Click(object sender, EventArgs e) {
            OpenFileDialog open = new OpenFileDialog();
            open.Filter = "Spectra Files (*.dat; *.txt)|*.dat;*.txt|Simulated Spectra Files (*.sim)|*.sim|All Files (*.*)|*.*";
            open.InitialDirectory = MainForm.CommonSpectraPath;
            open.Multiselect = true;
            if (open.ShowDialog() == DialogResult.OK) {
                foreach (string fileName in open.FileNames) {
                    if (!spectraList.Items.Contains(fileName))
                        spectraList.Items.Add(fileName);
                }
            }
            button1.Enabled = spectraList.Items.Count > 0;
            open.Dispose();
        }

        private void spectraList_Format(object sender, ListControlConvertEventArgs e) {
            //e.Value = System.IO.Path.GetFileName(e.ListItem.ToString());
            if (e.ListItem is ISpectrum)
                e.Value = ((ISpectrum)e.ListItem).Name;
            else
                e.Value = System.IO.Path.GetFileName(e.ListItem.ToString());
        }

        private void toolStripButton2_Click(object sender, EventArgs e) {
            while (spectraList.SelectedItems.Count > 0) {
                spectraList.Items.Remove(spectraList.SelectedItems[0]);
            }
            button1.Enabled = spectraList.Items.Count > 0;
        }

        private void panel1_Paint(object sender, PaintEventArgs e) {
            MainForm.DialogPanel_Paint(sender, e);
        }


    }
}
