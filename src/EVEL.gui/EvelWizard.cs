using System;
using System.Collections.Generic;
using System.Text;
using Evel.interfaces;
using System.IO;
using System.Xml;
using System.Windows.Forms;
using System.Collections;
using Evel.gui.dialogs;
using System.Threading;

namespace Evel.gui {

    public class WizardException : Exception {
        public WizardException()
            : base("Error in wizard definition file") { }

        public WizardException(string message) : base(message) { }
    }

    public enum WizardEventType {
        Created,
        ParameterValueChange,
        ParameterStatusChange,
        ComponentCountChange,
    }
    public delegate void WizardEventHandler(object sender, WizardEventArgs args);

    public class WizardEventArgs : EventArgs {
        public WizardEventType StepType;
        public ProjectForm projectForm;
        public WizardEventArgs(ProjectForm projectForm, WizardEventType type)
            : base() {
            this.projectForm = projectForm;
            this.StepType = type;
        }
    }

    public class WizardStep {
        public int id;
        public string html;
        public List<Hashtable> controlDefinitions;
        public string dialog;

        public WizardStep(int id, List<Hashtable> controlDefinitions, string html, string dialog) {
            this.id = id;
            this.controlDefinitions = controlDefinitions;
            this.html = html;
            this.dialog = dialog;
        }
    }

    public class EvelWizard {

        private List<WizardStep> steps;
        //private ProjectForm projectForm;
        private int _currentStepId;
        private WizardGuideForm guideForm;
        private string name;
        private List<Form> dialogsTree;
        private Thread wizardThread;
        private Semaphore stepsSemaphore;

        public WizardStep CurrentStep {
            get {
                if (_currentStepId < steps.Count)
                    return steps[_currentStepId];
                else
                    return null;
            }
        }

        public void DialogOpened(Form dialogForm) {
            if (CurrentStep != null) {
                this.dialogsTree.Add(dialogForm);
                if (CurrentStep.dialog == dialogForm.Name)
                    this.stepsSemaphore.Release();
            }
        }

        public void DialogClosed() {
            this.dialogsTree.RemoveAt(this.dialogsTree.Count - 1);
            if (CurrentStep != null && CurrentDialogForm != null) {
                if (CurrentStep.dialog == CurrentDialogForm.Name)
                    this.stepsSemaphore.Release();
            }
        }

        private Form CurrentDialogForm {
            get {
                if (this.dialogsTree.Count > 0)
                    return this.dialogsTree[this.dialogsTree.Count - 1];
                else
                    return null;
            }
        }

        public EvelWizard(Stream wizardDefinition) {
            steps = new List<WizardStep>();
            dialogsTree = new List<Form>();
            int id = 0;
            string dialog;
            XmlReaderSettings settings = new XmlReaderSettings();
            settings.IgnoreWhitespace = true;
            using (XmlReader reader = XmlReader.Create(wizardDefinition, settings)) {
                reader.ReadToFollowing("wizard");
                if (reader.MoveToFirstAttribute())
                    if (reader.Name == "name")
                        this.name = reader.Value;
                while (reader.Read()) {
                    if (reader.Name == "step") {
                        dialog = String.Empty;
                        reader.MoveToFirstAttribute();                        
                        do {
                            switch (reader.Name) {
                                case "id": id = int.Parse(reader.Value); break;
                                case "dialog": dialog = reader.Value; break;
                            }
                        } while (reader.MoveToNextAttribute());
                        reader.MoveToElement();
                        steps.Add(addStep(reader.ReadSubtree(), id, dialog));
                    }
                }
            }
            guideForm = new WizardGuideForm(name);
            guideForm.Left = Program.MainWindow.Right - guideForm.Width - 30;
            guideForm.Top = Program.MainWindow.Top + 80;
            stepsSemaphore = new Semaphore(0, 1);

            _currentStepId = 0;
            addActionEventToControls(CurrentStep.controlDefinitions);
            UpdateBrowser();
            guideForm.Show();
            Program.MainWindow.Focus();
        }

        public WizardStep addStep(XmlReader reader, int id, string dialog) {
            string html = "";
            //object control = null;
            List<Hashtable> controlDefinitions = null;
            //int id = 0;
            //bool dialogInvokation = false;
            while (reader.Read()) {
                switch (reader.Name) {
                    case "html":
                        html = reader.ReadString(); // ReadInnerXml();
                        break;
                    case "controls":
                        controlDefinitions = getControlDefinitions(reader);
                        break;
                }
            }
            
            WizardStep step = new WizardStep(id, controlDefinitions, html, dialog);
            return step;
        }

        private List<Hashtable> getControlDefinitions(XmlReader reader) {
            List<Hashtable> tables = new List<Hashtable>();
            Hashtable table;
            while (reader.Read()) {
                if (reader.Name == "control") {
                    reader.MoveToFirstAttribute();
                    table = new Hashtable();
                    do {
                        table.Add(reader.Name, reader.Value);
                    } while (reader.MoveToNextAttribute());
                    tables.Add(table);
                } else break;
            }

            return tables;
        }

        private object getControl(Hashtable definition) {
            object control = null;

            switch (definition["container"].ToString().ToLower()) {
                case "mainform":
                    control = controlFromMainForm(definition);
                    break;
                case "newprojectdialog":
                    control = ((NewProjectDialog)CurrentDialogForm).Controls.Find(definition["control"].ToString(), true)[0];
                    break;
                case "modelchooserdialog":
                    control = ((ModelChooserDialog)CurrentDialogForm).Controls.Find(definition["control"].ToString(), true)[0];
                    break;
                default:
                    throw new WizardException();
            }
            if (control == null)
                throw new WizardException("Couldn't find control");
            return control;
        }

        private object controlFromMainForm(Hashtable definition) {
            object control = null;
            MainForm mainForm = Program.MainWindow;
            IDictionaryEnumerator e = definition.GetEnumerator();
            while (e.MoveNext()) {
                switch (e.Key.ToString()) {
                    case "menu":
                        string[] items = e.Value.ToString().Split('\\');
                        ToolStripMenuItem menuItem = null;
                        foreach (string item in items) {
                            if (menuItem == null)
                                menuItem = (ToolStripMenuItem)mainForm.MainMenuStrip.Items[String.Format("menuItem{0}", item)];// .Menu.MenuItems[item];
                            else
                                menuItem = (ToolStripMenuItem)menuItem.DropDownItems[String.Format("menuItem{0}", item)];
                        }
                        control = menuItem;
                        break;
                }
            }         
            if (control != null)
                return control;
            else
                throw new WizardException();
        }

        //private void nextStep(object sender, EventArgs args) {
        //    //MessageBox.Show(String.Format("{0} clicked", getControl(CurrentStep.controlDefinition)));
            
        //    removeActionEventFromControl(getControl(CurrentStep.controlDefinition));
        //    _currentStepId++;
        //    if (CurrentStep != null) {
        //        addActionEventToControl(getControl(CurrentStep.controlDefinition));
        //        UpdateBrowser();
        //    } else
        //        MessageBox.Show("End of the wizard", Application.ProductName, MessageBoxButtons.OK, MessageBoxIcon.Information);
        //    //throw new NotImplementedException();
        //}

        private void nextStep() {
            removeActionEventFromControl(CurrentStep.controlDefinitions);
            _currentStepId++;
            wizardThread = new Thread(new ThreadStart(delegate {

                if (CurrentStep != null) {
                    if (CurrentStep.dialog != String.Empty)
                        stepsSemaphore.WaitOne();
                    addActionEventToControls(CurrentStep.controlDefinitions);
                    UpdateBrowser();
                } else
                    UpdateBrowser("End of the wizard");
            }));
            wizardThread.Start();
        }

        private void nextStep(object sender, MouseEventArgs args) {
            nextStep();
            //throw new NotImplementedException();
        }

        void nextStep(object sender, EventArgs e) {
            nextStep();
        }

        private void removeActionEventFromControl(List<Hashtable> definitions) {
            object control;
            foreach (Hashtable def in definitions) {
                control = getControl(def);
                lightItem(control, false);
                if (control is ToolStripMenuItem) {
                    ((ToolStripMenuItem)control).MouseDown -= new MouseEventHandler(nextStep);
                    ((ToolStripMenuItem)control).DropDownOpening -= new EventHandler(nextStep);
                } else if (control is Button) {
                    ((Button)control).Click -= new EventHandler(nextStep);
                }
            }
        }

        private void addActionEventToControls(List<Hashtable> definitions) {
            object control;
            foreach (Hashtable def in definitions) {
                control = getControl(def);
                lightItem(control, true);
                if (control is ToolStripMenuItem) {
                    ToolStripMenuItem menu = (ToolStripMenuItem)control;
                    if (!menu.HasDropDownItems)
                        menu.MouseDown += new MouseEventHandler(nextStep);
                    else
                        menu.DropDownOpening += new EventHandler(nextStep);
                } else if (control is Button) {
                    ((Button)control).Click += new EventHandler(nextStep);
                }
            }
        }

        public void lightItem(object control, bool check) {
            //throw new NotImplementedException();
            Program.MainWindow.Invoke(new ThreadStart(delegate {
                if (control is ToolStripMenuItem)
                    ((ToolStripMenuItem)control).ForeColor = (check) ? System.Drawing.Color.Red : System.Drawing.SystemColors.ControlText;
            }));
        }

        private void UpdateBrowser() {
            this.UpdateBrowser(CurrentStep.html);
        }

        private void UpdateBrowser(string text) {
            StringBuilder builder = new StringBuilder("<html><head><style type=\"text/css\">html {{ font-family: verdana; font-size: xx-small; }} </style></head>{0}</html>");
            Program.MainWindow.Invoke(new ThreadStart(delegate {
                guideForm.browser.DocumentText = String.Format(builder.ToString(), text);
            }));
        }

        ~EvelWizard() {
            //wizardThread.Abort();
            guideForm.Close();
            //foreach (WizardStep step in steps)
            //todo : remove onclick events from all steps
        }

    }
}
