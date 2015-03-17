using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using Evel.interfaces;

namespace Evel.gui {
    public partial class DeleteDocument : Form {

        private IProject project;

        public DeleteDocument(IProject project) {
            InitializeComponent();
            this.project = project;
            foreach (ISpectraContainer container in project.Containers)
                checkedListBox1.Items.Add(container, false);
        }

        private void checkedListBox1_Format(object sender, ListControlConvertEventArgs e) {
            ISpectraContainer container = (ISpectraContainer)e.ListItem;
            e.Value = String.Format("{0} ({1})", container.Name, container.Model.Name);
        }
    }
}
