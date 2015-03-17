using System;
using System.ComponentModel;
using System.Drawing;
using System.Windows.Forms;
using System.Collections.Generic;
using Evel.interfaces;
using Evel.engine.algorythms;
using Evel.share;
using System.Text;
using System.Runtime.InteropServices;
using GemBox.Spreadsheet;
using Evel.engine;
using System.Threading;
using Evel.gui.interfaces;
using Evel.engine.exceptions;
using Evel.gui.dialogs;
using System.IO;

namespace Evel.gui {

    [ComVisible(true)]
    public partial class ProjectForm : Form {

        internal LinkedList<ChangeStep> undoSteps;
        internal LinkedList<ChangeStep> redoSteps;
        public IProject project;
        //private RichTextBox eventsLog;
        private long _counterFreq;
        private long _counterStart;
        private long _counterStop;
        private bool _notSavedChanges = false;
        public IParameter currentBindingParameter;
        private TabControl hiddenDocsTabControl;
        internal List<SpectraContainerTabPage> documentTabs;
        //private List<IParameter> fixedIndParameters; //fixed independent parameters
        private Dictionary<IParameter, int> fixedIndParameters;
        //private HashSet<IParameter> commonParameters;
        private List<ISpectrum> searchingSpectra;

        //private delegate void PerformClickCallBack(RadioButton radioButton);
        private delegate void CheckRadioButtonCallBack(RadioButton radioButton);
        private delegate void SetEventTextCallBack(string text);
        //private delegate void SetReadonlyCallBack(bool readOnly);
        //private digit.Series searchSeries = null;
        private digit.LabeledSeries searchSeries = null;
        private WebBrowser webBrowser1;
        private bool _exiting;
        //private bool _searchingUndone;

        #region Construction

        protected ProjectForm() {
            InitializeComponent();
            this.commonValuesGrid.ReadonlyCellBackColor = SystemColors.Window;
            this.WindowState = FormWindowState.Maximized;
            hiddenDocsTabControl = new TabControl();
            documentTabs = new List<SpectraContainerTabPage>();
            //properties tab control and tabController
            propertiesTabControl.ItemSize = new Size(0, 1);
            for (int i = 0; i < 3; i++)
                tabController.Items.Add(propertiesTabControl.TabPages[i]);
            //foreach (TabPage page in propertiesTabControl.TabPages) {
            //    tabController.Items.Add(page);
            //}
            tabController.SelectedIndex = 0;
            fixedIndParameters = new Dictionary<IParameter, int>();// new List<IParameter>();
            //commonParameters = new HashSet<IParameter>();
            this._exiting = false;
            undoSteps = new LinkedList<ChangeStep>();
            redoSteps = new LinkedList<ChangeStep>();
        }

        public ProjectForm(IProject project)
            : this() {
            //this.eventsLog = eventsLog;
            this.project = project;
            if (project.CalculatedValues)
                CheckRadioButton(PrevCalcRadioButton);
            else
                CheckRadioButton(SpecRadioButton);
            //webBrowser1.ObjectForScripting = this;
            project.SearchCompleted += new AsyncCompletedEventHandler(project_SearchCompleted);
            project.SearchProgressChanged += new ProgressChangedEventHandler(project_SearchProgressChanged);
            project.FirstSpectraSearchCompleted += new AsyncFirstSpectraSearchCompletedEventHandler(project_FirstSpectraSearchCompleted);
            project.FirstSpectraSearchProgressChanged += new ProgressChangedEventHandler(project_FirstSpectraSearchProgressChanged);
            project.IndependencyFound += new IndependencyFoundEventHandler(project_IndependencyFound);
            project.IndefiniteMatrixGot += new IndefiniteMatrixEventHandler(project_IndefiniteMatrixGot);
            CreateContainerTabs();
            CreateBoundedGroupTabPages();
            projectTabControl.SelectedIndex = 2;
            Performancer.QueryPerformanceFrequency(ref _counterFreq);
            _notSavedChanges = false;
            ProjectForm_Resize(this, new EventArgs());

            //documents grid
            FillDocumentsGrid();
        }

        internal void AddUndoStep(ChangeStep step) {
            redoSteps.Clear();
            if (undoSteps.Count > 0) {
                if (!undoSteps.First.Value.Equals(step))
                    undoSteps.AddFirst(step);
                if (undoSteps.Count > 5)
                    undoSteps.RemoveLast();
            } else
                undoSteps.AddFirst(step);
            RefreshUndoRedoTexts();
        }

        private void CreateContainerTabs() {
            foreach (ISpectraContainer container in project.Containers) {
                CreateContainerTab(container);
            }
        }

        private void CreateContainerTab(ISpectraContainer container) {
            SpectraContainerTabPage tab = new SpectraContainerTabPage(container, this); // CreateSpectraContainerPage(container);
            if (container.Enabled)
                projectTabControl.TabPages.Add(tab);
            else
                hiddenDocsTabControl.TabPages.Add(tab);
            documentTabs.Add(tab);
        }

        /// <summary>
        /// Creates pages for all bindings defined in project
        /// </summary>
        private void CreateBoundedGroupTabPages() {
            for (int i = 0; i < project.BindingsManager.Count; i++) {
                if (project.BindingsManager[i] is GroupBinding)
                    CreateBoundedGroupTabPages((GroupBinding)project.BindingsManager[i]);
            }
        }

        /// <summary>
        /// Create pages for binding
        /// </summary>
        /// <param name="binding"></param>
        private void CreateBoundedGroupTabPages(GroupBinding binding) {
            int d;
            List<ISpectrum> spectra;
            //List<ISpectrum> bs;
            SharedGroupTabPage groupTabPage;
            spectra = new List<ISpectrum>();
            for (d = 0; d < binding.Containers.Length; d++)
                spectra.AddRange(binding.Containers[d].Spectra);

            bool skipPage;
            for (d = 0; d < binding.Groups.Length; d++) {
                //check if group for this binding is not already created
                skipPage = false;
                foreach (TabPage page in projectTabControl.TabPages)
                    if (page is SharedGroupTabPage)
                        if (((SharedGroupTabPage)page).Binding == binding && ((SharedGroupTabPage)page).GroupDefinition.name == binding.Groups[d]) {
                            skipPage = true;
                            break;
                        }
                if (!skipPage) {
                    //bs = new List<ISpectrum>(spectra);
                    groupTabPage = new SharedGroupTabPage(spectra, spectra[0].Parameters[binding.Groups[d]].Definition, statusStrip1, projectTabControl, this, binding);
                    projectTabControl.TabPages.Add(groupTabPage);
                }
            }
            //hide empty docs
            foreach (SpectraContainerTabPage page in documentTabs)
                page.Parent = page._groupsControl.TabCount > 0 ? projectTabControl : hiddenDocsTabControl;
        }

        private void ModifyBoundedGroupTabPages(GroupBinding binding) {
            SharedGroupTabPage spage;
            List<SharedGroupTabPage> spages = new List<SharedGroupTabPage>();
            HashSet<ISpectraContainer> removedContainers = new HashSet<ISpectraContainer>();
            HashSet<ISpectraContainer> presentContainers = new HashSet<ISpectraContainer>();

            int i;
            foreach (TabPage page in projectTabControl.TabPages)
                if (page is SharedGroupTabPage)
                    if ((spage = (SharedGroupTabPage)page).Binding == binding) {
                        //if this page is present in this binding modify otherwise remove this page and create all group pages in document pages
                        if (binding.ContainsGroup(spage.GroupDefinition.name)) {
                            //remove from page those spectra, which container doesn't belong to the binding any more
                            for (i = 0; i < spage.Spectra.Count; )
                                if (!binding.ContainsContainer(spage.Spectra[i].Container)) {
                                    removedContainers.Add(spage.Spectra[i].Container);
                                    spage.Spectra.Remove(spage.Spectra[i]);
                                } else {
                                    presentContainers.Add(spage.Spectra[i].Container);
                                    i++;
                                }
                            //create group pages in all documents whose spectra were just removed
                            foreach (ISpectraContainer container in removedContainers)
                                for (i = 0; i < documentTabs.Count; i++)
                                    if (documentTabs[i].SpectraContainer == container)
                                        documentTabs[i].InsertGroupTabPage(spage.GroupDefinition.name);
                            //add containers yet not present in shared page
                            for (i = 0; i < binding.Containers.Length; i++)
                                if (!presentContainers.Contains(binding.Containers[i]))
                                    spage.Spectra.AddRange(binding.Containers[i].Spectra);
                            spage.Reset();
                        } else {
                            //this group were removed in dialog. Remove this page and insert group pages in documents
                            spages.Add(spage);
                            for (i = 0; i < documentTabs.Count; i++)
                                if (binding.ContainsContainer(documentTabs[i].SpectraContainer))
                                    documentTabs[i].InsertGroupTabPage(spage.GroupDefinition.name);
                        }
                    }
            removeBoundedGroupsFromDocs(binding);
            CreateBoundedGroupTabPages(binding);
            while (spages.Count > 0) {
                projectTabControl.TabPages.Remove(spages[0]);
                spages.RemoveAt(0);
            }
        }

        public void RemoveBoundedGroupTabPages(GroupBinding binding) {
            int i = projectTabControl.TabPages.Count - 1;
            while (i > 0 && projectTabControl.TabPages.Count > 0) {
                if (projectTabControl.TabPages[i] is SharedGroupTabPage) {
                    SharedGroupTabPage page = (SharedGroupTabPage)projectTabControl.TabPages[i];
                    if (page.Binding == binding)
                        projectTabControl.TabPages.RemoveAt(i);
                }
                i--;
            }
        }


        #endregion Construction

        public void RepaintGrids() {
            foreach (Control groupControl in projectTabControl.Controls.Find("groupsControl", true)) {
                foreach (TabPage page in ((TabControl)groupControl).TabPages)
                    if (page is GroupTabPage) {
                        DataGridParameterView grid = ((GroupTabPage)page).grid;
                        foreach (DataGridViewRow row in grid.Rows) {
                            foreach (DataGridViewCell cell in row.Cells) {
                                if (cell is DataGridViewComboBoxCell && cell.Value != null) {
                                    ParameterStatus status = (ParameterStatus)(Int32.Parse(cell.Value.ToString()));
                                    cell.Style.ForeColor = MainForm.GetColor(status);
                                }
                            }
                        }
                        grid.Invalidate();
                    }
            }
        }

        public bool NotSavedChanges {
            get {
                if (_notSavedChanges) return true;
                if (projectTabControl.Controls.Find("groupsControl", true).Length > 0) {
                    foreach (Control groupControl in projectTabControl.Controls.Find("groupsControl", true)) { //foreach document
                        if (groupControl is TabControl) {
                            foreach (TabPage page in ((TabControl)groupControl).TabPages) {
                                if (((GroupTabPage)page).notSavedChanges)
                                    return true;
                            }
                        }
                    }
                }
                return false;
            }
            set {
                this._notSavedChanges = value;
                if (projectTabControl.Controls.Find("groupsControl", true).Length > 0) {
                    foreach (Control groupControl in projectTabControl.Controls.Find("groupsControl", true)) { //foreach document
                        if (groupControl is TabControl) {
                            foreach (TabPage page in ((TabControl)groupControl).TabPages)
                                ((GroupTabPage)page).notSavedChanges = value;
                        }
                    }
                }
            }
        }

        void project_IndefiniteMatrixGot(object sender, ISpectrum spectrum, ParameterStatus status) {
            StringBuilder message = new StringBuilder();
            message.AppendFormat("[p text='Calculating linear parameters in spectrum {0} resulted in indefinite matrix. Spectrum parameters has been restored to their starting values.']",
                spectrum.Name);
            project.RestoreSpectrumStartingValues(spectrum, status);
            if (eventsLog.InvokeRequired) {
                this.Invoke(new ThreadStart(delegate() {
                    SetEventText(message.ToString());
                    if (project.SearchMode == SearchMode.Main && (Program.MainWindow.ActiveMdiChild != this || Form.ActiveForm == null)) {
                        Program.MainWindow.notifyIcon.BalloonTipTitle = project.Caption;
                        Program.MainWindow.notifyIcon.BalloonTipText = String.Format("Indefinite matrix while calculating {0}", spectrum.Name);
                        Program.MainWindow.notifyIcon.BalloonTipIcon = ToolTipIcon.Warning;
                        Program.MainWindow.notifyIcon.ShowBalloonTip(1000);
                        Program.MainWindow.baloonForm = this;
                    }
                }));
                    //SetEventTextCallBack(SetEventText), new object[] { message.ToString() });
            }
        }

        void project_IndependencyFound(object parameterOwner, IParameter parameter) {
            StringBuilder message = new StringBuilder();
            bool local;
            string locationstr = String.Empty;
            ParameterLocation location = project.GetParameterLocation(parameter);
            ISpectrum spectrum = project.Containers[location.docId].Spectra[location.specId];
            if (local = (parameter.Status & ParameterStatus.Local) > 0 || project.SearchMode != SearchMode.Main) {
                locationstr = String.Format("[p text='\n {0} -> {1} -> {2} -> ']", project.Containers[location.docId].Name, spectrum.Name, spectrum.Parameters[location.groupId].Definition.name);
                //location.compId++;
            } else
                location.compId = -1;

            message.AppendFormat("[p text='Theoretical function does not depend on ']{0}{1}[p text='  parameter.\n']",
                locationstr != String.Empty ? locationstr : "global ",
                DefaultGroupGUI.BuildFormatedString(parameter.Definition.Header, DefaultGroupGUI.StringFormatTarget.ParameterDataGrid, (location.compId + 1).ToString())
                );

            if (!fixedIndParameters.ContainsKey(parameter))
                fixedIndParameters.Add(parameter, 0);

            if (fixedIndParameters[parameter] < MainForm.parameterRestoreCount || MainForm.parameterRestoreCount == -1) {
                project.RestoreParameter(spectrum, location);
                message.AppendFormat("[p text='Parameter has been restored to its starting value of {0:G6} ({1})']", parameter.Value, ++fixedIndParameters[parameter]);
            } else {
                parameter.Status = (parameter.Status & ~ParameterStatus.Free) | ParameterStatus.Fixed;
                message.Append("[p text='Parameter has been fixed']");
            }
            if (eventsLog.InvokeRequired) {
                this.Invoke(new ThreadStart(delegate() {
                    SetEventText(message.ToString());
                    if (project.SearchMode == SearchMode.Main && (Program.MainWindow.ActiveMdiChild != this || Form.ActiveForm == null)) {
                        Program.MainWindow.notifyIcon.BalloonTipTitle = project.Caption;
                        Program.MainWindow.notifyIcon.BalloonTipText = String.Format("Independency found");
                        Program.MainWindow.notifyIcon.BalloonTipIcon = ToolTipIcon.Warning;
                        Program.MainWindow.notifyIcon.ShowBalloonTip(1000);
                        Program.MainWindow.baloonForm = this;
                    }
                }));

                    //new SetEventTextCallBack(SetEventText), new object[] { message.ToString() });
            }
        }

        void project_FirstSpectraSearchProgressChanged(object sender, ProgressChangedEventArgs e) {
            Performancer.QueryPerformanceCounter(ref _counterStop);
            FitProgressChangedEventArgs me = (FitProgressChangedEventArgs)e; // (MinsqProgressChangedEventArgs)e;
            //NumericalLibrary.Minimalization.MarquardtFitEventArgs me = (NumericalLibrary.Minimalization.MarquardtFitEventArgs)e;
            StringBuilder message = new StringBuilder();
            

            message.AppendFormat("[p text='c' font='symbol'][p text='2' index='sup'][p text=' = {0}']", me.Chisq);
            //message.AppendFormat("[p text='Iteration {0} (Function call count: {1})\n']", me.Iteration, me.FunctionCallCount);
            //TimeSpan span = new TimeSpan((long)((_counterStop - _counterStart) * 1e7 / _counterFreq));
            //message.AppendFormat("[p text='Elapsed time {0}\n']", span.ToString().Substring(0, span.ToString().Length - 4));
            //message += String.Format("Calculations time: {0:F2} s", (_counterStop - _counterStart) * 1.0 / _counterFreq);
            if (eventsLog.InvokeRequired) {
                this.Invoke(new SetEventTextCallBack(SetEventText), new object[] { message.ToString() });
            }
            this.Invoke(new System.Threading.ThreadStart(RefreshSearchTabPage));
        }

        private void FillDocumentsGrid() {
            documentsGrid.Rows.Clear();
            foreach (ISpectraContainer container in project.Containers) {
                DataGridViewRow row = new DataGridViewRow();
                row.HeaderCell.Value = container;
                row.CreateCells(documentsGrid, new object[] { container.Model, container.Name, container.Spectra });
                documentsGrid.Rows.Add(row);
                
            }
        }

        void project_FirstSpectraSearchCompleted(object sender, AsyncFirstSpectraSearchCompletedEventArgs args) {
            project_SearchCompleted(sender, args);
            if (!args.Cancelled && args.Error == null) {

                Semaphore sem = new Semaphore(0, 1);
                FormClosedEventHandler closedHandler = new FormClosedEventHandler(delegate(object fsender, FormClosedEventArgs fargs) {
                    sem.Release();
                });
                
                this.Invoke(new System.Threading.ThreadStart(delegate() {
                    SpectrumFitForm sff = null;
                    try
                    {
                        sff = new SpectrumFitForm(null, args.Spectra, SpectrumFitFormControls.FitAgain | SpectrumFitFormControls.Ok | SpectrumFitFormControls.SpectraSelector);
                        sff.FormClosed += closedHandler;
                        sff.ShowDialog();
                        if (args.SearchAgain = sff.DialogResult == DialogResult.Retry)
                            this.searchingSpectra = (List<ISpectrum>)args.Spectra;

                    }
                    catch
                    {
                        MessageBox.Show("Couldn't display fit window", Application.ProductName, MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
                        args.SearchAgain = false;
                    }
                    finally
                    {
                        sff.Dispose();
                    }
                }));
                sem.WaitOne();
            } else
                args.SearchAgain = false;
            this.Invoke(new System.Threading.ThreadStart(RefreshSearchTabPage));
        }

        void project_SearchProgressChanged(object sender, ProgressChangedEventArgs e) {
            Performancer.QueryPerformanceCounter(ref _counterStop);
            FitProgressChangedEventArgs me = (FitProgressChangedEventArgs)e; // (MinsqProgressChangedEventArgs)e;
            //NumericalLibrary.Minimalization.MarquardtFitEventArgs me = (NumericalLibrary.Minimalization.MarquardtFitEventArgs)e;
            StringBuilder message = new StringBuilder();
            message.AppendFormat("[p text='c' font='symbol'][p text='2' index='sup'][p text=' = {0:G10}\n']", me.Chisq);
            message.AppendFormat("[p text='Iteration {0} (Function call count: {1})\n']", me.Iteration, me.FunctionCallCount);
            TimeSpan span = new TimeSpan((long)((_counterStop - _counterStart) * 1e7 / _counterFreq));
            message.AppendFormat("[p text='Elapsed time {0}\n']", span.ToString().Substring(0, span.ToString().Length - 4));
            //message += String.Format("Calculations time: {0:F2} s", (_counterStop - _counterStart) * 1.0 / _counterFreq);
            if (eventsLog.InvokeRequired) {
                this.Invoke(new SetEventTextCallBack(SetEventText), new object[] { message.ToString() });
            }
            this.Invoke(new System.Threading.ThreadStart(delegate() {
                RefreshSearchTabPage();
                if (Program.MainWindow.ActiveMdiChild != this || Form.ActiveForm == null) {
                    Program.MainWindow.notifyIcon.BalloonTipTitle = project.Caption;
                    Program.MainWindow.notifyIcon.BalloonTipText = String.Format("Series search fit variance: {0:G6}", me.Chisq);
                    Program.MainWindow.notifyIcon.BalloonTipIcon = ToolTipIcon.Info;
                    Program.MainWindow.notifyIcon.ShowBalloonTip(1000);
                    Program.MainWindow.baloonForm = this;
                }
            }));

        }

        void project_SearchCompleted(object sender, AsyncCompletedEventArgs e) {
            Performancer.QueryPerformanceCounter(ref _counterStop);
            StringBuilder message = new StringBuilder();

            if (e.Cancelled) {
                //this.Invoke(new ThreadStart(delegate() {
                //    MessageBox.Show("Calculations aborted by user!", Application.ProductName, MessageBoxButtons.OK, MessageBoxIcon.Information);
                //}));
                message.Append("[p text='Calculations aborted.']");
                this._notSavedChanges = false;
            } else {
                this._notSavedChanges = true;
                TimeSpan span = new TimeSpan((long)((_counterStop - _counterStart) * 1e7 / _counterFreq));
                message.AppendFormat("[p text='Calculations time: {0}\n']", span.ToString().Substring(0, span.ToString().Length - 4));

                if (e.Error != null) {
                    message.AppendFormat("[p text='Some spectra may be fitted incorrectely due to error while minimalization process: {0}\n']", e.Error.Message);
                    MessageBox.Show("Control on some of the parameters have been lost. Calculations are stoped now.", Application.ProductName, MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    try {
                        TextWriter writer = new StreamWriter(Path.Combine(Application.StartupPath, "error.log"));
                        try {
                            writer.WriteLine(e.Error.Message);
                            writer.WriteLine();
                            writer.WriteLine(e.Error.StackTrace);
                        } finally {
                            writer.Close();
                        }
                    } catch {
                    }
                }

                if (fixedIndParameters.Count > 0) {
                    message.Append("[p text='\nTheoretical function was independent on following parameter(s):\n']");
                    ParameterLocation location;
                    //for (int i = 0; i < fixedIndParameters.Count; i++) {
                    int i = 1;
                    foreach (IParameter parameter in fixedIndParameters.Keys) {
                        location = project.GetParameterLocation(parameter);
                        message.AppendFormat("[p text='\t{0}. {1} -> {2} -> {3} -> ']{4}[p text='{5}\n']", i++,
                            project.Containers[location.docId].Name,
                            project.Containers[location.docId].Spectra[location.specId].Parameters[location.groupId].Definition.name,
                            project.Containers[location.docId].Spectra[location.specId].Name,
                            DefaultGroupGUI.BuildFormatedString(parameter.Definition.Header, DefaultGroupGUI.StringFormatTarget.ParameterDataGrid, (location.compId + 1).ToString()),
                            fixedIndParameters[parameter] > 0 ? String.Format(" [{0} restore(s)]", fixedIndParameters[parameter]) : "");
                        parameter.Status = (parameter.Status & ~ParameterStatus.Fixed) | ParameterStatus.Free;
                    }
                    message.Append("[p text='Some of the parameters could have been fixed. After calculations all parameters was released.']");
                }

                if (sender != null)
                    message.AppendFormat("[p text='Calculations finished. Global  '][p text='c' font='symbol'][p text='2' index='sup'][p text=' = {0:G05}\n']", project.Fit);
                else {
                    message.Append("[p text='First spectra fitted with following values of '][p text='c' font='symbol'][p text='2\n' index='sup']");
                    for (int i = 0; i < searchingSpectra.Count; i++)
                        message.AppendFormat("[p text='-   {0}\t\t'][p text='c' font='symbol'][p text='2' index='sup'][p text=' = {1:G05}\n']", searchingSpectra[i].Name, searchingSpectra[i].Fit);
                }

            }

            if (eventsLog.InvokeRequired) {
                this.Invoke(new SetEventTextCallBack(SetEventText), new object[] { message.ToString() });
                if (sender != null) //if series search completed
                    this.Invoke(new System.Threading.ThreadStart(delegate {
                        SpecRadioButton.Checked = true;
                        radioButton_CheckedChanged(SpecRadioButton, new EventArgs());
                    }));
            }

            //this.Invoke(new System.Threading.ThreadStart());
            //this.Invoke(new SetReadonlyCallBack(SetReadonly), new object[] { false });
            
            
            this.Invoke(new System.Threading.ThreadStart(delegate {
                RefreshSearchTabPage();
                //Program.MainWindow.notifyIcon.Visible = true;
                if (!e.Cancelled && (Program.MainWindow.ActiveMdiChild != this || Form.ActiveForm == null)) {
                    Program.MainWindow.notifyIcon.BalloonTipTitle = project.Caption;
                    if (e.Error != null) {
                        Program.MainWindow.notifyIcon.BalloonTipText = String.Format("Control on calculations lost in {0}", project.Caption);
                        Program.MainWindow.notifyIcon.BalloonTipIcon = ToolTipIcon.Error;
                    } else {
                        Program.MainWindow.notifyIcon.BalloonTipText = String.Format("Calculations finished.\nFit variance: {0:G6}", project.Fit);
                        Program.MainWindow.notifyIcon.BalloonTipIcon = ToolTipIcon.Info;
                    }
                    Program.MainWindow.notifyIcon.ShowBalloonTip(10000);
                    Program.MainWindow.baloonForm = this;
                }
                tspbSearchProgress.Visible = false;
                Program.MainWindow.waitForm.RemoveProject(project);
            }));
            //this.searchingSpectra = null;
            refreshGrids();
        }

        private void RefreshSearchTabPage() {
            commonValuesGrid.Invalidate();
            if (chartVisibilityCB.Checked)
                RedrawChart();
        }

        private void SetEventText(string text) {
            DateTime time = DateTime.Now;
            int paragraphStart = eventsLog.Text.Length;
            string date = String.Format("[{0}]\n", time.ToString("T"));
            eventsLog.AppendText(date);
            eventsLog.Select(paragraphStart, date.Length);
            eventsLog.SelectionFont = new Font(eventsLog.SelectionFont, FontStyle.Bold);

            eventsLog.Select(eventsLog.Text.Length - 1, 1);
            eventsLog.SelectionIndent = 0;
            
            paragraphStart = eventsLog.Text.Length - 1;
            DefaultGroupGUI.writeFormattedText(text, eventsLog);
            //int newTextSize;
            //foreach (System.Text.RegularExpressions.Match pMatch in DefaultGroupGUI.parameterHeaderRegex.Matches(text)) {
            //    foreach (System.Text.RegularExpressions.Match aMatch in DefaultGroupGUI.attributeRegex.Matches(pMatch.Value)) {
            //        switch (aMatch.Groups["name"].Value) {
            //            case "text":
            //                newTextSize = aMatch.Groups["value"].Value.Length;
            //                eventsLog.AppendText(aMatch.Groups["value"].Value);
            //                eventsLog.Select(eventsLog.Text.Length - newTextSize, newTextSize);
            //                break;
            //            case "font":
            //                eventsLog.SelectionFont = new Font(aMatch.Groups["value"].Value, eventsLog.Font.Size, eventsLog.Font.Style);
            //                break;
            //            case "index":
            //                eventsLog.SelectionCharOffset = (aMatch.Groups["value"].Value == "sup") ? 5 : -3;
            //                break;
            //            case "style":
            //                switch (aMatch.Groups["value"].Value) {
            //                    case "underline": eventsLog.SelectionFont = new Font(eventsLog.SelectionFont, eventsLog.SelectionFont.Style | FontStyle.Underline); break;
            //                    case "bold": eventsLog.SelectionFont = new Font(eventsLog.SelectionFont, eventsLog.SelectionFont.Style | FontStyle.Bold); break;
            //                    case "italic": eventsLog.SelectionFont = new Font(eventsLog.SelectionFont, eventsLog.SelectionFont.Style | FontStyle.Italic); break;
            //                }
            //                break;
            //        }
            //    }
            //    eventsLog.Select(eventsLog.Text.Length, 0);
            //    eventsLog.SelectionFont = eventsLog.Font;
            //    eventsLog.SelectionCharOffset = 0;
            //}
            //string[] parts = text.Split(new string[] { "][" }, StringSplitOptions.RemoveEmptyEntries);
            //foreach (string part in parts) {
            //    if (part.IndexOf("@:") == 0) {
            //        string s = part.Substring(2);
            //        eventsLog.AppendText(s);
            //        eventsLog.Select(eventsLog.Text.Length - s.Length, s.Length);
            //        eventsLog.SelectionFont = new Font("Symbol", eventsLog.Font.Size, eventsLog.Font.Style);
            //        eventsLog.SelectionCharOffset = 0;
            //        eventsLog.SelectionBullet = false;
            //    } else {
            //        if (part.IndexOf("sup:") == 0) {
            //            string s = part.Substring(4);
            //            eventsLog.AppendText(s);
            //            eventsLog.Select(eventsLog.Text.Length - s.Length, s.Length);
            //            eventsLog.SelectionFont = eventsLog.Font;
            //            eventsLog.SelectionCharOffset = 3;
            //            eventsLog.SelectionBullet = false;
            //        } else {
            //            if (part.IndexOf("bul:") == 0) {
            //                string s = part.Substring(4);
            //                eventsLog.AppendText(s);
            //                eventsLog.Select(eventsLog.Text.Length - s.Length, s.Length);
            //                eventsLog.SelectionFont = eventsLog.Font;
            //                eventsLog.SelectionCharOffset = 0;
            //                eventsLog.SelectionBullet = true;
            //            } else {
            //                string s = part.Substring(0);
            //                eventsLog.AppendText(s);
            //                eventsLog.Select(eventsLog.Text.Length - s.Length, s.Length);
            //                eventsLog.SelectionFont = eventsLog.Font;
            //                eventsLog.SelectionCharOffset = 0;
            //                eventsLog.SelectionBullet = false;
            //            }
            //        }
            //    }
            //}
            eventsLog.Select(paragraphStart+1, eventsLog.Text.Length - paragraphStart);
            eventsLog.SelectionHangingIndent = 0;
            eventsLog.SelectionIndent = 20;
            eventsLog.AppendText("\n");
            eventsLog.ScrollToCaret();
        }

        private void CheckRadioButton(RadioButton radioButton) {
            radioButton.Checked = true;
        }

        //public static Color GetColor(ParameterStatus status) {
        //    switch (status) {
        //        case ParameterStatus.Local | ParameterStatus.Free: return Color.YellowGreen;
        //        case ParameterStatus.Local | ParameterStatus.Fixed: return Color.IndianRed;
        //        case ParameterStatus.Common | ParameterStatus.Free: return Color.MediumSeaGreen;
        //        case ParameterStatus.Common | ParameterStatus.Fixed: return Color.Red;
        //        default: return Color.Wheat;
        //    }
        //}

        private void refreshGrids() {
            foreach (ISpectraContainer container in project.Containers) {
                if (!container.Enabled) continue;
                int i, j;
                for (i = 0; i < documentTabs.Count; i++)
                    for (j = 0; j < documentTabs[i]._groupsControl.TabCount; j++) {
                        if (documentTabs[i]._groupsControl.TabPages[j] is GroupTabPage) {
                            ((GroupTabPage)documentTabs[i]._groupsControl.TabPages[j]).grid.Invalidate();
                            ((GroupTabPage)documentTabs[i]._groupsControl.TabPages[j]).grid.SaveUndoStep();
                        }

                    }
                for (i=0; i<projectTabControl.TabCount; i++)
                    if (projectTabControl.TabPages[i] is GroupTabPage) {
                        ((GroupTabPage)projectTabControl.TabPages[i]).grid.Invalidate();
                        ((GroupTabPage)projectTabControl.TabPages[i]).grid.SaveUndoStep();
                    }
            }
        }

        private void SaveIfNecessary() {
            if (NotSavedChanges) {
                switch (MainForm.savebeforefitting) {
                    case 1://if ask
                        SaveBeforeFitDialog dialog = new SaveBeforeFitDialog();
                        if (dialog.ShowDialog() == DialogResult.Yes)
                            project.Save(project.Path);
                        break;
                    case 2: //save without asking
                        project.Save(project.Path);
                        break;
                }
            }
        }

        public void FitSingleSpectrum(List<ISpectrum> spectra) {
            if (project.IsBusy)
                MessageBox.Show("Project is already searching!", "", MessageBoxButtons.OK, MessageBoxIcon.Information);
            else {
                //if (this.searchingSpectra == null)
                this.searchingSpectra = spectra;
                AddUndoStep(new SearchChangeStep(this, String.Format("{0} fit results", searchingSpectra[0].Name), searchingSpectra));
                fixedIndParameters.Clear();
                tspbSearchProgress.Visible = true;
                SaveIfNecessary();
                if (MainForm.switchToSearch)
                    projectTabControl.SelectedIndex = 1;
                eventsLog.Clear();
                SetEventText("[p text='Fitting first spectra in each document...\n']");
                _counterStart = 0;
                _counterStop = 0;
                Performancer.QueryPerformanceCounter(ref _counterStart);
                //ISpectrum[] firstSpectra = new ISpectrum[project.Containers.Count];               
                project.FirstSpectraSearchAsync(spectra);
            } 
        }

        private void SetSearchableSpectraAndCommonParameters(bool series) {
            //List<ISpectrum> spectra = null;
            //searchingSpectra.Clear();
            //this._searchingUndone = false;
            this.searchingSpectra = null;
            //commonParameters.Clear();
            commonValuesGrid.Rows.Clear();
            //bool oneContainerOnly;// = true;
            //if (project.Containers.Count > 0) {
                if (this.projectTabControl.SelectedTab is GroupTabPage) {
                    searchingSpectra = ((GroupTabPage)this.projectTabControl.SelectedTab).Spectra;
                    //oneContainerOnly = false;
                } else {
                    if (this.projectTabControl.SelectedTab is SpectraContainerTabPage) {
                        SpectraContainerTabPage page = (SpectraContainerTabPage)this.projectTabControl.SelectedTab;
                        if (page.GroupsControl.SelectedTab is GroupTabPage)
                            searchingSpectra = ((GroupTabPage)page.GroupsControl.SelectedTab).Spectra;
                    }
                }
            //} else
                //spectra = project.Containers[0].Spectra;
            //    searchingSpectra = null;
            //if (series || searchingSpectra == null)
            //    return searchingSpectra;
            //else {
            if (searchingSpectra != null) {
                if (series) {
                    //setCommonParametersGrid();
                    setParametersPreview();
                    yAxes.SelectedIndex = 0;
                    ThreadStart redraw = delegate() {
                        Thread.Sleep(2000);
                        this.BeginInvoke(new ThreadStart(RedrawChart));
                    };
                    //this.BeginInvoke(new ThreadStart(redraw));
                    redraw.BeginInvoke(null, null);
                    //set axes in search tab

                } else {
                    List<ISpectrum> fspectrum = new List<ISpectrum>();
                    fspectrum.Add(searchingSpectra[0]);
                    searchingSpectra = fspectrum;
                }
            }
        }

        private YAxisComboBoxColumnItem getAxisColumnItem(DataGridParameterView grid, DataGridViewColumn column, string groupName, string documentName) {
            //if (column.HeaderText.ToLower().Contains("int") ||
            //    column.HeaderText.ToLower().Contains("contrib"))
            //    return new YAxisComboBoxColumnItem(
            //        grid, column.Index, groupName, documentName, 0.0f, 100.0f);
            //else if (column.HeaderText.ToLower().Contains("shift"))
                return new YAxisComboBoxColumnItem(
                    grid, column.Index, groupName, documentName);
            //else
            //     return new YAxisComboBoxColumnItem(
            //        grid, column.Index, groupName, documentName, 0.0f, float.PositiveInfinity);
        }

        private void setParametersPreview() {
            List<ISpectraContainer> countingContainers;
            int i, p = 0, cp, c;
            yAxes.Items.Clear();
            //DataGridParameterView grid = null;
            DataGridViewRow commonValueRow;
            DataGridViewTextBoxCell textCell;
            DataGridViewParameterCell parameterCell;
            GroupTabPage tabPage = null;
            string documentName = null;
            string groupName = null;
            if (searchingSpectra == null)
                countingContainers = project.Containers;
            else {
                countingContainers = new List<ISpectraContainer>();
                for (i = 0; i < searchingSpectra.Count; i++)
                    if (!countingContainers.Contains(searchingSpectra[i].Container))
                        countingContainers.Add(searchingSpectra[i].Container);
            }

            ThreadStart addColumns = delegate() {
                //if one spectrum in grid there is no reason to draw chart with one point
                if (tabPage.grid.Rows.Count > 2) {
                    //skip first two columns : spectrum name and key value
                    int tmpc = 0;
                    int referenceSum = 0;
                    ParameterStatus statusSum = 0;
                    for (c = 2; c < tabPage.grid.Columns.Count; c++) {
                        statusSum = 0;
                        referenceSum = 0;
                        if (tabPage.grid[c, 1] is DataGridViewParameterCell) {
                            if (tabPage.grid.Columns[c].Visible && tmpc > 0 &&
                                ((((DataGridViewParameterCell)tabPage.grid[c, 1]).Parameter.Definition.BindedStatus == ParameterStatus.None) ||
                                (((DataGridViewParameterCell)tabPage.grid[c, 1]).Parameter.Definition.Properties & ParameterProperties.Readonly) > 0)) {
                                //sumowanie wszystkich statusow i grup w kolumnie
                                for (int r = 1; r < tabPage.grid.RowCount; r++) {
                                    if (tabPage.grid[c, r] is DataGridViewParameterCell) {
                                        referenceSum += ((DataGridViewParameterCell)tabPage.grid[c, r]).Parameter.ReferenceGroup;
                                        statusSum |= ((DataGridViewParameterCell)tabPage.grid[c, r]).Parameter.Status;
                                    }
                                }
                                //jesli nie bylo podzialow, grup w ramach parametru i suma statusow ma w sobie common - do tabeli
                                //w przeciwnym wypadku byly podzialy na grupy
                                if (referenceSum == 0 && (statusSum & ParameterStatus.Common) > 0) {
                                    commonValueRow = new DataGridViewRow();
                                    textCell = new DataGridViewTextBoxCell();
                                    textCell.Value = DefaultGroupGUI.BuildFormatedString(
                                        tabPage.grid.Columns[c].HeaderText,
                                        DefaultGroupGUI.StringFormatTarget.ParameterDataGrid,
                                        groupName,
                                        documentName);
                                    textCell.ToolTipText = tabPage.grid.Columns[c].ToolTipText;
                                    parameterCell = (DataGridViewParameterCell)tabPage.grid[c, 1].Clone(); // tabPage.groupGUI.CreateParameterCell(((DataGridViewParameterCell)tabPage.grid[c, 1]).Parameter);
                                    commonValueRow.Cells.Add(textCell);
                                    commonValueRow.Cells.Add(parameterCell);
                                    commonValuesGrid.Rows.Add(commonValueRow);
                                } else {
                                    yAxes.Items.Add(getAxisColumnItem(tabPage.grid, tabPage.grid.Columns[c], groupName, documentName));
                                }
                                
                            }
                            tmpc++;
                        }
                    }
                }
            };

            if (searchingSpectra != null)
                yAxes.Items.Add(new YAxisComboBoxFitVarianceItem(searchingSpectra));
            for (p=1; p<projectTabControl.TabCount; p++) {
                if (projectTabControl.TabPages[p] is SpectraContainerTabPage) {
                    if (countingContainers.Contains(((SpectraContainerTabPage)projectTabControl.TabPages[p]).SpectraContainer)) {
                        for (cp=0; cp<((SpectraContainerTabPage)projectTabControl.TabPages[p]).GroupsControl.TabCount; cp++) {
                            if (((SpectraContainerTabPage)projectTabControl.TabPages[p]).GroupsControl.TabPages[cp] is GroupTabPage) {
                                //((GroupTabPage)((SpectraContainerTabPage)projectTabControl.TabPages[p])).spectra
                                //grid = ((GroupTabPage)(((SpectraContainerTabPage)projectTabControl.TabPages[p]).GroupsControl.TabPages[cp])).grid;
                                tabPage = (GroupTabPage)((SpectraContainerTabPage)projectTabControl.TabPages[p]).GroupsControl.TabPages[cp];
                                documentName = ((SpectraContainerTabPage)projectTabControl.TabPages[p]).SpectraContainer.Name;
                                groupName = ((GroupTabPage)(((SpectraContainerTabPage)projectTabControl.TabPages[p]).GroupsControl.TabPages[cp])).Text;
                                addColumns();
                            }
                        }
                    }
                } else if (projectTabControl.TabPages[p] is SharedGroupTabPage) {
                    tabPage = (SharedGroupTabPage)projectTabControl.TabPages[p];
                    groupName = ((SharedGroupTabPage)projectTabControl.TabPages[p]).Text;
                    documentName = null;
                    addColumns();
                }
            }
        }

        private void toolStripButton1_Click(object sender, EventArgs e) {
            //List<ISpectrum> spectrum = GetSearchableSpectra(false);
            SetSearchableSpectraAndCommonParameters(false);
            refreshGrids();
            if (searchingSpectra != null) {
                FitSingleSpectrum(searchingSpectra);
            } else
                MessageBox.Show("Choose spectra set for analysis by selecting one of the tabs holding spectra list.", Application.ProductName, MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        private void toolStripButton2_Click(object sender, EventArgs e) {
            if (project.IsBusy)
                MessageBox.Show("Project is already searching!", "", MessageBoxButtons.OK, MessageBoxIcon.Information);
            else {
                SetSearchableSpectraAndCommonParameters(true);

                if (searchingSpectra == null) {
                    MessageBox.Show("Choose spectra set for analysis by selecting one of the tabs holding spectra list.", Application.ProductName, MessageBoxButtons.OK, MessageBoxIcon.Information);
                    return;
                }
                AddUndoStep(new SearchChangeStep(this, "Series fit results", searchingSpectra));
                refreshGrids();
                tspbSearchProgress.Visible = true;
                SaveIfNecessary();
                fixedIndParameters.Clear();
                
                if (MainForm.switchToSearch)
                    projectTabControl.SelectedIndex = 1;
                eventsLog.Clear();
                SetEventText("[p text='Calculations started for series of spectra...\n']");
                _counterStart = 0;
                _counterStop = 0;
                Performancer.QueryPerformanceCounter(ref _counterStart);
                
                //List<ISpectraContainer> containers = new List<ISpectraContainer>();
                //foreach (ISpectraContainer container in project.Containers)
                //    if (container.Enabled)
                //        containers.Add(container);
                project.SeriesSearchAsync(null, searchingSpectra);
            }
        }

        public void CancelSearching() {
            toolStripButton3_Click(this, null);
        }

        private void toolStripButton3_Click(object sender, EventArgs e) {
            if (project.IsBusy) {
                project.SearchAsyncCancel();
                Program.MainWindow.waitForm.AddProject(project);
            }
        }

        private void radioButton_CheckedChanged(object sender, EventArgs e) {
            if (((RadioButton)sender).Checked) {
                project.CalculatedValues = sender == PrevCalcRadioButton;
            }
            this._notSavedChanges = true;
        }

        private void ProjectForm_Activated(object sender, EventArgs e) {
            //put new toolstrip at the end
            int position = 0;
            for (int i = 0; i < Program.MainWindow.toolStripPanel1.Controls.Count; i++) {
                Control control = Program.MainWindow.toolStripPanel1.Controls[i];
                if (i == 0)
                    position += control.Width + control.Margin.Left + control.Margin.Right;
                else
                    control.Visible = false;
            }
            Program.MainWindow.toolStripPanel1.Join(this.toolStrip1, position + this.toolStrip1.Margin.Left + 3, 0);
            this.toolStrip1.Visible = true;
        }

        private void ProjectForm_Deactivate(object sender, EventArgs e) {
            //this.toolStripPanel1.Join(this.toolStrip1);
            this.toolStrip1.Visible = false;
        }

        private void copyToolStripMenuItem_Click(object sender, EventArgs e) {
            GroupTabPage activeTabPage = getOpenedGroupTabPage();
            if (activeTabPage != null)
                if (activeTabPage.grid.Focused)
                    activeTabPage.copyToClipboard(false);
        }

        private void copyWithHeadersToolStripMenuItem_Click(object sender, EventArgs e) {
            GroupTabPage activeTabPage = getOpenedGroupTabPage();
            if (activeTabPage != null)
                activeTabPage.copyToClipboard(true);
        }

        private void pasteToolStripMenuItem_Click(object sender, EventArgs e) {
            GroupTabPage activeTabPage = getOpenedGroupTabPage();
            if (activeTabPage != null) {
                activeTabPage.pasteFromClipboard();
                activeTabPage.grid.SaveUndoStep();
            }
        }

        private void undoMenuItem_Click(object sender, EventArgs e) {
            //MessageBox.Show("This feature will be available soon", Application.ProductName, MessageBoxButtons.OK, MessageBoxIcon.Information);
            //if (!this._searchingUndone && searchingSpectra != null) {
            //    for (int i = 0; i < searchingSpectra.Count; i++)
            //        project.RestoreSpectrumStartingValues(searchingSpectra[i], ParameterStatus.Free);
            //    this._searchingUndone = true;
            //    refreshGrids();
            //} else {
            //    GroupTabPage activeTabPage = getOpenedGroupTabPage();
            //    if (activeTabPage != null)
            //        activeTabPage.grid.Undo();
            //}
            
            ChangeStep step;
            if (undoSteps.Count > 0) {
                step = undoSteps.First.Value;
                undoSteps.RemoveFirst();
                step.Commit();
                redoSteps.AddFirst(step);
                //go to group tab where change have been made/restored if any
                if (step.holder != null) {
                    if (!(step.holder is SharedGroupTabPage)) {
                        for (int i = 0; i < projectTabControl.TabCount; i++) {
                            if (projectTabControl.TabPages[i] is SpectraContainerTabPage)
                                if (((SpectraContainerTabPage)projectTabControl.TabPages[i]).SpectraContainer == step.holder.Spectra[0].Container) {
                                    projectTabControl.SelectedIndex = i;
                                    break;
                                }
                        }
                    }
                    ((TabControl)step.holder.Parent).SelectedTab = step.holder;
                }
            }
            RefreshUndoRedoTexts();
        }

        private void redoMenuItem_Click(object sender, EventArgs e) {
            //MessageBox.Show("This feature will be available soon", Application.ProductName, MessageBoxButtons.OK, MessageBoxIcon.Information);
            //GroupTabPage activeTabPage = getOpenedGroupTabPage();
            //if (activeTabPage != null)
            //    activeTabPage.grid.Redo();
            ChangeStep step;
            if (redoSteps.Count > 0) {
                step = redoSteps.First.Value;
                redoSteps.RemoveFirst();
                step.Commit();
                undoSteps.AddFirst(step);
            }
            RefreshUndoRedoTexts();
        }

        private void RefreshUndoRedoTexts() {
            if (toolStripButton7.Enabled = undoMenuItem.Enabled = undoSteps.Count > 0)
                toolStripButton7.Text = undoMenuItem.Text = String.Format("Undo \"{0}\"", undoSteps.First.Value.Name);
            else
                toolStripButton7.Text = undoMenuItem.Text = "Undo";

            if (toolStripButton8.Enabled = redoMenuItem.Enabled = redoSteps.Count > 0)
                toolStripButton8.Text = redoMenuItem.Text = String.Format("Redo \"{0}\"", redoSteps.First.Value.Name);
            else
                toolStripButton8.Text = redoMenuItem.Text = "Redo";
        }

        private void editToolStripMenuItem_DropDownOpening(object sender, EventArgs e) {
            //GroupTabPage activeTabPage = getOpenedGroupTabPage();
            //bool allowUndo = false;
            //bool allowRedo = false;
            //if (activeTabPage != null) {
            //    allowRedo = activeTabPage.grid.CanRedo;
            //    allowUndo = activeTabPage.grid.CanUndo;
            //}
            //undoMenuItem.Enabled = allowUndo;
            //redoMenuItem.Enabled = allowRedo;
        }

        private GroupTabPage getOpenedGroupTabPage() {
            if (projectTabControl.SelectedTab is SharedGroupTabPage)
                return (SharedGroupTabPage)projectTabControl.SelectedTab;
            else if (projectTabControl.SelectedTab is SpectraContainerTabPage)
                if (((SpectraContainerTabPage)projectTabControl.SelectedTab)._groupsControl.SelectedTab is GroupTabPage)
                    return (GroupTabPage)((SpectraContainerTabPage)projectTabControl.SelectedTab)._groupsControl.SelectedTab;
            return null;
            //if (projectTabControl.SelectedTab.Controls.Find("groupsControl", false).Length == 0) return null;
            //TabControl groupsControl = (TabControl)projectTabControl.SelectedTab.Controls.Find("groupsControl", false)[0]; // is GroupTabPage)
            //if (groupsControl != null)
            //    if (groupsControl.SelectedTab is GroupTabPage)
            //        return ((GroupTabPage)groupsControl.SelectedTab);
            //return null;
        }

        private void projectTabControl_SelectedIndexChanged(object sender, EventArgs e) {
            //commonValuesGrid.Font = new Font("Tahoma", 9.2f);
            editToolStripMenuItem_DropDownOpening(sender, e);
            if (projectTabControl.SelectedIndex == 1) {
                ProjectForm_Resize(this, new EventArgs());
                RedrawChart();
                //commonValuesGrid.Rows.Clear();
                //int xSelection = xAxes.SelectedIndex;
                //int ySelection = yAxes.SelectedIndex;
                //yAxes.Items.Clear();
                //xAxes.Items.Clear();

                //foreach (TabPage documentPage in projectTabControl.TabPages)
                //    foreach (Control groupControl in documentPage.Controls.Find("groupsControl", true)) {

                //        foreach (TabPage page in ((TabControl)groupControl).TabPages)
                //            foreach (Control grid in page.Controls.Find("grid", true)) {
                //                DataGridParameterView dgpv = (DataGridParameterView)grid;
                //                foreach (DataGridViewCell cell in dgpv.Rows[1].Cells)
                //                    if (cell is DataGridViewParameterCell && cell.Visible) {
                //                        IParameter parameter = ((DataGridViewParameterCell)cell).Parameter;
                //                        if ((parameter.Status & ParameterStatus.Local) == ParameterStatus.Local) {
                //                            yAxes.Items.Add(new YAxisComboBoxItem(
                //                                cell.OwningColumn,
                //                                page.Text,
                //                                (project.Containers.Count > 1 ? documentPage.Text : "")));
                //                        } else if ((parameter.Status & ParameterStatus.Common) == ParameterStatus.Common) {
                //                            DataGridViewRow row = new DataGridViewRow();
                //                            DataGridViewTextBoxCell cell0 = new DataGridViewTextBoxCell();


                //                            //cell0.Value = String.Format(" \0{0} {1} \0sub:{2}\0",
                //                            //    page.Text,
                //                            //    cell.OwningColumn.HeaderCell.Value,
                //                            //    (project.Containers.Count > 1 ? documentPage.Text : ""));
                //                            if (project.Containers.Count > 1)
                //                                cell0.Value = DefaultGroupGUI.BuildFormatedString(cell.OwningColumn.HeaderCell.Value.ToString(), DefaultGroupGUI.StringFormatTarget.ParameterDataGrid, documentPage.Text);
                //                            else
                //                                cell0.Value = cell.OwningColumn.HeaderCell.Value.ToString();
                //                            cell0.Style.BackColor = SystemColors.Control;
                                            
                //                            //cell1.ConvertFromUserValue += new UserValueConversionHandler(((DataGridViewParameterCell)cell). .ConvertFromUserValue);
                //                            DataGridViewParameterCell cell1 = new DataGridViewParameterCell(parameter);
                //                            cell1.Style.ForeColor = SystemColors.ControlText;
                //                            row.Cells.Add(cell0);
                //                            row.Cells.Add(cell1);
                //                            row.HeaderCell.Value = cell;
                //                            commonValuesGrid.Rows.Add(row);
                //                        }
                //                    }
                //            }
                //    }
                //if (ySelection < yAxes.Items.Count)
                //    yAxes.SelectedIndex = ySelection;
                //if (xSelection < xAxes.Items.Count)
                //    xAxes.SelectedIndex = xSelection;
            }
        }

        private void ProjectForm_FormClosing(object sender, FormClosingEventArgs e) {
            if (!this.IsHandleCreated) return;
            if (e.Cancel = project.IsBusy) {
                project.SearchAsyncCancel();
                Program.MainWindow.waitForm.AddProject(project);
                //if (!Program.MainWindow.Exiting)
                if (e.CloseReason == CloseReason.UserClosing)
                    this.Exiting = true;
            } else {
                if (NotSavedChanges)
                    switch (MessageBox.Show(String.Format("Save changes in project '{0}'?", project.Caption), Application.ProductName, MessageBoxButtons.YesNoCancel, MessageBoxIcon.Question)) {
                        case DialogResult.Yes:
                            if (Program.MainWindow.SaveProject(this, false) == DialogResult.Cancel)
                                e.Cancel = true;
                            //project.Save(project.Path, false);
                            break;
                        case DialogResult.No:
                            break;
                        case DialogResult.Cancel:
                            e.Cancel = true;
                            this.Exiting = false;
                            break;
                    }
                MainForm.removeProjectFromRecoveryFile(project.ProjectFile);
            }
        }

        public bool Exiting {
            get { return this._exiting; }
            set {
                if (!this._exiting && value)
                    project.SearchCompleted += project_CloseAfterCompleteSearch;
                else if (!value)
                    project.SearchCompleted -= project_CloseAfterCompleteSearch;

                Program.MainWindow.Exiting = false;
            }
        }

        private void project_CloseAfterCompleteSearch(object sender, AsyncCompletedEventArgs e) {
            if (this.IsHandleCreated) {
                this.Invoke(new ThreadStart(delegate() {
                    Close();
                }));
            }
        }

        private void ProjectForm_Resize(object sender, EventArgs e) {
            foreach (DataGridViewColumn column in commonValuesGrid.Columns)
                column.Width = commonValuesGrid.ClientRectangle.Width / commonValuesGrid.ColumnCount - 1;
        }

        private void commonValuesGrid_CellFormatting(object sender, DataGridViewCellFormattingEventArgs e) {
            if (commonValuesGrid[e.ColumnIndex, e.RowIndex] is DataGridViewParameterCell) {
                
                //    e.Value = ((DataGridViewParameterCell)commonValuesGrid[e.ColumnIndex, e.RowIndex]).Value.ToString();
                //    DataGridViewParameterCell cell = (DataGridViewParameterCell)commonValuesGrid.Rows[e.RowIndex].HeaderCell.Value;
                //    //e.Value = cell.UserValue.ToString("G06");
                //    e.Value = ((double)cell.Value).ToString("G06");
            }
        }

        private void checkBox1_CheckedChanged(object sender, EventArgs e) {
            axesPanel.Visible = chartVisibilityCB.Checked;
            chart1.Visible = chartVisibilityCB.Checked;
            RedrawChart();
        }

        private void axisComboFormat(object sender, ListControlConvertEventArgs e) {
            if (e.ListItem is YAxisComboBoxItem) {
                YAxisComboBoxItem item = (YAxisComboBoxItem)e.ListItem;
                //e.Value = item.
                //e.Value = DefaultGroupGUI.BuildFormatedString(
                //    item.grid.Columns[item.column.HeaderCell.Value.ToString(),
                //    DefaultGroupGUI.StringFormatTarget.ParameterDataGrid,
                //    item.groupName,
                //    item.documentName);
            } else
                if (e.ListItem is XAxisComboBoxItem) {
                    e.Value = DefaultGroupGUI.BuildFormatedString(
                        ((XAxisComboBoxItem)e.ListItem).parameterName,
                        DefaultGroupGUI.StringFormatTarget.ParameterDataGrid);
                }
        }

        private void yAxes_DrawItem(object sender, DrawItemEventArgs e) {
            //e.Graphics.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.HighQuality;
            if (e.Index == -1) return;
            e.DrawBackground();
            ComboBox comboBox = (ComboBox)sender;
            e.Graphics.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.HighQuality;
            if ((e.State & DrawItemState.Selected) == DrawItemState.Selected)
                DefaultGroupGUI.DrawHeaderContent(
                    ((YAxisComboBoxItem)comboBox.Items[e.Index]).FormattedCaption,
                    //comboBox.GetItemText(comboBox.Items[e.Index]),
                    e.Graphics,
                    e.Font,
                    e.Bounds,
                    false, SystemBrushes.HighlightText);
            else
                DefaultGroupGUI.DrawHeaderContent(
                    ((YAxisComboBoxItem)comboBox.Items[e.Index]).FormattedCaption,
                    //comboBox.GetItemText(comboBox.Items[e.Index]),
                    e.Graphics,
                    e.Font,
                    e.Bounds,
                    false, SystemBrushes.WindowText);
        }

        abstract class YAxisComboBoxItem {
            //public DataGridParameterView grid;
            public float min, max; //chart extrema
            public string groupName;
            public string documentName;
            public abstract float GetValue(int x);
            public abstract string GetPointLabel(int x);
            public abstract string FormattedCaption { get; }
            public YAxisComboBoxItem(string groupName, string documentName) {
                this.groupName = groupName;
                this.documentName = documentName;
                this.min = float.NegativeInfinity;
                this.max = float.PositiveInfinity;
            }

            public YAxisComboBoxItem(string groupName, string documentName, float min, float max) {
                this.groupName = groupName;
                this.documentName = documentName;
                this.min = min;
                this.max = max;
            }

            public abstract int DataCount { get; }
            
        }

        class YAxisComboBoxColumnItem : YAxisComboBoxItem {
            //public delegate float GetValueHandler(int x);
            //public GetArrayHandler GetValue;
            //public DataGridViewColumn gridColumn;
            public int columnId;
            public DataGridParameterView grid;

            public YAxisComboBoxColumnItem(DataGridParameterView grid, int columnId, string groupName, string documentName, float min, float max)
                : base(groupName, documentName, min, max) {
                this.grid = grid;
                this.columnId = columnId;
            }

            public YAxisComboBoxColumnItem(DataGridParameterView grid, int columnId, string groupName, string documentName)
                : base(groupName, documentName) {
                this.grid = grid;
                this.columnId = columnId;
            }

            public override float GetValue(int x) {
                float v;
                try {
                    v = float.Parse(grid[columnId, x].Value.ToString());
                } catch (Exception) {
                    v = 0;
                }
                return v;
            }
            public override string FormattedCaption {
                get {
                    return DefaultGroupGUI.BuildFormatedString(
                        grid.Columns[columnId].HeaderText,
                        DefaultGroupGUI.StringFormatTarget.ParameterDataGrid,
                        documentName,
                        groupName);
                }
            }
            public override int DataCount {
                get { return grid.RowCount; }
            }
            public override string GetPointLabel(int x) {
                return ((DataGridViewSpectrumRow)grid.Rows[x]).Spectrum.Name;
            }
        }

        class YAxisComboBoxFitVarianceItem : YAxisComboBoxItem {
            public List<ISpectrum> spectra;
            public YAxisComboBoxFitVarianceItem(List<ISpectrum> spectra)
                : base(null, null) {
                this.spectra = spectra;
                this.min = 0.0f;
            }
            public override float GetValue(int x) {
                return (float)spectra[x-1].Fit;
            }
            public override string FormattedCaption {
                get {
                    return DefaultGroupGUI.BuildFormatedString(
                        "[p text='fit variance']",
                        DefaultGroupGUI.StringFormatTarget.ParameterDataGrid);
                }
            }
            public override int DataCount {
                get { return spectra.Count + 1; }
            }
            public override string GetPointLabel(int x) {
                return spectra[x-1].Name;
            }
        }

        class XAxisComboBoxItem {
            public string groupName;
            public int componentId;
            public string parameterName;
            public XAxisComboBoxItem(string groupName, int componentId, string parameterName) {
                this.groupName = groupName;
                this.componentId = componentId;
                this.parameterName = parameterName;
            }
        }

        private void yAxes_SelectedIndexChanged(object sender, EventArgs e) {
            //int selectedIndex = xAxes.SelectedIndex;
            //xAxes.Items.Clear();
            //xAxes.Items.Add(new XAxisComboBoxItem("notpresentgroup", -1, "spectrum id"));
            //ISpectrum spectrum = (ISpectrum)((YAxisComboBoxItem)yAxes.SelectedItem).gridColumn.DataGridView.Rows[1].HeaderCell.Value;
            //foreach (IGroup group in spectrum.Parameters)
            //    if ((group.Definition.Type & GroupType.SpectrumConstants) == GroupType.SpectrumConstants) {
            //        foreach (Evel.interfaces.IComponent component in group.Components)
            //            foreach (IParameter parameter in component) {
            //                if ((parameter.Definition.Properties & ParameterProperties.KeyValue) == ParameterProperties.KeyValue)
            //                    xAxes.Items.Add(new XAxisComboBoxItem(
            //                        group.Definition.name,
            //                        group.Components.IndexOf(component),
            //                        parameter.Definition.Name));
            //            }
            //    }
            //if (selectedIndex < xAxes.Items.Count)
            //    xAxes.SelectedIndex = selectedIndex;
            RedrawChart();
        }

        private void xAxes_SelectedIndexChanged(object sender, EventArgs e) {
            RedrawChart();
        }

        private void RedrawChart() {
            if (yAxes.SelectedIndex == -1) return;
            YAxisComboBoxItem yitem = (YAxisComboBoxItem)yAxes.SelectedItem;
            //DataGridParameterView grid = (DataGridParameterView)((YAxisComboBoxItem)yAxes.SelectedItem).gridColumn.DataGridView;
            //int columnIndex = ((YAxisComboBoxItem)yAxes.SelectedItem).gridColumn.Index;
            if (searchSeries == null) {
                searchSeries = new digit.LabeledBarSeries(yitem.DataCount, "searchseries", chart1, Color.RoyalBlue, Color.Black, 0.6f);
                chart1.AddSeries(searchSeries);
            } else if (searchSeries.MaxPointCount < yitem.DataCount)
                searchSeries.MaxPointCount = yitem.DataCount;
            searchSeries.Clear();
            chart1.AxesExtrema = digit.AxesExtrema.Fixed;
            //if (float.IsInfinity(yitem.max))
                chart1.AxesExtrema |= digit.AxesExtrema.AutoMaxY;
            //else
            //    chart1.YAxisMax = yitem.max;

            //if (float.IsInfinity(yitem.min))
                chart1.AxesExtrema |= digit.AxesExtrema.AutoMinY;
            //else
            //    chart1.YAxisMin = yitem.min;

            float x=1, y;
            for (int r = 1; r < yitem.DataCount; r++) {
                //y = (float)((DataGridViewParameterCell)grid[columnIndex, r]).Parameter.Value;
                y = yitem.GetValue(r);
                x = r;
                searchSeries.AddPoint(x, y, yitem.GetPointLabel(r), false);
            }
            chart1.XAxisMin = 0;
            chart1.XAxisMax = yitem.DataCount;
            //chart1.XAxisUnit = 1;
            //chart1.YAxisUnit = (chart1.YAxisMax - chart1.YAxisMin) / 10;
            chart1.Refresh();
        }

        private void ProjectForm_HelpRequested(object sender, HelpEventArgs hlpevent) {
            Help.ShowHelp(this, MainForm.helpfile, HelpNavigator.KeywordIndex, "Project");
        }

        private void expressionButton_Click(object sender, EventArgs e) {
            //if (sender == button1) {
            //    Tokenizer tokenizer = new Tokenizer(expressionTxt.Text);
            //    while (tokenizer.NextToken() != TokenType.EOF) {
            //        if (tokenizer.TType == TokenType.Literal) {
            //            try {

            //                if (!project.Variables.ContainsKey(tokenizer.Literal)) {
            //                    IParameter parameter = project.GetParameter(tokenizer.Literal);
            //                    project.Variables.Add(tokenizer.Literal, parameter);
            //                }
            //            } catch {
            //                MessageBox.Show("Unrecognized literal. Check addresses in expression", Application.ProductName, MessageBoxButtons.OK, MessageBoxIcon.Error);
            //            }
            //        }
            //    }
            //    try {
            //        Expression expression = new Expression(expressionTxt.Text, ExtractValue, project.Variables);
            //        currentBindingParameter.Expression = expression;
            //        if (!project.ExpressionedParameters.Contains(currentBindingParameter))
            //            project.ExpressionedParameters.Add(currentBindingParameter);
            //    } catch {
            //        MessageBox.Show("Invalid expression format.", Application.ProductName, MessageBoxButtons.OK, MessageBoxIcon.Error);
            //    }
            //}
            //splitContainer1.Panel2Collapsed = true;
            //bindingsBrowser.DocumentText = generateBindingsView();
        }

        //double ExtractValue(object parameter) {
        //    return ((IParameter)parameter).Value;
        //}

        private void documentsGrid_CellFormatting(object sender, DataGridViewCellFormattingEventArgs e) {
            object o = documentsGrid.Rows[e.RowIndex].Cells[e.ColumnIndex].Value;
            //if (o.GetType() != typeof(string) && o.GetType() != typeof(bool)) {
            if (o is List<ISpectrum>) {
                ISpectraContainer container = (ISpectraContainer)documentsGrid.Rows[e.RowIndex].HeaderCell.Value;
                e.Value = String.Format("{0}{1}", container.Spectra[0].Name, 
                    (container.Spectra.Count>1) ? String.Format(" ({0} more)", container.Spectra.Count - 1) : "");
            } else if (o is IModel) {
                e.Value = ((IModel)o).Name;
            }
        }

        private void documentsGrid_CellValueChanged(object sender, DataGridViewCellEventArgs e) {
            if (e.RowIndex>=0) {
                ISpectraContainer container = (ISpectraContainer)documentsGrid.Rows[e.RowIndex].HeaderCell.Value;
                object o = documentsGrid.Rows[e.RowIndex].Cells[e.ColumnIndex].Value;
                //switch (e.ColumnIndex) {
                //    case 0: 
                //        container.Name = o.ToString();

                //        break;
                //    case 1: container.Enabled = (bool)o; break;
                //}
                if (e.ColumnIndex == 1)
                    container.Name = o.ToString();
                //change visibility of tabpage 
                foreach (SpectraContainerTabPage tab in documentTabs) {
                    if (tab.SpectraContainer == container) {
                        tab.Text = container.Name;
                        tab.Parent = container.Enabled ? projectTabControl : hiddenDocsTabControl;
                        break;
                    }
                }
            }
            NotSavedChanges = true;
        }

        private void documentsGrid_CurrentCellDirtyStateChanged(object sender, EventArgs e) {
            if (documentsGrid.IsCurrentCellDirty)
                documentsGrid.CommitEdit(DataGridViewDataErrorContexts.Commit);
        }

        private void btnAddDocument_Click(object sender, EventArgs e) {
            NewDocument nd = new NewDocument(project);
            if (nd.ShowDialog() == DialogResult.OK) {

                ISpectraContainer container = project.CreateContainer(
                    nd.DocumentName,
                    nd.Model,
                    nd.SpectraFiles,
                    nd.GroupDefinitions);
                project.AddSpectraContainer(container);
                //ISpectrum spectrum;
                if (nd.TemplateContainer != null) {
                    ISpectraContainer sc = nd.TemplateContainer;
                    for (int spectrumId = 0; spectrumId < sc.Spectra.Count; spectrumId++) {
                        container.Spectra[spectrumId].copy(sc.Spectra[spectrumId], "source", CopyOptions.Status | CopyOptions.Value);
                        container.Spectra[spectrumId].copy(sc.Spectra[spectrumId], "prompt", CopyOptions.Status | CopyOptions.Value);
                        container.Spectra[spectrumId].copy(sc.Spectra[spectrumId], "ranges", CopyOptions.Status | CopyOptions.Value);
                    }
                }
                CreateContainerTab(container);
                FillDocumentsGrid();

            }
        }

        private void btnRemoveDocument_Click(object sender, EventArgs e) {
            DeleteDocument dd = new DeleteDocument(project);
            if (dd.ShowDialog() == DialogResult.OK) {
                NotSavedChanges = true;
                foreach (object item in dd.checkedListBox1.CheckedItems) {
                    ISpectraContainer sc = (ISpectraContainer)item;

                    //hidden documents
                    int tabId = hiddenDocsTabControl.TabCount - 1;
                    while (tabId >= 0) {
                        SpectraContainerTabPage page = (SpectraContainerTabPage)hiddenDocsTabControl.TabPages[tabId];
                        if (page.SpectraContainer == sc)
                            hiddenDocsTabControl.TabPages.Remove(page);
                        tabId--;
                    }

                    //visible documents
                    tabId = projectTabControl.TabCount - 1;
                    while (tabId >= 0) {
                        TabPage page = projectTabControl.TabPages[tabId];
                        if (page is SpectraContainerTabPage) {
                            if (((SpectraContainerTabPage)page).SpectraContainer == sc)
                                projectTabControl.TabPages.Remove(page);
                        }
                        tabId--;
                    }
                    project.Containers.Remove(sc);
                }
                FillDocumentsGrid();
            }
        }

        private void documentsGrid_CellClick(object sender, DataGridViewCellEventArgs e) {
            int i;
            if (e.RowIndex >= 0)
                if (documentsGrid.Rows[e.RowIndex].Cells[e.ColumnIndex] is DataGridViewButtonCell) {
                    ISpectraContainer container = (ISpectraContainer)documentsGrid.Rows[e.RowIndex].HeaderCell.Value;
                    if (e.ColumnIndex == 2) {
                        SpectraDialog dialog = new SpectraDialog(container);
                        int bufferSize = 0;
                        if (dialog.ShowDialog() == DialogResult.OK) {
                            ISpectrum lastSpectrumToRemove = null;

                            List<ISpectrum> spectraToRemove = new List<ISpectrum>();
                            List<ISpectrum> spectraToAdd = new List<ISpectrum>();
                            //removing spectra
                            int spectrumId = 0;
                            while (spectrumId < container.Spectra.Count) {
                                bool exists = false;
                                i = 0;
                                //while (i < dialog.SpectraFiles.Length && !exists) {
                                while (i < dialog.spectraList.Items.Count && !exists) {
                                    if (dialog.spectraList.Items[i] is ISpectrum)
                                        exists = ((ISpectrum)dialog.spectraList.Items[i]) == container.Spectra[spectrumId];
                                    //exists = System.IO.Path.GetFileNameWithoutExtension(dialog.SpectraFiles[i]).Equals(System.IO.Path.GetFileNameWithoutExtension(container.Spectra[spectrumId].Path));
                                    i++;
                                }
                                if (!exists) {
                                    spectraToRemove.Add(container.Spectra[spectrumId]);
                                    if (container.Spectra.Count > 1)
                                        container.RemoveSpectrum(container.Spectra[spectrumId]);
                                    else {
                                        lastSpectrumToRemove = container.Spectra[spectrumId];
                                        //bufferSize = lastSpectrumToRemove.DataLength;
                                        spectrumId++;
                                    }
                                } else
                                    spectrumId++;
                            }
                            //checking which spectra to add and calculating data buffer size increase
                            int stat;
                            for (i = 0; i < dialog.spectraList.Items.Count; i++)
                                if (dialog.spectraList.Items[i] is string) {
                                    System.IO.TextReader reader;
                                    using (reader = new System.IO.StreamReader((string)dialog.spectraList.Items[i])) {
                                        bufferSize += Evel.engine.SpectrumBase.getSpectrumData(reader, true, null, -1, out stat);
                                    }
                                } else if (dialog.spectraList.Items[i] is ISpectrum)
                                    bufferSize += ((ISpectrum)dialog.spectraList.Items[i]).DataLength;
                            int bufferStart = container.ResizeBuffer(bufferSize);
                            if (lastSpectrumToRemove != null)
                                bufferStart = 0;
                            //adding spectra
                            for (i = 0; i < dialog.spectraList.Items.Count; i++)
                                if (dialog.spectraList.Items[i] is string) {
                                    ISpectrum spectrum = container.CreateSpectrum(
                                        (string)dialog.spectraList.Items[i],
                                        bufferStart);
                                    spectraToAdd.Add(spectrum);
                                    bufferStart = spectrum.BufferEndPos + 1;
                                    container.AddSpectrum(spectrum, CopyOptions.Value | CopyOptions.Status);
                                }
                            if (lastSpectrumToRemove != null)
                                container.RemoveSpectrum(lastSpectrumToRemove);
                            //refreshing document group grids
                            SpectraContainerTabPage containerTabPage = getContainerPage(container);
                            TabControl groupsTabControl = (TabControl)containerTabPage.Controls.Find("groupsControl", true)[0];
                            foreach (TabPage page in groupsTabControl.TabPages)
                                if (page is GroupTabPage)
                                    ((GroupTabPage)page).Reset();
                            foreach (TabPage page in projectTabControl.TabPages) {
                                if (page is SharedGroupTabPage) {
                                    SharedGroupTabPage spage = (SharedGroupTabPage)page;
                                    if (spage.Binding.ContainsContainer(container)) {
                                        for (i = 0; i < spectraToRemove.Count; i++)
                                            spage.Spectra.Remove(spectraToRemove[i]);
                                        for (i = 0; i < spectraToAdd.Count; i++)
                                            if (!spage.Spectra.Contains(spectraToAdd[i]))
                                                spage.Spectra.AddRange(spectraToAdd);
                                    }
                                    spage.Reset();
                                }
                            }
                        }
                    //changing model
                    } else if (e.ColumnIndex == 0) {
                        ModelChooserDialog dialog = new ModelChooserDialog(container.Model.ProjectType, container.Model.Name);
                        if (dialog.ShowDialog() == DialogResult.OK) {
                            IModel model = AvailableAssemblies.getModel(dialog.Selection.plugin.className);
                            if (project.BindingsManager.Contains(container)) {
                                HashSet<ISpectraContainer> modifyingContainers = new HashSet<ISpectraContainer>();
                                ParameterizedThreadStart findContainers = null;
                                StringBuilder builder = new StringBuilder();
                                findContainers = delegate(object con) {
                                    foreach (Evel.interfaces.Binding b in project.BindingsManager.GetBindings((ISpectraContainer)con)) {
                                        if (b is GroupBinding) {
                                            for (i = 0; i < ((GroupBinding)b).Containers.Length; i++) {
                                                ISpectraContainer c = ((GroupBinding)b).Containers[i];
                                                if (!modifyingContainers.Contains(c)) {
                                                    modifyingContainers.Add(c);
                                                    builder.AppendFormat("{0}\n", c.Name);
                                                    if (c != con)
                                                        findContainers(c);
                                                }
                                            }
                                        }
                                    }
                                };
                                findContainers(container);
                                if (MessageBox.Show(
                                    String.Format("Following documents forms interdocument dependiences:\n\n{0}\nChanging theoretical model need to be performed in all binded documents to maintain existing group bindings. Continue?", builder.ToString()),
                                    Application.ProductName, MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes)
                                    foreach (ISpectraContainer c in modifyingContainers)
                                        c.Model = model;
                            } else
                                container.Model = model;
                            foreach (TabPage page in projectTabControl.TabPages)
                                if (page is SpectraContainerTabPage) {
                                    foreach (TabPage gpage in ((SpectraContainerTabPage)page)._groupsControl.TabPages)
                                        if (gpage is GroupTabPage) {
                                            //((GroupTabPage)gpage).
                                            for (i = 0; i < model.GroupsDefinition.Length; i++)
                                                if (model.GroupsDefinition[i].name == ((GroupTabPage)gpage).GroupDefinition.name)
                                                    ((GroupTabPage)gpage).GroupDefinition = model.GroupsDefinition[i];
                                            //((GroupTabPage)gpage).Reset();
                                        }
                                } else if (page is SharedGroupTabPage) {
                                    for (i = 0; i < model.GroupsDefinition.Length; i++)
                                        if (model.GroupsDefinition[i].name == ((GroupTabPage)page).GroupDefinition.name)
                                            ((GroupTabPage)page).GroupDefinition = model.GroupsDefinition[i];
                                }
                        }
                    }
                    FillDocumentsGrid();
                }
        }

        private SpectraContainerTabPage getContainerPage(ISpectraContainer container) {
            SpectraContainerTabPage page = null;
            foreach (TabPage p in projectTabControl.TabPages)
                if (p is SpectraContainerTabPage)
                    if (((SpectraContainerTabPage)p).SpectraContainer == container) {
                        page = (SpectraContainerTabPage)p;
                        break;
                    }
            if (page == null) {
                foreach (TabPage p in hiddenDocsTabControl.TabPages)
                    if (p is SpectraContainerTabPage)
                        if (((SpectraContainerTabPage)p).SpectraContainer == container) {
                            page = (SpectraContainerTabPage)p;
                            break;
                        }
            }
            return page;
        }

        private void tabController_SelectedIndexChanged(object sender, EventArgs e) {
            propertiesTabControl.SelectedIndex = tabController.SelectedIndex;
        }

        private void tabController_Format(object sender, ListControlConvertEventArgs e) {
            e.Value = ((TabPage)e.ListItem).Text;
        }

        private void refreshBindingsView() {
            try {
                webBrowser1.DocumentText = generateBindingsView();
                //foreach (SpectraContainerTabPage tp in documentTabs) {
                //    TabControl tabControl = (TabControl)tp.Controls.Find("groupsControl", true)[0];
                //    foreach (GroupTabPage gtp in tabControl.TabPages) {
                //        gtp.SetDataGrid(true);
                //    }
                //}
            } catch (InvalidCastException) {
                MessageBox.Show("Couldn't operate on one of the LT10 controls. The cause may lay in unsufficient rights put for LT10. Setting any kind of bindings is now disabled.", Application.ProductName, MessageBoxButtons.OK, MessageBoxIcon.Warning);
                tabController.Items.Remove(tbBindings);
            } catch (Exception e) {
                MessageBox.Show(e.StackTrace);
            }
        }

        private string generateBindingsView() {
            StringBuilder builder = new StringBuilder(1000);
            int i;
            builder.Append(@"<html><head><style type=""text/css"">");
            builder.AppendFormat("html, dl, table {{ font-family: {0}; font-size: {1}px; }}", this.Font.FontFamily.Name, this.Font.Height);
            builder.Append(@"dt { font-weight: bold; text-decoration: underline; margin-bottom: 10px; }");
            builder.Append(@"dd { margin-bottom: 10px; }");
            builder.Append(@"table.parameter th, table.group th { background-color: #2c4d08; color: white; padding: 0px 10px; }");
            builder.Append(@"table.parameter th { background-color: #08224d; }");
            builder.Append(@"table.group td { border: 1px solid silver; padding: 5px; background-color: #e5ffc9; text-align: center;}");
            builder.Append(@"table.parameter td { border: 1px solid silver; padding: 5px; background-color: #c9ddff; text-align: center;}");
            builder.Append("</style></head><body>");
            //dt
            if (project.BindingsManager.Count > 0) {
                builder.Append(@"<dl>");
                int id=1;
                string doc;
                string group;
                string compId;
                string bindingName;
                foreach (Evel.interfaces.Binding b in project.BindingsManager) {
                    builder.AppendFormat("<dt{0}>{1}. ", (id > 1) ? @" style="" border-top: 1px solid black; """ : "", id);
                    if (b is ParameterBinding) {
                        ParameterBinding pb = (ParameterBinding)b;
                        bindingName = (b.Name == String.Empty) ? 
                            String.Format("Parameter {0}", DefaultGroupGUI.BuildFormatedString(
                                DefaultGroupGUI.ConvertHeaderToHTML(pb.Parameters[0].Definition.Header),
                                DefaultGroupGUI.StringFormatTarget.Html)) :
                            b.Name;
                        builder.AppendFormat("{0} ({1} ± {2})</dt>",
                            bindingName,
                            pb.Parameters[0].Value,
                            pb.Parameters[0].Error);
                        builder.Append("<dd><table class=\"parameter\">");
                        builder.Append("<tr><th>Document</th><th>Group</th><th>Parameter</th></tr>");
                        foreach (IParameter p in pb.Parameters) {
                            Evel.engine.ProjectBase.getParameterInfo(p, out doc, out group, out compId);
                            builder.AppendFormat("<tr><td class=\"containerName\">{0}</td><td>{1}</td>\n<td>{2}</td></tr>",
                                doc, group, DefaultGroupGUI.BuildFormatedString(
                                    DefaultGroupGUI.ConvertHeaderToHTML(p.Definition.Header),
                                    DefaultGroupGUI.StringFormatTarget.Html,
                                    compId));
                        }
                    } else { //groupBinding
                        GroupBinding gb = (GroupBinding)b;
                        bindingName = (gb.Name == String.Empty) ? "Shared groups" : gb.Name;
                        builder.AppendFormat("{0}</dt><dd><table class=\"group\">", bindingName);
                        builder.AppendFormat("<tr><th>Document</th><th colspan=\"{0}\">Groups</th></tr>", gb.Groups.Length);
                        builder.AppendFormat("<tr><td class=\"containerName\">{0}</td>", gb.Containers[0].Name);
                        for (i = 0; i < gb.Groups.Length; i++)
                            builder.AppendFormat("<td rowspan=\"{0}\">{1}</td>", gb.Containers.Length, gb.Groups[i]);
                        builder.Append("</tr>");
                        for (i = 1; i < gb.Containers.Length; i++)
                            builder.AppendFormat("<tr><td class=\"containerName\">{0}</td></tr>", gb.Containers[i].Name);
                    }
                    builder.AppendFormat("</table><br /><input type=\"button\" href=\"javascript:void(0)\" onclick=\"window.external.removeBinding('{0}')\" value=\"remove\" />", id-1);
                    builder.AppendFormat("<input type=\"button\" href=\"javascript:void(0)\" onclick=\"window.external.renameBinding('{0}')\" value=\"rename\" />", id - 1);
                    builder.AppendFormat("<input type=\"button\" href=\"javascript:void(0)\" onclick=\"window.external.modifyBinding('{0}')\" value=\"modify\" /></dd>", id - 1);
                    id++;
                }
                builder.Append("</dl>");
            } else {
                builder.Append("<p>No bindings defined</p>");
            }
            builder.Append("</body></html>");
            return builder.ToString();
        }

        //public bool findBinding(IParameter parameter, out Evel.interfaces.Binding binding) {
        //    int i;
        //    foreach (Evel.interfaces.Binding b in project.Bindings) {
        //        for (i = 0; i<b.parameters.Length; i++)
        //            if (b.parameters[i] == parameter) {
        //                binding = b;
        //                return i == 0;
        //            }
        //    }
        //    binding = null;
        //    return false;
        //}

        private void RebuildTabPages() {

        }

        public void modifyBinding(int id) {
            Evel.interfaces.Binding binding = project.BindingsManager[id];
            if (binding is ParameterBinding) {
                BindingCreatorForm bcf = new BindingCreatorForm(project, (ParameterBinding)binding);
                if (bcf.ShowDialog() == DialogResult.OK) {
                    rebuildGridsWithParameterBinding((ParameterBinding)binding, bcf.AffectedContainers);
                    refreshBindingsView();
                }
            } else if (binding is GroupBinding) {
                GroupBindingCreatorForm gbcf = new GroupBindingCreatorForm(project, (GroupBinding)binding);
                if (gbcf.ShowDialog() == DialogResult.OK) {
                    //MessageBox.Show("This feature will be available in the nearest future", Application.ProductName, MessageBoxButtons.OK, MessageBoxIcon.Information);
                    //return;
                    ModifyBoundedGroupTabPages((GroupBinding)binding);
                    refreshBindingsView();
                    NotSavedChanges = true;

                        //TODO : reset grids and show those which are no longer binded
                }
            }
        }

        public void renameBinding(int id) {
            Evel.interfaces.Binding binding = project.BindingsManager[id];
            DocumentNameDialog dialog = new DocumentNameDialog();
            dialog.Text = "Binding name";
            if (dialog.ShowDialog() == DialogResult.OK)
                binding.Name = dialog.textBox1.Text;
            for (int i = 0; i < projectTabControl.TabCount; i++)
                if (projectTabControl.TabPages[i] is SharedGroupTabPage)
                    ((SharedGroupTabPage)projectTabControl.TabPages[i]).UpdateName();
            refreshBindingsView();
        }

        public void removeBinding(int id) {
            Evel.interfaces.Binding binding = project.BindingsManager[id];
            int i;
            project.BindingsManager.Remove(binding);
            refreshBindingsView();
            NotSavedChanges = true;
            //create splitted groups in container pages
            if (binding is GroupBinding) {
                GroupBinding gb = (GroupBinding)binding;
                RemoveBoundedGroupTabPages(gb);
                foreach (SpectraContainerTabPage page in documentTabs)
                    if (gb.ContainsContainer(page.SpectraContainer)) {
                        for (i = 0; i < gb.Groups.Length; i++)
                            page.InsertGroupTabPage(gb.Groups[i]).refreshReferences();
                        page.Parent = page._groupsControl.TabCount > 0 ? projectTabControl : hiddenDocsTabControl;
                        //page.SortableGroupGrids = true;
                    }
                binding.Dispose();
            } else {
                binding.Dispose();
                List<ISpectraContainer> affcontainers = new List<ISpectraContainer>(binding.Containers);
                rebuildGridsWithParameterBinding((ParameterBinding)binding, affcontainers);
            }
        }

        private void rebuildGridsWithParameterBinding(ParameterBinding binding, List<ISpectraContainer> affectedContainers) {
            int i,j;
            //rebuild group pages
            for (i = 0; i < documentTabs.Count; i++)
                //if (binding.ContainsContainer(documentTabs[i].SpectraContainer))
                if (affectedContainers.Contains(documentTabs[i].SpectraContainer))
                    for (j = 0; j < documentTabs[i].GroupsControl.TabCount; j++)
                        if (documentTabs[i].GroupsControl.TabPages[j] is GroupTabPage)
                            //((GroupTabPage)documentTabs[i].GroupsControl.TabPages[j]).SetDataGrid(true);
                            ((GroupTabPage)documentTabs[i].GroupsControl.TabPages[j]).Reset();

            //refresh SharedGroupTabPages
            for (i = 0; i<projectTabControl.TabCount; i++)
                if (projectTabControl.TabPages[i] is SharedGroupTabPage) {
                    for (j=0; j < ((SharedGroupTabPage)projectTabControl.TabPages[i]).Binding.Containers.Length; j++)
                        //if (binding.ContainsContainer(((SharedGroupTabPage)projectTabControl.TabPages[i]).Binding.Containers[j])) {
                        if (affectedContainers.Contains(((SharedGroupTabPage)projectTabControl.TabPages[i]).Binding.Containers[j])) {
                            //((SharedGroupTabPage)projectTabControl.TabPages[i]).SetDataGrid(true);
                            ((SharedGroupTabPage)projectTabControl.TabPages[i]).Reset();
                            break; //this page is already reset. proceed to next pages
                        }
                }
        }

        private void tsbCreateBinding_Click(object sender, EventArgs e) {
            BindingCreatorForm bcf = new BindingCreatorForm(project, null);
            if (bcf.ShowDialog() == DialogResult.OK) {
                project.BindingsManager.Add(bcf.Binding);
                refreshBindingsView();
                NotSavedChanges = true;
                rebuildGridsWithParameterBinding((ParameterBinding)bcf.Binding, bcf.AffectedContainers);
            }
        }


        private void tsbCreateGroupBinding_Click(object sender, EventArgs e) {
            GroupBindingCreatorForm bcf = new GroupBindingCreatorForm(project);
            if (bcf.ShowDialog() == DialogResult.OK) {
                project.BindingsManager.Add(bcf.Binding);
                removeBoundedGroupsFromDocs(bcf.Binding);
                CreateBoundedGroupTabPages(bcf.Binding);
                
                refreshBindingsView();
                NotSavedChanges = true;
            }
        }

        private void removeBoundedGroupsFromDocs(GroupBinding binding) {
            int i;
            foreach (SpectraContainerTabPage page in documentTabs) {
                if (binding.ContainsContainer(page.SpectraContainer)) {
                    for (i = 0; i < binding.Groups.Length; i++)
                        page.RemoveGroupTabPage(binding.Groups[i]);
                    if (page._groupsControl.TabCount == 0)
                        page.Hide();
                }
            }
        }

        public ExcelFile createExcelFile() {
            ExcelFile ef = new ExcelFile();
            int[] startColumns = new int[] { 0, 0, 0, 0 };
            ef.Worksheets.Add("sample");
            ef.Worksheets.Add("source");
            ef.Worksheets.Add("prompt");
            ef.Worksheets.Add("ranges");
            foreach (TabPage page in projectTabControl.TabPages) {
                if (page is SpectraContainerTabPage) {
                    ((SpectraContainerTabPage)page).fillExcelWorksheets(ef.Worksheets, startColumns);
                } else if (page is SharedGroupTabPage) {
                    if (page.Text.Contains("sample"))
                        ((SharedGroupTabPage)page).fillExcelWorksheet(ef.Worksheets[0], ref startColumns[0]);
                    else if (page.Text.Contains("source"))
                        ((SharedGroupTabPage)page).fillExcelWorksheet(ef.Worksheets[1], ref startColumns[1]);
                    else if (page.Text.Contains("prompt"))
                        ((SharedGroupTabPage)page).fillExcelWorksheet(ef.Worksheets[2], ref startColumns[2]);
                    else if (page.Text.Contains("ranges"))
                        ((SharedGroupTabPage)page).fillExcelWorksheet(ef.Worksheets[3], ref startColumns[3]);
                }
            }
            return ef;
        }

        private void commonValuesGrid_CellPainting(object sender, DataGridViewCellPaintingEventArgs e) {

        }

        private void exportToolStripMenuItem_Click(object sender, EventArgs e) {
            SaveFileDialog sfd = new SaveFileDialog();
            sfd.InitialDirectory = MainForm.ProjectsPath; // System.IO.Directory.GetCurrentDirectory();
            sfd.FileName = Path.GetFileNameWithoutExtension(project.Caption);
            sfd.AddExtension = true;
            sfd.Filter = "MS Excel Spreadsheet 97-2003 (*.xls)|*.xls|MS Excel Spreadsheet (*.xlsx)|*.xlsx|CSV (tab-delimited)(*.csv)|*.csv|Open document spreadsheet (*.ods)|*.ods";
            sfd.FilterIndex = 1;
            if (sfd.ShowDialog() == DialogResult.OK) {
                try {
                    ExcelFile file = createExcelFile();
                    switch (sfd.FilterIndex) {
                        case 1: file.SaveXls(sfd.FileName); break;
                        case 2: file.SaveXlsx(sfd.FileName); break;
                        case 3: file.SaveCsv(sfd.FileName, '\t'); break;
                        case 4: file.SaveOds(sfd.FileName); break;
                    }
                } catch (Exception) {
                    MessageBox.Show("Couldn't export data! (The file is propably used by other process)", Application.ProductName, MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
        }

        private void ProjectForm_Load(object sender, EventArgs e) {
            this.Invoke(new ThreadStart(delegate() {
                Thread.Sleep(100);
                this.webBrowser1 = new WebBrowser();
                this.webBrowser1.Parent = panel1;
                this.webBrowser1.Dock = DockStyle.Fill;
                this.webBrowser1.ObjectForScripting = this;
                panel1.Controls.Add(webBrowser1);
                refreshBindingsView();
            }));
        }

        private void ProjectForm_KeyDown(object sender, KeyEventArgs e) {
            if (((e.KeyData & Keys.F5) == Keys.F5) && project.IsBusy && projectTabControl.SelectedIndex == 1)
                RefreshSearchTabPage();
        }

        private void importToolStripMenuItem_Click(object sender, EventArgs e) {
            try {
                OpenFileDialog dialog = new OpenFileDialog();
                dialog.InitialDirectory = MainForm.ProjectsPath;
                dialog.Filter = "LT10 Parameter Files (*.ltpe; *.ltpp; *.ltm; *.ltmi)|*.ltpe;*.ltpp;*.ltm;*.ltmi|All Files (*.*)|*.*";
                if (dialog.ShowDialog() == DialogResult.OK) {
                    ParametersImportForm form = new ParametersImportForm(dialog.FileName, this.project);
                    if (form.ShowDialog() == DialogResult.OK) {
                        int i, j, k;
                        ParametersImportForm.ImportedModel im;
                        ISpectraContainer isc;
                        GroupDefinition definition;
                        for (i = 0; i < form.importGridView.RowCount; i++) {
                            if (form.ImportedModelNames.IndexOf(form.importGridView[0, i].Value.ToString()) > 0) {
                                isc = (ISpectraContainer)form.importGridView[1, i].Value;
                                im = form.ImportedModels[form.ImportedModelNames.IndexOf(form.importGridView[0, i].Value.ToString()) - 1];
                                for (k = 2; k < form.importGridView.ColumnCount; k++)
                                    if ((bool)form.importGridView[k, i].Value == true) {
                                        definition = ((ParametersImportForm.DataGridViewCheckBoxGroupCell)form.importGridView[k, i]).groupDefinition;
                                        for (j = 0; j < isc.Spectra.Count && j < im.parameters.Count; j++)
                                            isc.Spectra[j].copy(im.parameters[j], definition, CopyOptions.Value);
                                        if (isc.Spectra.Count > im.parameters.Count)
                                            for (j = im.parameters.Count; j < isc.Spectra.Count; j++)
                                                isc.Spectra[j].copy(im.parameters[0], definition, CopyOptions.Value);
                                    }
                            }
                        }
                        //refresh tabs
                        for (i = 0; i < projectTabControl.TabCount; i++) {
                            if (projectTabControl.TabPages[i] is SpectraContainerTabPage) {
                                for (j = 0; j < ((SpectraContainerTabPage)projectTabControl.TabPages[i])._groupsControl.TabCount; j++)
                                    if (((SpectraContainerTabPage)projectTabControl.TabPages[i])._groupsControl.TabPages[j] is GroupTabPage)
                                        ((GroupTabPage)((SpectraContainerTabPage)projectTabControl.TabPages[i])._groupsControl.TabPages[j]).Reset();
                            } else if (projectTabControl.TabPages[i] is SharedGroupTabPage) {
                                ((SharedGroupTabPage)projectTabControl.TabPages[i]).Reset();
                            }
                        }
                }
                }
            } catch (ImportException exception) {
                MessageBox.Show(exception.Message, Application.ProductName, MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }

        private void contextMenuStrip1_Opening(object sender, CancelEventArgs e) {
            logarithmicScaleToolStripMenuItem.Checked = chart1.LogarythmicY;
        }

        private void logarithmicScaleToolStripMenuItem_Click(object sender, EventArgs e) {
            chart1.LogarythmicY = logarithmicScaleToolStripMenuItem.Checked;
            chart1.Invalidate();
        }

    }
}
