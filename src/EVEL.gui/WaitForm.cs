using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Text;
using System.Windows.Forms;
using Evel.interfaces;

namespace Evel.gui {
    public partial class WaitForm : Form {

        private bool _canClose;

        public event EventHandler ProjectRemoved;

        public WaitForm() {
            InitializeComponent();
            listView1.Columns[1].Width = 100;
            listView1.Columns[0].Width = listView1.Width - listView1.Columns[1].Width - 5;
            _canClose = true;
        }

        public void AddProject(IProject project) {
            for (int i = 0; i < listView1.Items.Count; i++)
                if (((ProgressListViewItem)listView1.Items[i].SubItems[1]).Project == project)
                    return;
            if (project.IsBusy) {
                ListViewItem item = new ListViewItem(project.Caption);
                item.SubItems.Add(new ProgressListViewItem(listView1, project));
                item.SubItems[1].Text = "Finished";
                listView1.Items.Add(item);
            }
            _canClose = listView1.Items.Count == 0;
            if (!_canClose) {
                listView1.Height = listView1.GetItemRect(0).Height * Math.Min(listView1.Items.Count, 10) + 3;
                Show();
            }
        }

        public void RemoveProject(IProject project) {
            for (int i = 0; i < listView1.Items.Count; i++)
                if (((ProgressListViewItem)listView1.Items[i].SubItems[1]).Project == project) {
                    ((ProgressListViewItem)listView1.Items[i].SubItems[1]).ProgressBar.Dispose();
                    listView1.Items[i].Remove();
                }
            if (_canClose = listView1.Items.Count == 0)
                Hide();
            if (listView1.Items.Count > 0)
                listView1.Height = listView1.GetItemRect(0).Height * Math.Min(listView1.Items.Count, 10) + 3;
            if (this.ProjectRemoved != null)
                this.ProjectRemoved(this, new EventArgs());
        }

        private void listView1_DrawSubItem(object sender, DrawListViewSubItemEventArgs e) {
            if (e.SubItem is ProgressListViewItem) {
                ProgressListViewItem item = (ProgressListViewItem)e.SubItem;
                if (item.ProgressBar.Visible = item.Project.IsBusy)
                    item.ProgressBar.Bounds = e.Bounds;
            }
            e.DrawBackground();
            e.DrawText();
            //e.DrawDefault = true;
        }

        private void WaitForm_FormClosing(object sender, FormClosingEventArgs e) {
            e.Cancel = !_canClose;
        }

        public bool CanClose {
            get { return this._canClose; }
        }

    }

    class ProgressListViewItem : ListViewItem.ListViewSubItem {

        ProgressBar _progressBar;
        IProject _project;

        public ProgressListViewItem(ListView parent, IProject project)
            : base() {
            this._progressBar = new ProgressBar();
            this._progressBar.Parent = parent;
            this._progressBar.Style = ProgressBarStyle.Marquee;
            this._project = project;
        }

        public ProgressBar ProgressBar {
            get { return this._progressBar; }
        }

        public IProject Project {
            get { return this._project; }
        }

        public bool Finished {
            get { return this._project.IsBusy; }
        }

    }

}
