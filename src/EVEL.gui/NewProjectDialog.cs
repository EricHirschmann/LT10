using System;
using System.Windows.Forms;
using Evel.interfaces;
using Evel.engine;
using System.Drawing;

namespace Evel.gui.dialogs {

    public partial class NewProjectDialog : Form {

        private Type _projectType;
        
        public NewProjectDialog(Type projectType) {
            InitializeComponent();
            //projectPathTextBox.Text = Program.ProjectsPath;
            this._projectType = projectType;
        }

        public new DialogResult ShowDialog() {
            //DialogResult result;
            //if (AddModel() == DialogResult.Cancel) {
            //    this.DialogResult = DialogResult.Cancel;
            //    this.Hide();
            //    result = DialogResult.Cancel;
            //} else {
            //    result = base.ShowDialog();
            //}
            //return result;
            this._projectType = null;
            AddModel();
            return base.ShowDialog();
        }

        public SpectraContainerDescription[] ContainerDescriptions {
            get {
                SpectraContainerDescription[] result = new SpectraContainerDescription[ModelsTree.Nodes.Count];
                for (int i = 0; i < ModelsTree.Nodes.Count; i++) {
                    result[i] = ((ModelSubtree)ModelsTree.Nodes[i]).ContainerDescription;
                }
                return result;
            }
        }

        public Type ProjectType {
            get {
                return _projectType;
            }
            set {
                this._projectType = value;
            }
        }

        public void AddModel() {

            //ModelChooserDialog mcd = new ModelChooserDialog(_projectType);
            //if (Program.MainWindow.CurrentWizard != null)
            //    Program.MainWindow.CurrentWizard.DialogOpened(mcd);

            //DialogResult result = mcd.ShowDialog();
            //if (result == DialogResult.OK) {
                //TreeNode modelTreeNode = new ModelSubtree(AvailableAssemblies.getModel(((ModelDescription)mcd.models_dis.SelectedItem).plugin.className), ModelsTree);
            //    IModel model = AvailableAssemblies.getModel(mcd.Selection.plugin.className);
            ModelSubtree modelTreeNode = new ModelSubtree(this._projectType, ModelsTree);
            modelTreeNode.ProjectTypeChanged += new EventHandler(delegate(object sender, EventArgs e) {
                this._projectType = ((ModelSubtree)sender).ProjectType;
            });
                //if (this._projectType == null)
                //    this._projectType = model.ProjectType;
                this.ModelsTree.Nodes.Add(modelTreeNode);
                modelTreeNode.Expand();
            //}
            //if (Program.MainWindow.CurrentWizard != null)
            //    Program.MainWindow.CurrentWizard.DialogClosed();

            //mcd.Dispose();
            //Invalidate();
            //return result;
        }

        private void DisableButtonTreeNode(TreeNode node) {
            if (node is ButtonTreeNode)
                ((ButtonTreeNode)node).ButtonControl.Visible = node.IsVisible;
            foreach (TreeNode n in node.Nodes)
                DisableButtonTreeNode(n);
        }

        private void ModelsTree_DrawNode(object sender, DrawTreeNodeEventArgs e) {
            Brush brush = null;
            foreach (TreeNode tn in ModelsTree.Nodes)
                DisableButtonTreeNode(tn);
            if (e.Node is ButtonTreeNode) {
                ButtonTreeNode btn = e.Node as ButtonTreeNode;
                Rectangle rect = new Rectangle(
                    ModelsTree.ClientRectangle.Width - 210,
                    e.Bounds.Y,
                    200,
                    ModelsTree.ItemHeight - 1);
                btn.ButtonControl.Bounds = rect;
                //color
            }
            if (e.Node is IValidable) {
                if (!((IValidable)e.Node).IsValid)
                    brush = Brushes.Red;
                else
                    brush = Brushes.Green;
            } else {
                if ((e.State & TreeNodeStates.Selected) == TreeNodeStates.Selected)
                    brush = Brushes.White;
                else
                    brush = Brushes.Black;
            }
            int x = (ModelsTree.ItemHeight - (int)e.Graphics.MeasureString(e.Node.Text, ModelsTree.Font).Height) / 2;
            e.Graphics.DrawString(e.Node.Text, ModelsTree.Font,
                            brush, e.Bounds.Left, e.Bounds.Top + x);
            e.DrawDefault = false;
            ValidateProject();
        }

        private void ValidateProject() {
            //OkButton.Enabled = (projectNameTextBox.Text != "") && (projectPathTextBox.Text != "") && (ModelsTree.Nodes.Count>0);
            OkButton.Enabled = ModelsTree.Nodes.Count > 0;
            foreach (TreeNode node in ModelsTree.Nodes) {
                if (node is IValidable)
                    OkButton.Enabled &= ((IValidable)node).IsValid;
                if (!OkButton.Enabled) break;
            }
        }

        private void toolStripButton1_Click(object sender, EventArgs e) {
            AddModel();
        }

        private void projectNameTextBox_TextChanged(object sender, EventArgs e) {
            ValidateProject();
        }

        private void panel1_Paint(object sender, PaintEventArgs e) {
            MainForm.DialogPanel_Paint(sender, e);
        }

        private void toolStripButton2_Click(object sender, EventArgs e) {
            if (ModelsTree.SelectedNode != null) {
                TreeNode modelNode;
                if (ModelsTree.SelectedNode.Parent != null)
                    modelNode = ModelsTree.SelectedNode.Parent;
                else
                    modelNode = ModelsTree.SelectedNode;
                ((ModelSubtree)modelNode).Dispose();
                ModelsTree.Nodes.Remove(modelNode);
            }
            ValidateProject();
        }

        //private void button1_Click(object sender, EventArgs e) {
        //    folderBrowserDialog1.SelectedPath = projectPathTextBox.Text;
        //    if (folderBrowserDialog1.ShowDialog() == DialogResult.OK)
        //        projectPathTextBox.Text = folderBrowserDialog1.SelectedPath;
        //}


    }

    interface IValidable {
        bool IsValid { get; }
    }

    public delegate bool ValidationEventHandler(ButtonTreeNode node);

    public class ButtonTreeNode : TreeNode, IValidable, IDisposable {

        public object Value;

        public Button ButtonControl;

        public bool IsValid {
            get { return Validate(this); }
        }

        public event ValidationEventHandler Validate;

        public ButtonTreeNode(object value, string text, Control parentControl, EventHandler onClickHandler, ValidationEventHandler validate)
            : base(text) {
            this.Value = value;
            this.Validate += validate;
            this.ButtonControl = new Button();
            this.ButtonControl.Text = getButtonString(this.Value);
            this.ButtonControl.Parent = parentControl;
            this.ButtonControl.Visible = false;
            this.ButtonControl.FlatStyle = FlatStyle.Popup;
            this.ButtonControl.BackColor = SystemColors.Control;
            this.ButtonControl.Click += onClickHandler;
        }

        public static string getButtonString(object value) {
            if (value == null)
                return "(Define)";
            else {
                if (value is IModel)
                    return ((IModel)value).Name;
                else {
                    if (value is GroupDefinition[]) {
                        int count = 0;
                        foreach (GroupDefinition gd in (GroupDefinition[])value) {
                            if (gd.kind == 1) count++;  //sample
                        }
                        if (count > 0)
                            return String.Format("{0} Group{1}", count, (count == 1) ? "" : "s");
                        else
                            return "(Define)";
                    } else {
                        if (value is string[]) {
                            string[] spectra = (string[])value;
                            if (spectra.Length>0) {
                                return String.Format("{0} {1}", System.IO.Path.GetFileName(spectra[0]), (spectra.Length > 1) ? String.Format("({0} more)", spectra.Length-1) : "");
                            }else {
                                return "(Define)";
                            }
                        } else
                            return value.ToString();
                    }
                }
            }
        }

        #region IDisposable Members

        public void Dispose() {
            ButtonControl.Dispose();
        }

        #endregion
    }


    public class ModelSubtree : TreeNode, IValidable, IDisposable {

        //private GroupDefinition[] _groupsDefinition;

        public event EventHandler ProjectTypeChanged;

        private Type _projectType;

        public Type ProjectType {
            get { return this._projectType; }
            set {
                if (this._projectType != value) {
                    this._projectType = value;
                    if (ProjectTypeChanged != null)
                        ProjectTypeChanged(this, null);
                }
            }
        }

        public GroupDefinition[] GroupsDefinition {
            get { return (GroupDefinition[])this._description.groupsDefinition; }
        }

        private SpectraContainerDescription _description;

        public bool IsValid {
            get {
                bool result = true;
                foreach (TreeNode node in Nodes) {
                    if (node is IValidable) {
                        result &= ((IValidable)node).IsValid;
                    }
                    if (!result) break;
                }
                return result;
            }
        }

        public ModelSubtree(Type projectType, TreeView parentControl)
            : base() {
            this._projectType = projectType;
            this._description = new SpectraContainerDescription();
            this._description.name = String.Format("Document{0}{1}", ((parentControl.Nodes.Count > 8) ? "" : "0"), parentControl.Nodes.Count + 1);
            this._description.model = null;
            //this._description.groupsDefinition = model.GroupsDefinition;
            //this._groupsDefinition = 
            this.Text = "Document";
            this.Nodes.Add(new ButtonTreeNode(_description.name, "Document Name", parentControl, new EventHandler(NameTreeNodeClick), new ValidationEventHandler(NameValidation)));
            ButtonTreeNode modelTreeNode = new ButtonTreeNode(_description.model, "Theoretical Model", parentControl, new EventHandler(ModelTreeNodeClick), new ValidationEventHandler(ModelValidation));
            //modelTreeNode.ButtonControl.Enabled = false;
            //modelTreeNode.ButtonControl.BackColor = Color.White;
            this.Nodes.Add(modelTreeNode);
            //foreach (GroupDefinition gd in GroupsDefinition)
            //    if (gd.allowMultiple) {
            //        ButtonTreeNode groupTreeNode = new ButtonTreeNode(new GroupDefinition[] { gd }, "Groups", parentControl, new EventHandler(GroupsTreeNodeClick), new ValidationEventHandler(GroupsValidation));
            //        //groupTreeNode.Value = this._groupsDefinition;
            //        this.Nodes.Add(groupTreeNode);
            //        break;
            //    }
            ButtonTreeNode SpectraTreeNode = new ButtonTreeNode(_description.spectraPaths, "Spectra", parentControl, new EventHandler(SpectraTreeNodeClick), new ValidationEventHandler(SpectraValidation));
            SpectraTreeNode.Name = "Spectra";
            this.Nodes.Add(SpectraTreeNode);
            
        }

        public SpectraContainerDescription ContainerDescription {
            get { return this._description; }
        }

        private void ModelTreeNodeClick(object sender, EventArgs e) {
            ModelChooserDialog mcd = new ModelChooserDialog(_projectType);
            //if (Program.MainWindow.CurrentWizard != null)
            //    Program.MainWindow.CurrentWizard.DialogOpened(mcd);

            DialogResult result = mcd.ShowDialog();
            if (result == DialogResult.OK) {
                this._description.model = AvailableAssemblies.getModel(mcd.Selection.plugin.className);
                ((ButtonTreeNode)this.Nodes[1]).Value = this._description.model;
                ((Button)sender).Text = ButtonTreeNode.getButtonString(this._description.model);
                this.ProjectType = this._description.model.ProjectType;
            }
            //if (Program.MainWindow.CurrentWizard != null)
            //    Program.MainWindow.CurrentWizard.DialogClosed();
            mcd.Dispose();
            this.TreeView.Invalidate();
        }

        private bool ModelValidation(ButtonTreeNode node) {
            bool result = node.Value is IModel;
            if (result)
                node.ButtonControl.BackColor = Color.White;
            else
                node.ButtonControl.BackColor = SystemColors.Control;
            return result;
            //return true;
        }

        private void NameTreeNodeClick(object sender, EventArgs e) {
            DocumentNameDialog dng = new DocumentNameDialog();
            dng.textBox1.Text = this._description.name;
            if (dng.ShowDialog() == DialogResult.OK) {
                this._description.name = dng.textBox1.Text;
                ((ButtonTreeNode)this.Nodes[0]).Value = this._description.name;
                ((Button)sender).Text = this._description.name;
            }
            dng.Dispose();
            this.TreeView.Invalidate();
        }

        private bool NameValidation(ButtonTreeNode node) {
            bool result;
            if (node.Value == null) return false;
            int c = 0;
            foreach (TreeNode n in this.TreeView.Nodes) {
                if (((ButtonTreeNode)n.Nodes[0]).Value.ToString() == (string)node.Value) c++;
            }
            result = ((string)node.Value) != "" && c == 1;
            if (result)
                node.ButtonControl.BackColor = Color.White;
            else
                node.ButtonControl.BackColor = SystemColors.Control;
            return result;
        }

        //private void GroupsTreeNodeClick(object sender, EventArgs e) {
        //    GroupsDialog groupsDialog = new GroupsDialog(this.GroupsDefinition, this._description.model.GroupsDefinition);
        //    if (groupsDialog.ShowDialog() == DialogResult.OK) {
        //        this._description.groupsDefinition = groupsDialog.GroupsDefinition;
        //        ((ButtonTreeNode)this.Nodes[2]).Value = this._description.groupsDefinition;
        //        ((Button)sender).Text = ButtonTreeNode.getButtonString(this._description.groupsDefinition);
        //    }
        //    groupsDialog.Dispose();
        //    this.TreeView.Invalidate();
        //}

        //private bool GroupsValidation(ButtonTreeNode node) {
        //    bool result = node.Value != null;
        //    if (result) {
                
        //        int zeroCount = 0;
        //        foreach (GroupDefinition gd in (GroupDefinition[])node.Value) {
        //            if (gd.kind == 1) zeroCount++;  //sample
        //        }
        //        result = zeroCount > 0;
        //    }
        //    if (result)
        //        node.ButtonControl.BackColor = Color.White;
        //    else
        //        node.ButtonControl.BackColor = SystemColors.Control;
        //    return result;
        //}

        private void SpectraTreeNodeClick(object sender, EventArgs e) {
            SpectraDialog sd;
            //if (this._description.spectraPaths != null) {
                sd = new SpectraDialog(this._description.spectraPaths);
            //}
            if (sd.ShowDialog() == DialogResult.OK) {
                this._description.spectraPaths = new string[sd.spectraList.Items.Count];
                for (int i = 0; i < sd.spectraList.Items.Count; i++)
                    this._description.spectraPaths[i] = (string)sd.spectraList.Items[i];

                //this._description.spectraPaths = sd.SpectraFiles;
                ButtonTreeNode treeNode = (ButtonTreeNode)(this.Nodes.Find("Spectra", false)[0]);
                treeNode.Value = this._description.spectraPaths;
                ((Button)sender).Text = ButtonTreeNode.getButtonString(this._description.spectraPaths);                
            }
            sd.Dispose();
            this.TreeView.Invalidate();
        }

        private bool SpectraValidation(ButtonTreeNode node) {
            bool result;
            if (node.Value == null) 
                result = false;
            else
                result = ((string[])node.Value).Length > 0;
            if (result)
                node.ButtonControl.BackColor = Color.White;
            else
                node.ButtonControl.BackColor = SystemColors.Control;
            return result;
        }


        #region IDisposable Members

        public void Dispose() {
            foreach (TreeNode node in Nodes)
                if (node is ButtonTreeNode)
                    ((ButtonTreeNode)node).Dispose();
        }

        #endregion
    }
}