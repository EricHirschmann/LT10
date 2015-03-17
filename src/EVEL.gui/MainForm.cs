using System;
using System.Drawing;
using System.Windows.Forms;
using Evel.interfaces;
using Evel.engine;
using Evel.gui.dialogs;
using System.Collections.Generic;
using System.Threading;
using Microsoft.Win32;
using System.Text;
using System.Collections;
using System.IO;
using System.Net;

namespace Evel.gui {
    public partial class MainForm : Form {

        //int mouseY;

        internal WaitForm waitForm;
        private bool _exiting;

        private delegate void SetStatusEventHandler(string status);

        public static string _registryGUIKeyName = Program.RegistryRootKeyName + "\\gui";

        private static List<string> _recentProjects;
        public static int maxRecentProjects;
        public static byte savebeforefitting; //0 - don't save, 1 - ask, 2 - save without asking
        public static Hashtable StatusColors;
        public static bool switchToSearch;
        public static short parameterRestoreCount;
        public static string uversion; //wersja programu lub wersja na stronie, która została zignorowana przy powiadomieniu
        internal Form baloonForm;

        public static string helpfile = @"help\Reference.chm";
        public static string recoveryProjectsFile = System.IO.Path.Combine(Application.StartupPath, "pr.bak");
        //internal NewProjectDialog newProjectDialog;
        public EvelWizard CurrentWizard;
        private static string projectsPath = null;
        private static string commonSpectraPath = null;

        public static string ProjectsPath {
            get {
                if (projectsPath == null)
                    projectsPath = System.IO.Path.Combine(System.Environment.GetFolderPath(System.Environment.SpecialFolder.MyDocuments), "My LT10 Projects");
                return projectsPath;
            }
            set {
                projectsPath = value;
            }
        }

        public static string CommonSpectraPath {
            get {
                if (commonSpectraPath != null)
                    return commonSpectraPath;
                else
                    return ProjectsPath;
            }
            set {
                commonSpectraPath = value;
            }
        }

        public MainForm(string[] args) {
            InitializeComponent();
            ReadRegistry();

            Splashscreen splashscreen = new Splashscreen();
            splashscreen.ShowDialog();

            //Thread updatesThread = new Thread(LookForUpdates);
            //updatesThread.Start();
            new Lt10Updater(true, UpdateFound);

            //check projects recovery file
            TextReader reader;
            try {
                reader = new StreamReader(recoveryProjectsFile);
                try {
                    string projectPath;
                    bool reopenedInfoShown = false;
                    while ((projectPath = reader.ReadLine()) != null) {
                        if (!reopenedInfoShown) {
                            MessageBox.Show("Application hasn't closed properly last time. Some projects will be reopened now.", Application.ProductName, MessageBoxButtons.OK, MessageBoxIcon.Warning);
                            reopenedInfoShown = true;
                        }
                        try {
                            OpenProject(projectPath);
                        } catch (Exception) { }
                    }
                } finally {
                    reader.Close();
                    //overwrite file
                    try {
                        FileStream fs = File.Create(recoveryProjectsFile);
                        fs.Close();
                    } catch (Exception) { }
                }
            } catch (Exception) { }

            //open program from argument list
            if (args.Length > 0) {
                int i = 0;
                string project = "";
                //List<string> hosts = new List<string>();
                while (i < args.Length) {
                    switch (args[i]) {
                        case "-p":
                            project = args[++i];
                            break;
                        //case "-h":
                        //    hosts.Add(args[++i]);
                        //    break;
                    }
                    i++;
                }
                if (project != "")
                    try {
                        bool alreadyOpened = false;
                        foreach (Form f in MdiChildren) {
                            if (f is ProjectForm) {
                                if (((ProjectForm)f).project.ProjectFile == project) {
                                    alreadyOpened = true;
                                    break;
                                }
                            }
                        }
                        if (!alreadyOpened) {
                            OpenProject(project);
                        }
                    } catch (Exception) { }
            }


            SpectraContainerBase.OpenProgressChanged += SpectraContainerBase_OpenSaveProgressChanged;
            SpectraContainerBase.SaveProgressChanged += SpectraContainerBase_OpenSaveProgressChanged;
            helpProvider1.HelpNamespace = helpfile;
            waitForm = new WaitForm();
            this._exiting = false;
        }

        void ReadRegistry() {
            //options
            try {
                maxRecentProjects = (int)Registry.GetValue(_registryGUIKeyName, "MaxRecentProjects", 5);
                savebeforefitting = (byte)((int)Registry.GetValue(_registryGUIKeyName, "SaveBeforeFitting", 1));
                switchToSearch = Boolean.Parse((string)Registry.GetValue(_registryGUIKeyName, "SwitchToSearchTab", "true"));
                projectsPath = (string)Registry.GetValue(_registryGUIKeyName, "projectsPath", Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments));
                commonSpectraPath = (string)Registry.GetValue(_registryGUIKeyName, "commonSpectraPath", projectsPath);
                parameterRestoreCount = (short)((int)Registry.GetValue(_registryGUIKeyName, "RestoreCount", 2));
                uversion = (string)Registry.GetValue(_registryGUIKeyName, "uversion", System.Reflection.Assembly.GetExecutingAssembly().GetName().Version.ToString());
            } catch {
                maxRecentProjects = 5;
                savebeforefitting = 1; //ask
                switchToSearch = true;
                Registry.SetValue(_registryGUIKeyName, "MaxRecentProjects", maxRecentProjects, RegistryValueKind.DWord);
                projectsPath = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
                commonSpectraPath = projectsPath;
                parameterRestoreCount = 2;
                uversion = System.Reflection.Assembly.GetExecutingAssembly().GetName().Version.ToString();
            }
            //recent projects
            string[] recentProjects = (string[])Registry.GetValue(_registryGUIKeyName, "RecentProjects", new string[0]);
            if (recentProjects == null)
                recentProjects = new string[0];
            if (recentProjects.Length > 0) {
                menuItemOpenRecent.DropDownItems.Clear();
                for (int i = 0; i < Math.Min(recentProjects.Length, maxRecentProjects); i++) {
                    ToolStripItem item = new ToolStripMenuItem(
                        recentProjects[i], null, new EventHandler(RecentProjectClick), (Keys)((int)(Keys.Alt | Keys.D0)+i+1));
                    menuItemOpenRecent.DropDownItems.Add(item);
                }
            } else {
                ToolStripItem item = new ToolStripMenuItem("(empty)");
                item.Enabled = false;
                menuItemOpenRecent.DropDownItems.Add(item);
            }
            _recentProjects = new List<string>(recentProjects);
            //status colors
            StatusColors = new Hashtable();
            int localFreeColor = Color.FromArgb(0xC2, 0xE4, 0x74).ToArgb();
            int localFixedColor = Color.FromArgb(0xE1, 0xA3, 0x9F).ToArgb();
            int commonFreeColor = Color.FromArgb(0x6D, 0xE8, 0x60).ToArgb();
            int commonFixedColor = Color.FromArgb(0xED, 0x87, 0x87).ToArgb();
            int bindedFixedColor = Color.FromArgb(0xfd, 0xc8, 0x4a).ToArgb();
            int bindedFreeColor = Color.FromArgb(0x4a, 0xd0, 0xfd).ToArgb();
            try {
                localFreeColor = (int)Registry.GetValue(_registryGUIKeyName, "LocalFreeColor", localFreeColor);
                localFixedColor = (int)Registry.GetValue(_registryGUIKeyName, "LocalFixedColor", localFixedColor);
                commonFreeColor = (int)Registry.GetValue(_registryGUIKeyName, "CommonFreeColor", commonFreeColor);
                commonFixedColor = (int)Registry.GetValue(_registryGUIKeyName, "CommonFixedColor", commonFixedColor);
                bindedFixedColor = (int)Registry.GetValue(_registryGUIKeyName, "BindedFixedColor", bindedFixedColor);
                bindedFreeColor = (int)Registry.GetValue(_registryGUIKeyName, "BindedFreeColor", bindedFreeColor);
            } finally {
                StatusColors.Add(ParameterStatus.Local | ParameterStatus.Free, Color.FromArgb(localFreeColor));
                StatusColors.Add(ParameterStatus.Local | ParameterStatus.Fixed, Color.FromArgb(localFixedColor));
                StatusColors.Add(ParameterStatus.Common | ParameterStatus.Free, Color.FromArgb(commonFreeColor));
                StatusColors.Add(ParameterStatus.Common | ParameterStatus.Fixed, Color.FromArgb(commonFixedColor));
                StatusColors.Add(ParameterStatus.Binding | ParameterStatus.Free, Color.FromArgb(bindedFreeColor));
                StatusColors.Add(ParameterStatus.Binding | ParameterStatus.Fixed, Color.FromArgb(bindedFixedColor));
            }
        }

        private void UpdateFound(object sender, EventArgs args) {
            baloonForm = (Form)sender;
            notifyIcon.BalloonTipIcon = ToolTipIcon.Info;
            notifyIcon.BalloonTipTitle = "";

            notifyIcon.BalloonTipText = String.Format("New version of LT10 version is available ({0})", ((Lt10Updater)sender).SiteVersion.ToString());
            notifyIcon.ShowBalloonTip(5000);
        }

        //private void LookForUpdates() {
        //    try {
        //        HttpWebRequest request = (HttpWebRequest)WebRequest.Create("http://prac.us.edu.pl/~kansy/lt10v.htm");
        //        IAsyncResult result = request.BeginGetResponse(RespCallback, request);
        //    } catch (Exception) {
        //        //MessageBox.Show(e.Message);
        //    }
        //}

        //private void RespCallback(IAsyncResult asyncResult) {
        //    try {
        //        HttpWebRequest request = (HttpWebRequest)asyncResult.AsyncState;
        //        HttpWebResponse response = (HttpWebResponse)request.EndGetResponse(asyncResult);
        //        using (StreamReader reader = new StreamReader(response.GetResponseStream())) {
        //            Version siteVersion = new Version(reader.ReadToEnd());
        //            Version thisVersion = System.Reflection.Assembly.GetExecutingAssembly().GetName().Version;
        //            if (siteVersion > thisVersion) {
        //                //MessageBox.Show("New Version is available!");
        //                notifyIcon.BalloonTipIcon = ToolTipIcon.Info;
        //                notifyIcon.BalloonTipTitle = "";
                        
        //                notifyIcon.BalloonTipText = String.Format("New version of LT10 version is available ({0})", siteVersion.ToString());
        //                notifyIcon.ShowBalloonTip(5000);
        //            }
        //        }   
        //    } catch (Exception) {
        //        //MessageBox.Show(e.Message);
        //    }
        //}

        private void RecentProjectClick(object sender, EventArgs args) {
            OpenProject(((ToolStripMenuItem)sender).Text);
        }

        //void SpectraContainerBase_SaveProgressChanged(object sender, System.ComponentModel.ProgressChangedEventArgs e) {
        //    try {
        //        tsMainProgressBar.Value++;
        //    } catch {
        //        tsMainProgressBar.Value = tsMainProgressBar.Maximum;
        //    }
        //}

        void SpectraContainerBase_OpenSaveProgressChanged(object sender, System.ComponentModel.ProgressChangedEventArgs e) {
            try {
                if (tsMainProgressBar.Value + 1<=tsMainProgressBar.Maximum)
                    tsMainProgressBar.Value++;
            } catch (Exception) {
                //tsMainProgressBar.Value = tsMainProgressBar.Maximum;
                //MessageBox.Show("Inner exception:" + ex.Message, Application.ProductName, MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }

        private void OpenFile(object sender, EventArgs e) {
            OpenFileDialog openFileDialog = new OpenFileDialog();
            toolStripStatusLabel.Text = "Opening";
            openFileDialog.InitialDirectory = ProjectsPath;
            openFileDialog.Filter = "LT10 Project Files (*.ltp;*.ltpi;*.ltpe;*.ltpp)|*.ltp;*.ltpi;*.ltpe;*.ltpp|All Files (*.*)|*.*";
            if (openFileDialog.ShowDialog(this) == DialogResult.OK) {
                OpenProject(openFileDialog.FileName);
            }
            openFileDialog.Dispose();
            toolStripStatusLabel.Text = "Ready";
        }

        private void SetStatusText(string status) {
            this.toolStripStatusLabel.Text = status;
        }

        private void OpenProject(string projectFile) {
            tsMainProgressBar.Visible = true;
            try {
                tsMainProgressBar.Maximum = 0;
                //foreach (string dir in System.IO.Directory.GetDirectories(System.IO.Path.GetDirectoryName(projectFile), "spectra", System.IO.SearchOption.AllDirectories))
                //    tsMainProgressBar.Maximum += System.IO.Directory.GetFiles(dir, "*.*", System.IO.SearchOption.TopDirectoryOnly).Length;
                //}
                IProject project = AvailableAssemblies.getProject(projectFile, new ReturnAttributeValue(delegate(string name, string value) {
                    if (name == "spectraCount")
                        tsMainProgressBar.Maximum = int.Parse(value);
                }));
                CreateProjectWindow(project);
                tsMainProgressBar.Value = tsMainProgressBar.Maximum;
                //adding to recent projects list
                refreshRecentProjectsList(projectFile);
                appendProjectToRecoveryFile(projectFile);
            } catch (SpectrumLoadException sle) {
                MessageBox.Show(sle.Message, Application.ProductName, MessageBoxButtons.OK, MessageBoxIcon.Warning);
            } catch (System.Reflection.TargetInvocationException tiex) {
                Exception finalException = Evel.share.Utilities.findException(tiex);
                if (finalException.GetType() == typeof(FileNotFoundException) || 
                    finalException.GetType() == typeof(SpectrumLoadException) ||
                    finalException.GetType() == typeof(IOException)) {
                    MessageBox.Show(finalException.Message, Application.ProductName, MessageBoxButtons.OK, MessageBoxIcon.Error);
                } else {
                    ExceptionSendForm exForm = new ExceptionSendForm(finalException);
                    exForm.ShowDialog();
                }
            } catch (System.IO.FileNotFoundException fnfe) {
                MessageBox.Show(fnfe.Message, Application.ProductName, MessageBoxButtons.OK, MessageBoxIcon.Error);
            } catch (System.IO.DirectoryNotFoundException dnfe) {
                MessageBox.Show(dnfe.Message, Application.ProductName, MessageBoxButtons.OK, MessageBoxIcon.Error);
            } catch (Exception ex) {
                //System.Windows.Forms.MessageBox.Show(ex.Message, "LT10", MessageBoxButtons.OK, MessageBoxIcon.Error);
                ExceptionSendForm exForm = new ExceptionSendForm(ex);
                exForm.ShowDialog();
            }
            tsMainProgressBar.Visible = false;
        }

        private void refreshRecentProjectsList(string projectFile) {
            if (_recentProjects.IndexOf(projectFile) >= 0)
                _recentProjects.Remove(projectFile);
            _recentProjects.Insert(0, projectFile);
            if (_recentProjects.Count > maxRecentProjects)
                _recentProjects.RemoveAt(_recentProjects.Count - 1);
            WriteRegistry();
        }

        private void appendProjectToRecoveryFile(string projectFile) {
            //check if project is not already there
            List<string> projects = new List<string>();
            try {
                using (TextReader reader = new StreamReader(recoveryProjectsFile)) {
                    string projectPath;
                    while ((projectPath = reader.ReadLine()) != null)
                        if (!projects.Contains(projectPath))
                            projects.Add(projectPath);
                }
            } catch (Exception) { }
            //if project has not been found in recovery file, add this project
            if (!projects.Contains(projectFile)) {
                projects.Add(projectFile);
                //save the project path to the file in case program will not quit properly so after reset those projects can be reopened
                try {
                    using (System.IO.TextWriter writer = new System.IO.StreamWriter(recoveryProjectsFile, false)) {
                        for (int i = 0; i < projects.Count; i++)
                            writer.WriteLine(projects[i]);
                    }
                } catch (Exception) { }
            }
        }

        public static void removeProjectFromRecoveryFile(string projectFile) {
            try {
                TextReader reader = new StreamReader(recoveryProjectsFile);
                string line;
                List<string> lines = new List<string>();
                while ((line = reader.ReadLine()) != null)
                    if (line != projectFile) lines.Add(line);
                reader.Close();
                TextWriter writer = new StreamWriter(recoveryProjectsFile, false);
                foreach (string l in lines)
                    writer.WriteLine(l);
                writer.Close();
            } catch (Exception) { }
        }

        public static void WriteRegistry() {
            //switching to search after calculations run
            Registry.SetValue(_registryGUIKeyName, "SwitchToSearchTab", switchToSearch.ToString(), RegistryValueKind.String);
            //saving before calculations
            Registry.SetValue(_registryGUIKeyName, "SaveBeforeFitting", savebeforefitting, RegistryValueKind.DWord);
            //recent projects
            string[] recentProjectsPaths = new string[_recentProjects.Count];
            _recentProjects.CopyTo(recentProjectsPaths);
            Registry.SetValue(_registryGUIKeyName, "RecentProjects", recentProjectsPaths, RegistryValueKind.MultiString);
            Registry.SetValue(_registryGUIKeyName, "MaxRecentProjects", maxRecentProjects, RegistryValueKind.DWord);
            //paths
            Registry.SetValue(_registryGUIKeyName, "projectsPath", projectsPath, RegistryValueKind.String);
            Registry.SetValue(_registryGUIKeyName, "commonSpectraPath", commonSpectraPath, RegistryValueKind.String);
            //status colors
            Registry.SetValue(_registryGUIKeyName, "LocalFreeColor", ((Color)StatusColors[ParameterStatus.Local | ParameterStatus.Free]).ToArgb(), RegistryValueKind.DWord);
            Registry.SetValue(_registryGUIKeyName, "LocalFixedColor", ((Color)StatusColors[ParameterStatus.Local | ParameterStatus.Fixed]).ToArgb(), RegistryValueKind.DWord);
            Registry.SetValue(_registryGUIKeyName, "CommonFreeColor", ((Color)StatusColors[ParameterStatus.Common | ParameterStatus.Free]).ToArgb(), RegistryValueKind.DWord);
            Registry.SetValue(_registryGUIKeyName, "CommonFixedColor", ((Color)StatusColors[ParameterStatus.Common | ParameterStatus.Fixed]).ToArgb(), RegistryValueKind.DWord);
            Registry.SetValue(_registryGUIKeyName, "BindedFreeColor", ((Color)StatusColors[ParameterStatus.Binding | ParameterStatus.Free]).ToArgb(), RegistryValueKind.DWord);
            Registry.SetValue(_registryGUIKeyName, "BindedFixedColor", ((Color)StatusColors[ParameterStatus.Binding | ParameterStatus.Fixed]).ToArgb(), RegistryValueKind.DWord);
            Registry.SetValue(_registryGUIKeyName, "RestoreCount", parameterRestoreCount, RegistryValueKind.DWord);
            Registry.SetValue(_registryGUIKeyName, "uversion", uversion, RegistryValueKind.String);
        }

        private ProjectForm CreateProjectWindow(IProject project) {
            ProjectForm form = new ProjectForm(project); //, EventsTextBox);
            form.MdiParent = this;
            form.Text = project.Caption;
            form.Show();
            return form;
        }

        private void ExitToolsStripMenuItem_Click(object sender, EventArgs e) {
            this.Close();
        }

        private void CutToolStripMenuItem_Click(object sender, EventArgs e) {
        }

        private void CopyToolStripMenuItem_Click(object sender, EventArgs e) {
        }

        private void PasteToolStripMenuItem_Click(object sender, EventArgs e) {
        }

        private void ToolBarToolStripMenuItem_Click(object sender, EventArgs e) {
            toolStrip.Visible = menuItemToolbar.Checked;
        }

        private void StatusBarToolStripMenuItem_Click(object sender, EventArgs e) {
            statusStrip.Visible = menuItemStatusBar.Checked;
        }

        private void CascadeToolStripMenuItem_Click(object sender, EventArgs e) {
            LayoutMdi(MdiLayout.Cascade);
        }

        private void TileVerticalToolStripMenuItem_Click(object sender, EventArgs e) {
            LayoutMdi(MdiLayout.TileVertical);
        }

        private void TileHorizontalToolStripMenuItem_Click(object sender, EventArgs e) {
            LayoutMdi(MdiLayout.TileHorizontal);
        }

        private void ArrangeIconsToolStripMenuItem_Click(object sender, EventArgs e) {
            LayoutMdi(MdiLayout.ArrangeIcons);
        }

        private void CloseAllToolStripMenuItem_Click(object sender, EventArgs e) {
            foreach (Form childForm in MdiChildren) {
                childForm.Close();
            }
        }

        //private void bottomToolbarToolStripMenuItem_Click(object sender, EventArgs e) {
        //    panel1.Visible = ((ToolStripMenuItem)sender).Checked;
        //}

        //private void panel2_MouseMove(object sender, MouseEventArgs e) {
        //    if (e.Button == MouseButtons.Left)
        //        panel1.Height += mouseY - e.Y;
        //}

        //private void panel2_MouseDown(object sender, MouseEventArgs e) {
        //    mouseY = e.Y;
        //}

        private void SaveAsToolStripMenuItem_Click(object sender, EventArgs e) {
            if (this.ActiveMdiChild is ProjectForm)
                SaveProject((ProjectForm)this.ActiveMdiChild, true);
        }

        private void SaveProject(ProjectForm projectForm, string projectFilePath, ProjectFileType fileType) {
            //toolStripStatusLabel.Text = "Saving";
            try {
                tsMainProgressBar.Visible = true;
                tsMainProgressBar.Maximum = 0;
                //SavingSpectraCount = 0;
                //if (Path.GetExtension(project
                foreach (ISpectraContainer container in projectForm.project.Containers)
                    tsMainProgressBar.Maximum += container.Spectra.Count;
                //if (projectPath == System.IO.Path.GetDirectoryName(projectForm.project.Path))
                if (projectFilePath == projectForm.project.ProjectFile)
                    projectForm.project.Save(projectForm.project.ProjectFile, fileType);
                else {
                    //share.Utilities.CopyDirectory(projectForm.project.Path, projectFile);
                    projectForm.project.Save(projectFilePath, fileType);
                    //projectForm.project.Path = projectFile;
                }
                projectForm.NotSavedChanges = false;
                projectForm.Text = projectForm.project.Caption;
                //toolStripStatusLabel.Text = "Ready";
                tsMainProgressBar.Visible = false;
                appendProjectToRecoveryFile(projectForm.project.ProjectFile);
                refreshRecentProjectsList(projectForm.project.ProjectFile);
                ReadRegistry();
            } catch (Exception e) {
                MessageBox.Show(String.Format("Couldn't save project ({0}).", e.Message), Application.ProductName, MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        public DialogResult SaveProject(ProjectForm projectForm, bool newLocation) {
            DialogResult result;
            if (projectForm.project.Path == null || newLocation) {
                removeProjectFromRecoveryFile(projectForm.project.ProjectFile);
                SaveFileDialog saveFileDialog = new SaveFileDialog();
                saveFileDialog.Title = "Type name of folder new project will be saved to";
                saveFileDialog.InitialDirectory = ProjectsPath;
                saveFileDialog.Filter = "Plain LT10 Project (*.ltp)|*.ltp|Plain LT10 Project (compr) (*.ltpi)|*.ltpi|Referenced LT10 Project (*.ltpe)|*.ltpe|Packed LT10 Project (*.ltpp)|*.ltpp";
                saveFileDialog.FilterIndex = 4;
                saveFileDialog.FileName = Path.GetFileNameWithoutExtension(projectForm.project.Caption);
                saveFileDialog.AddExtension = false;
                string projectFile;
                ProjectFileType pft;
                if ((result = saveFileDialog.ShowDialog(this)) == DialogResult.OK) {
                    pft = (ProjectFileType)saveFileDialog.FilterIndex;
                    projectFile = saveFileDialog.FileName;
                    if (pft == ProjectFileType.Normal || pft == ProjectFileType.CompressedIS)
                        projectFile = Path.Combine(projectFile, Path.GetFileName(projectFile));
                    projectFile = Path.ChangeExtension(projectFile, ProjectFileExtensions.GetExtension(pft));
                    SaveProject(projectForm, projectFile, pft);
                }
                saveFileDialog.Dispose();
            } else {
                SaveProject(projectForm, projectForm.project.ProjectFile, projectForm.project.FileType);
                result = DialogResult.OK;
            }
            return result;
        }

        public void saveToolStripButton_Click(object sender, EventArgs e) {
            if (this.ActiveMdiChild is ProjectForm)
                SaveProject((ProjectForm)this.ActiveMdiChild, false);
            //    toolStripStatusLabel.Text = "Saving";
            //    tsMainProgressBar.Visible = true;
            //    IProject project = ((ProjectForm)this.ActiveMdiChild).project;

            //    if (project.Path != null) {

            //        //SavingSpectraCount = 0;
            //        tsMainProgressBar.Maximum = 0;
            //        foreach (ISpectraContainer container in project.Containers)
            //            tsMainProgressBar.Maximum += container.Spectra.Count;
            //        project.Save(project.Path, false, project.Compressed);
            //        ((ProjectForm)this.ActiveMdiChild).NotSavedChanges = false;
            //        tsMainProgressBar.Visible = false;
            //        toolStripStatusLabel.Text = "Ready";
            //        appendProjectToRecoveryFile(project.ProjectFile);
            //        refreshRecentProjectsList(project.ProjectFile);
            //        ReadRegistry();
            //    } else
            //        SaveAsToolStripMenuItem_Click(sender, e);
                    
            //}
        }

        private void newToolStripMenuItem_Click(object sender, EventArgs e) {
            CreateNewProject(null);
        }

        public void CreateNewProject(WizardEventHandler wizardEventHandler) {
            NewProjectDialog npd = new NewProjectDialog(null);
            npd = new NewProjectDialog(null);
            if (this.CurrentWizard != null) this.CurrentWizard.DialogOpened(npd);
            toolStripStatusLabel.Text = "Creating new project";
            if (npd.ShowDialog() == DialogResult.OK) {
                //OpeningSpectraCount = 0;
                tsMainProgressBar.Maximum = 0;
                tsMainProgressBar.Visible = true;
                //toolStripStatusLabel.Text = String.Format("Creating Project {0}...", npd.projectNameTextBox.Text);
                foreach (SpectraContainerDescription scd in npd.ContainerDescriptions)
                    tsMainProgressBar.Maximum += scd.spectraPaths.Length;
                string defaultProjectName = getDefaultProjectName();
                IProject project = AvailableAssemblies.getProject(
                    npd.ProjectType.ToString(), 
                    new object[] { 
                        defaultProjectName, //npd.projectNameTextBox.Text, 
                        npd.ContainerDescriptions });
                //project.Save(System.IO.Path.Combine(npd.projectPathTextBox.Text, npd.projectNameTextBox.Text), false);
                ProjectForm form = CreateProjectWindow(project);
                form.NotSavedChanges = true;
                tsMainProgressBar.Visible = false;
                if (wizardEventHandler != null)
                    wizardEventHandler(this, new WizardEventArgs(form, WizardEventType.Created));
            }
            if (CurrentWizard != null) CurrentWizard.DialogClosed();
            npd.Dispose();
            toolStripStatusLabel.Text = "Ready";
        }

        private string getDefaultProjectName() {
            int i = 1;
            string name;
            if (Directory.Exists(ProjectsPath)) {
                while (Directory.Exists(name = Path.Combine(ProjectsPath, String.Format("LtProject{0}", i))) && i < 1000)
                    i++;
            } else
                name = "LT10Project";
            return Path.GetFileName(name);
        }

        //public static void WriteFormatedText(string text, RichTextBox textBox) {
        //    string[] parts = text.Split(new char[] { '\0' }, StringSplitOptions.RemoveEmptyEntries);
        //    foreach (string part in parts) {
        //        if (part.IndexOf("greek:") == 0) {
        //            string s = part.Substring(6);
        //            textBox.AppendText(s);
        //            textBox.Select(textBox.Text.Length - s.Length, s.Length);
        //            textBox.SelectionFont = new Font("Symbol", textBox.Font.Size, textBox.Font.Style);
        //            textBox.SelectionCharOffset = 0;
        //            textBox.SelectionBullet = false;
        //        } else {
        //            if (part.IndexOf("sup:") == 0) {
        //                string s = part.Substring(4);
        //                textBox.AppendText(s);
        //                textBox.Select(textBox.Text.Length - s.Length, s.Length);
        //                textBox.SelectionFont = textBox.Font;
        //                textBox.SelectionCharOffset = 3;
        //                textBox.SelectionBullet = false;
        //            } else {
        //                if (part.IndexOf("bul:") == 0) {
        //                    string s = part.Substring(4);
        //                    textBox.AppendText(s);
        //                    textBox.Select(textBox.Text.Length - s.Length, s.Length);
        //                    textBox.SelectionFont = textBox.Font;
        //                    textBox.SelectionCharOffset = 0;
        //                    textBox.SelectionBullet = true;
        //                } else {
        //                    if (part.IndexOf("sub:") == 0) {
        //                        string s = part.Substring(4);
        //                        textBox.AppendText(s);
        //                        textBox.Select(textBox.Text.Length - s.Length, s.Length);
        //                        textBox.SelectionFont = textBox.Font;
        //                        textBox.SelectionCharOffset = -3;
        //                        textBox.SelectionBullet = false;
        //                    } else {
        //                        string s = part.Substring(0);
        //                        textBox.AppendText(s);
        //                        textBox.Select(textBox.Text.Length - s.Length, s.Length);
        //                        textBox.SelectionFont = textBox.Font;
        //                        textBox.SelectionCharOffset = 0;
        //                        textBox.SelectionBullet = false;
        //                    }
        //                }
        //            }
        //        }
        //    }
        //}

        public static void DialogPanel_Paint(object sender, PaintEventArgs e) {
            try {
                Rectangle rect = ((Panel)sender).ClientRectangle;
                e.Graphics.FillRectangle(new System.Drawing.Drawing2D.LinearGradientBrush(rect, SystemColors.Control, Color.White, 270, false), rect);
            } catch { }
        }

        private void EventsTextBox_TextChanged(object sender, EventArgs e)
        {

        }

        public static Color GetColor(ParameterStatus status) {
            if (status == ParameterStatus.None)
                return Color.Silver;
            else if ((status & ParameterStatus.Binding) > 0)
                return (Color)StatusColors[status & ~(ParameterStatus.Local | ParameterStatus.Common)];
            else
                return (Color)StatusColors[status];
        }

        private void optionsToolStripMenuItem_Click(object sender, EventArgs e) {
            OptionsForm form = new OptionsForm();
            if (form.ShowDialog() == DialogResult.OK) {
                WriteRegistry();
                foreach (Form f in this.MdiChildren) {
                    if (f is ProjectForm)
                        ((ProjectForm)f).RepaintGrids();
                }
            }
        }

        private void contentsToolStripMenuItem_Click(object sender, EventArgs e) {
            Help.ShowHelp(this, helpfile);

        }

        private void indexToolStripMenuItem_Click(object sender, EventArgs e) {
            Help.ShowHelpIndex(this, helpfile);
        }

        private void aboutToolStripMenuItem_Click(object sender, EventArgs e) {
            AboutBox aboutbox = new AboutBox();
            aboutbox.ShowDialog();
            aboutbox.Dispose();
        }

        private void startWizard(string wizardFile) {
            CurrentWizard = new EvelWizard(System.IO.File.Open(wizardFile, System.IO.FileMode.Open));
            //MessageBox.Show(wizard.CurrentStep.html);
        }

        private void startWizardToolStripMenuItem_Click(object sender, EventArgs e) {
            if (startWizardToolStripMenuItem.Visible) {
                OpenFileDialog dialog = new OpenFileDialog();
                dialog.Filter = "Wizard files|*.xml";
                if (dialog.ShowDialog() == DialogResult.OK) {
                    startWizard(dialog.FileName);
                }
            } else {
                MessageBox.Show("This feature will be available in full version of LT10", Application.ProductName, MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
        }

        private void newWindowToolStripMenuItem_Click(object sender, EventArgs e) {
            throw new Exception("test");
        }

        private void MainForm_FormClosing(object sender, FormClosingEventArgs e) {
            //foreach (Form form in this.MdiChildren)
            //    if (form is ProjectForm)
            //        if (((ProjectForm)form).project.IsBusy)
            //            waitForm.AddProject(((ProjectForm)form).project);
            //e.Cancel = !waitForm.CanClose;
            foreach (Form form in this.MdiChildren)
                ((ProjectForm)form).CancelSearching();
            Exiting = true;
        }

        internal void CloseForm() {
            SpectraContainerBase.OpenProgressChanged -= SpectraContainerBase_OpenSaveProgressChanged;
            SpectraContainerBase.SaveProgressChanged -= SpectraContainerBase_OpenSaveProgressChanged;
            Close();
        }

        internal void waitForm_ProjectRemoved(object sender, EventArgs e) {
            Close();
        }

        internal bool Exiting {
            get { return this._exiting; }
            set {
                if (!this._exiting && value)
                    waitForm.ProjectRemoved += waitForm_ProjectRemoved;
                else if (!value)
                    waitForm.ProjectRemoved -= waitForm_ProjectRemoved;
                this._exiting = value;
                //if (this._exiting = value) {
                //    foreach (Form form in this.MdiChildren)
                //        if (form is ProjectForm)
                //            ((ProjectForm)form).Exiting = false;
                //}
            }
        }

        private void notifyIcon_BalloonTipClicked(object sender, EventArgs e) {
            this.WindowState = FormWindowState.Maximized;
            this.Activate();            
            if (this.baloonForm != null) {
                //this.baloonProjectForm.Show();
                //this.baloonProjectForm.Activate();
                if (this.baloonForm is ProjectForm)
                    ((ProjectForm)this.baloonForm).projectTabControl.SelectedTab = ((ProjectForm)this.baloonForm).SearchTabPage;
                else
                    this.baloonForm.ShowDialog();
            }
        }

        //private void MainForm_Load(object sender, EventArgs e) {

        //}

    }
}