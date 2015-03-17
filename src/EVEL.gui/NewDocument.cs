using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Text;
using System.Windows.Forms;
using Evel.interfaces;
using Evel.engine;
using System.Collections;

namespace Evel.gui {
    public partial class NewDocument : Form {

        private List<string> _newSpectraList;
        private IProject _project;

        public NewDocument(IProject project) {
            InitializeComponent();

            _newSpectraList = new List<string>();
            this._project = project;

            for (int i = 0; i < AvailableAssemblies.AvailableModels.Count; i++)
                if (AvailableAssemblies.AvailableModels[i].projectType == project.GetType())
                    cbTheoreticalModel.Items.Add(AvailableAssemblies.AvailableModels[i]);

            foreach (ISpectraContainer container in project.Containers)
                cbDocument.Items.Add(container);

            cbTheoreticalModel.SelectedIndex = 0;
            cbDocument.SelectedIndex = 0;
            
        }

        private void cbTheoreticalModel_SelectedIndexChanged(object sender, EventArgs e) {
            dialogs.ModelChooserDialog.SetDescrption((ModelDescription)cbTheoreticalModel.SelectedItem, info);
        }

        private void cbDocument_SelectedIndexChanged(object sender, EventArgs e) {
            label1.Text = setSpectraText(((ISpectraContainer)cbDocument.SelectedItem).Spectra);
        }

        private string setSpectraText(ICollection<ISpectrum> spectra) {
            
            IEnumerator<ISpectrum> e = spectra.GetEnumerator();
            if (e.MoveNext())
                return String.Format("{0} ({1} more)", e.Current.Name, spectra.Count - 1);
            else
                return "No spectra available";
        }

        private string setSpectraText(ICollection<string> spectraNames) {
            IEnumerator<string> e = spectraNames.GetEnumerator();
            if (e.MoveNext())
                return String.Format("{0}{1}", System.IO.Path.GetFileName(e.Current), (spectraNames.Count - 1 > 0) ? String.Format(" ({0} more)", spectraNames.Count - 1) : "");
            else
                return "Define";
        }

        private void spectraOriginChange(object sender, EventArgs e) {
            pnlCopyFromDoc.Enabled = radioButton1.Checked;
            button1.Enabled = radioButton2.Checked;
            ValidateForm();
        }

        private void comboFormat(object sender, ListControlConvertEventArgs e) {
            if (e.ListItem.GetType().GetInterface("ISpectraContainer", true) == typeof(ISpectraContainer))
                e.Value = ((ISpectraContainer)e.ListItem).Name;
            else
                e.Value = ((ModelDescription)e.ListItem).name;
        }

        private void button1_Click(object sender, EventArgs e) {
            SpectraDialog sd = new SpectraDialog(_newSpectraList);

            if (sd.ShowDialog() == DialogResult.OK) {
                _newSpectraList.Clear();
                //if (sd.SpectraFiles != null)
                //    foreach (string s in sd.SpectraFiles)
                //        _newSpectraList.Add(s);

                //nie trzeba sprawdzac jakiego typu jest o bo dialog zostal utworzony z lista stringow
                //wiec nie ma wsrod obiektow obiektu typu ISPectrum
                foreach (object o in sd.spectraList.Items)
                    _newSpectraList.Add((string)o);
                button1.Text = setSpectraText(_newSpectraList);
                ValidateForm();
            }
        }

        private void ValidateForm() {
            OkButton.Enabled = true;
            //valid name
            foreach (ISpectraContainer container in _project.Containers)
                if (container.Name.Equals(textBox1.Text)) {
                    OkButton.Enabled = false;
                    break;
                }
            if (!OkButton.Enabled) {
                lblNameError.Text = "".Equals(textBox1.Text) ? "Wrong document name" : "Document name must be unique in project";
            }
            lblNameError.Visible = !OkButton.Enabled;

            //spectra list
            OkButton.Enabled &= !(radioButton2.Checked && _newSpectraList.Count == 0) || radioButton1.Checked;
        }

        public ISpectraContainer TemplateContainer {
            get {
                if (radioButton1.Checked)
                    return (ISpectraContainer)cbDocument.SelectedItem;
                else
                    return null;
            }
        }

        public List<string> SpectraFiles {
            get {
                
                List<string> files = null;
                if (radioButton1.Checked) {
                    files = new List<string>();
                    foreach (ISpectrum spectrum in ((ISpectraContainer)cbDocument.SelectedItem).Spectra)
                        files.Add(spectrum.Path);
                } else {
                    files = _newSpectraList; // new List<string>(_newSpectraList);
                }
                return files;
            }
        }

        public IModel Model {
            get { 
                ModelDescription modelDescription = (ModelDescription)cbTheoreticalModel.SelectedItem;
                return AvailableAssemblies.getModel(modelDescription.plugin.className); 
            }
        }

        public string DocumentName {
            get { return textBox1.Text; }
        }

        public GroupDefinition[] GroupDefinitions {
            get { return ((ModelDescription)cbTheoreticalModel.SelectedItem).groupDefinitions; }
        }

        private void panel3_Paint(object sender, PaintEventArgs e) {
            MainForm.DialogPanel_Paint(sender, e);
        }

        private void textBox1_TextChanged(object sender, EventArgs e) {
            ValidateForm();
        }

    }
}
