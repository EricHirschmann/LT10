using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Text;
using System.Windows.Forms;
using Evel.interfaces;
using Evel.engine;

namespace Evel.gui {
    public partial class GroupBindingCreatorForm : Form {

        private GroupBinding _binding = null;
        private IProject _project = null;

        public GroupBinding Binding {
            get { return this._binding; }
        }

        public GroupBindingCreatorForm(IProject project) {
            InitializeComponent();
            this._project = project;
            this._binding = null;
            populateDocsControl();
        }

        public GroupBindingCreatorForm(IProject project, GroupBinding binding) {
            InitializeComponent();
            this._project = project;
            this._binding = binding;
            populateDocsControl();
            populateGroupNamesList();
            btnOK.Enabled = true;
        }

        private void panel1_Paint(object sender, PaintEventArgs e) {
            MainForm.DialogPanel_Paint(sender, e);
        }

        private void populateDocsControl() {
            int i, j;
            if (this._binding != null)
                for (i = 0; i < this._binding.Containers.Length; i++)
                    listChosenDocs.Items.Add(this._binding.Containers[i]);


            for (i = 0; i < _project.Containers.Count; i++) {
                for (j = 0; j < _project.Containers[i].Spectra[0].Parameters.GroupCount; j++)
                    if ((_project.Containers[i].Spectra[0].Parameters[j].Definition.Type & GroupType.Hidden) == 0)
                        if (!_project.BindingsManager.Contains(_project.Containers[i], _project.Containers[i].Spectra[0].Parameters[j].Definition.name)) {
                            if (this._binding == null) {
                                listAvailableDocs.Items.Add(_project.Containers[i]);
                                break;
                            } else if (!this._binding.ContainsContainer(_project.Containers[i])) {
                                listAvailableDocs.Items.Add(_project.Containers[i]);
                                break;
                            }
                        }
            }
            
        }

        private void populateGroupNamesList() {
            listGroupNames.Items.Clear();
            btnOK.Enabled = false;
            if (listChosenDocs.Items.Count > 1) {
                bool bindableGroup;
                int i, j;
                string groupName;
                ISpectraContainer container = (ISpectraContainer)listChosenDocs.Items[0];
                ISpectraContainer currentContainer;
                for (i = 0; i < container.Spectra[0].Parameters.GroupCount; i++) {
                    groupName = container.Spectra[0].Parameters[i].Definition.name;
                    if (((container.Spectra[0].Parameters[i].Definition.Type & GroupType.Hidden) == 0)) {
                        if (!_project.BindingsManager.Contains(container, groupName)) {
                            bindableGroup = true;
                            for (j = 1; j < listChosenDocs.Items.Count; j++) {
                                currentContainer = (ISpectraContainer)listChosenDocs.Items[j];
                                bindableGroup &= currentContainer.Spectra[0].Parameters.ContainsGroup(groupName) &&
                                    !_project.BindingsManager.Contains(currentContainer, groupName);
                                if (bindableGroup)
                                    bindableGroup &= currentContainer.Spectra[0].Parameters[groupName].Definition == container.Spectra[0].Parameters[groupName].Definition;
                            }
                            if (bindableGroup)
                                listGroupNames.Items.Add(groupName);

                        } else if (this._binding != null)
                            if (this._binding.ContainsGroup(groupName)) {
                                listGroupNames.Items.Add(groupName, true);
                                btnOK.Enabled = true;
                            }
                    }
                }
            }
        }

        private void btnMove_Click(object sender, EventArgs e) {
            ListBox source, target;
            if (sender == btnAdd) {
                source = listAvailableDocs;
                target = listChosenDocs;
            } else {
                source = listChosenDocs;
                target = listAvailableDocs;
            }
            while (source.SelectedItems.Count > 0) {
                target.Items.Add(source.SelectedItems[0]);
                source.Items.Remove(source.SelectedItems[0]);
            }
            populateGroupNamesList();
        }

        private void listGroupNames_SelectedValueChanged(object sender, EventArgs e) {
            btnOK.Enabled = listGroupNames.CheckedItems.Count > 0;
        }

        private void btnOK_Click(object sender, EventArgs e) {
            List<ISpectraContainer> containers = new List<ISpectraContainer>();
            List<string> groupNames = new List<string>();
            int i;
            for (i=0; i<listChosenDocs.Items.Count; i++)
                containers.Add((ISpectraContainer)listChosenDocs.Items[i]);
            for (i=0; i<listGroupNames.CheckedItems.Count; i++)
                groupNames.Add((string)listGroupNames.CheckedItems[i]);
            if (this._binding == null)
                this._binding = new GroupBinding(containers, groupNames, this._project, txtName.Text);
            else {
                this._binding.SetParticipants(containers, groupNames);
                this._binding.Name = txtName.Text;
            }
        }

    }
}
