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
    public partial class BindingCreatorForm : Form {

        private enum MoveDirection { Left, Right }

        private ParameterBinding _binding;
        private ListBoxParameterItem draggingItem;
        private Rectangle draggingRect;
        private Brush regWindowBrush = SystemBrushes.Window;
        private Brush altWindowBrush = Brushes.Wheat;
        private IProject project;
        private List<ISpectraContainer> _affectedContainers;

        public BindingCreatorForm(IProject project, ParameterBinding binding) {
            InitializeComponent();
            this._binding = binding;
            this._affectedContainers = new List<ISpectraContainer>();
            bool alreadyInBinding = false;
            this.project = project;
            initListBox(lbBinding, false);
            int s, g, c, p;
            ListBoxParameterItem item;
            IParameter parameter;
            foreach (ISpectraContainer container in project.Containers) {
                if (!container.Enabled) continue;
                TabPage tp = new TabPage(container.Name);
                tp.Padding = new Padding(5);
                ListBox lb = new ListBox();
                lb.Parent = tp;
                initListBox(lb, true);
                
                Brush bkBrush = Brushes.Gainsboro;
                for (g = 1; g < container.Spectra[0].Parameters.GroupCount; g++) {
                    if (bkBrush == Brushes.Gainsboro) bkBrush = SystemBrushes.Window;
                    else bkBrush = Brushes.Gainsboro;
                    for (c=0; c < container.Spectra[0].Parameters[g].Components.Size; c++)
                        for (p = 0; p < container.Spectra[0].Parameters[g].Components[c].Size; p++) {
                            for (s = 0; s < container.Spectra.Count; s++) {
                                parameter = container.Spectra[s].Parameters[g].Components[c][p];

                                ParameterBinding foundBinding = null;
                                foreach (Evel.interfaces.Binding b in project.BindingsManager)
                                    if (b is ParameterBinding)
                                        if (alreadyInBinding = ((ParameterBinding)b).ContainsParameter(parameter)) {
                                            foundBinding = (ParameterBinding)b;
                                            break;
                                        }
                                if (parameter.Definition.Properties == 0 && (s == 0 || (parameter.ReferenceGroup > 0 && !parameter.HasReferenceValue))) {
                                    lb.Items.Add(item = new ListBoxParameterItem(parameter, container.Spectra[s].Parameters[g], container, lb, alreadyInBinding));
                                    if (foundBinding == binding && binding != null)
                                        moveItem(item, MoveDirection.Right, true);
                                }
                            }
                        }
                }

                tbParams.TabPages.Add(tp);
            }
        }

        public List<ISpectraContainer> AffectedContainers {
            get { return this._affectedContainers; }
        }

        private void initListBox(ListBox lb, bool allowDragging) {
            lb.Font = new Font("Tahoma", 9.2f);
            lb.Dock = DockStyle.Fill;
            lb.DrawMode = DrawMode.OwnerDrawFixed;

            lb.ItemHeight = (int)lb.Font.Size * 3 + 1;
            lb.IntegralHeight = true;
            lb.DrawItem += listBox1_DrawItem;
            lb.SelectedIndexChanged += listBox_SelectedIndexChanged;
            if (allowDragging) {
                lb.MouseDown += lbBinding_MouseDown;
                lb.MouseMove += lbBinding_MouseMove;
                lb.MouseUp += lbBinding_MouseUp;
                lb.DoubleClick += ok_Click;
            }
        }

        private void panel1_Paint(object sender, PaintEventArgs e) {
            MainForm.DialogPanel_Paint(sender, e);
        }

        private void listBox1_DrawItem(object sender, DrawItemEventArgs e) {
            if (e.Index < 0) return;
            ListBoxParameterItem item = (ListBoxParameterItem)((ListBox)sender).Items[e.Index];
            IParameter p = item.parameter;
            string compId, doc, group, spectrumName;
            //string info = DefaultGroupGUI.getParameterInfo(p, out compId);
            Evel.engine.ProjectBase.getParameterInfo(p, out doc, out group, out compId);
            ParameterLocation location = project.GetParameterLocation(p);
            compId = (location.compId + 1).ToString();
            spectrumName = (p.ReferenceGroup > 0) ? String.Format("{0} ref{1}", project.Containers[location.docId].Spectra[location.specId].Name, p.ReferenceGroup) : "";
            doc = (sender == lbBinding) ? project.Containers[location.docId].Name : "";
            group = project.Containers[location.docId].Spectra[location.specId].Parameters[location.groupId].Definition.name;

            string s = DefaultGroupGUI.BuildFormatedString(p.Definition.Header, DefaultGroupGUI.StringFormatTarget.ParameterDataGrid, compId, group, doc, spectrumName);
            //if (sender == lbBinding)
            //    s = DefaultGroupGUI.BuildFormatedString(p.Definition.Header, DefaultGroupGUI.StringFormatTarget.ParameterDataGrid, compId, group, doc);
            //else
            //    s = DefaultGroupGUI.BuildFormatedString(p.Definition.Header, DefaultGroupGUI.StringFormatTarget.ParameterDataGrid, compId, group);
            if ((e.State & DrawItemState.Selected) == DrawItemState.Selected) {
                if (item.inBinding && sender != lbBinding) {
                    e.Graphics.FillRectangle((e.Index % 2 == 0) ? regWindowBrush : altWindowBrush, e.Bounds);
                    e.Graphics.DrawRectangle(SystemPens.Highlight, new Rectangle(e.Bounds.Left, e.Bounds.Top, e.Bounds.Width-1, e.Bounds.Height-1));
                } else
                    e.Graphics.FillRectangle(SystemBrushes.Highlight, e.Bounds);
                DefaultGroupGUI.DrawHeaderContent(s, e.Graphics, e.Font, e.Bounds, false, (item.inBinding && sender != lbBinding) ? SystemBrushes.InactiveCaptionText : SystemBrushes.HighlightText);
            } else {
                e.Graphics.FillRectangle((e.Index % 2 == 0) ? regWindowBrush : altWindowBrush, e.Bounds);
                DefaultGroupGUI.DrawHeaderContent(s, e.Graphics, e.Font, e.Bounds, false, (item.inBinding && sender != lbBinding) ? SystemBrushes.InactiveCaptionText : SystemBrushes.WindowText);
            }
        }

        public Evel.interfaces.Binding Binding {
            get { return this._binding; }
        }

        private void move_Click(object sender, EventArgs e) {
            ListBox lb;
            MoveDirection md;
            if (sender == buttonAdd) {
                lb = (ListBox)tbParams.SelectedTab.Controls[0];
                md = MoveDirection.Right;
            } else {
                lb = lbBinding;
                md = MoveDirection.Left;
            }

            List<ListBoxParameterItem> items = new List<ListBoxParameterItem>();
            foreach (ListBoxParameterItem item in lb.SelectedItems)
                items.Add(item);

            foreach (ListBoxParameterItem item in items)
                moveItem(item, md, false);
            ((Button)sender).Enabled = false;
        }

        private void moveItem(ListBoxParameterItem item, MoveDirection direction, bool init) {
            switch (direction) {
                case MoveDirection.Right: 
                    if (!item.inBinding || init) 
                        lbBinding.Items.Add(item);
                    //if modifying binding 
                    if (_binding != null) {
                        if (!_binding.ContainsContainer(item.container))
                            _affectedContainers.Add(item.container);
                        else
                            _affectedContainers.Remove(item.container);
                    } else if (_binding == null && !_affectedContainers.Contains(item.container))
                        _affectedContainers.Add(item.container);

                    break;
                case MoveDirection.Left:
                    if (_binding != null && !_affectedContainers.Contains(item.container)) {
                        if (_binding.ContainsContainer(item.container))
                            _affectedContainers.Add(item.container);
                        else
                            _affectedContainers.Remove(item.container);
                    } else if (_binding == null)
                        _affectedContainers.Remove(item.container);
                    if (item.inBinding || init) 
                        lbBinding.Items.Remove(item); 
                    break;
            }
            item.inBinding = direction == MoveDirection.Right;
            buttonAdd.Enabled = false;
            buttonRemove.Enabled = false;
            item.parent.SelectedIndex = -1;
            item.parent.Invalidate();
            buttonOK.Enabled = lbBinding.Items.Count > 1;
        }

        class ListBoxParameterItem {
            public IParameter parameter;
            public IGroup group;
            public ISpectraContainer container;
            //public Brush bkBrush;
            public bool inBinding;
            public ListBox parent;
            public ListBoxParameterItem(IParameter parameter, IGroup group, ISpectraContainer container, ListBox parent, bool inBinding) {
                this.parameter = parameter;
                this.group = group;
                this.container = container;
                //this.bkBrush = bkBrush;
                this.inBinding = false;
                this.parent = parent;
                this.inBinding = inBinding;
            }
        }

        private void listBox_SelectedIndexChanged(object sender, EventArgs e) {
            if (sender == lbBinding)
                buttonRemove.Enabled = true;
            else
                buttonAdd.Enabled = true;
        }

        private void lbBinding_DragDrop(object sender, DragEventArgs e) {
            if (e.Data.GetDataPresent(typeof(ListBoxParameterItem))) {
                ListBoxParameterItem item = (ListBoxParameterItem)e.Data.GetData(typeof(ListBoxParameterItem));
                moveItem(item, MoveDirection.Right, false);
            }
        }

        private void lbBinding_MouseDown(object sender, MouseEventArgs e) {
            int index = ((ListBox)sender).IndexFromPoint(e.X, e.Y);
            if (index != ListBox.NoMatches) {
                draggingItem = (ListBoxParameterItem)((ListBox)sender).Items[index];
                Size s = SystemInformation.DragSize;
                draggingRect = new Rectangle(e.X - s.Width / 2, e.Y - s.Height / 2, s.Width, s.Height);
            } else {
                draggingItem = null;
                draggingRect = Rectangle.Empty;
            }
        }

        private void lbBinding_MouseMove(object sender, MouseEventArgs e) {
            if ((e.Button & MouseButtons.Left) == MouseButtons.Left) {
                if (draggingRect != Rectangle.Empty &&
                    !draggingRect.Contains(e.Location)) {
                    if (draggingItem != null)
                        ((ListBox)sender).DoDragDrop(draggingItem, DragDropEffects.Link);
                }
            }
        }

        private void lbBinding_MouseUp(object sender, MouseEventArgs e) {
            draggingItem = null;
            draggingRect = Rectangle.Empty;
        }

        private void lbBinding_QueryContinueDrag(object sender, QueryContinueDragEventArgs e) {
            //if (e.EscapePressed) {
            //    e.Action = DragAction.Cancel;
            //    return;
            //}

        }

        private void lbBinding_DragOver(object sender, DragEventArgs e) {
            if (e.Data.GetDataPresent(typeof(ListBoxParameterItem)))
                e.Effect = DragDropEffects.Link;
            else
                e.Effect = DragDropEffects.None;
        }

        private void ok_Click(object sender, EventArgs e) {
            if (lbBinding.Items.Count > 1) {
                List<IParameter> parameters = new List<IParameter>();
                int i;
                for (i = 0; i < lbBinding.Items.Count; i++)
                    parameters.Add(((ListBoxParameterItem)lbBinding.Items[i]).parameter);
                if (_binding == null)
                    _binding = new ParameterBinding(parameters, project, txtName.Text);
                else {
                    _binding.Name = txtName.Text;
                    _binding.setParameters(parameters);                    
                }
            }
        }

    }
}
