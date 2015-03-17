using System.Windows.Forms;
using Evel.interfaces;
using Evel.gui.interfaces;
using Evel.engine;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.ComponentModel;
//using MathExpressions;
using GemBox.Spreadsheet;

namespace Evel.gui {

    public class GroupTabPage : TabPage {

        protected GroupDefinition groupDefinition;
        //public ISpectraContainer container;
        //public List<ISpectrum> spectra;
        private List<ISpectrum> _spectra;
        protected ProjectForm _parentProjectForm;
        //public NumericUpDown componentCount;
        protected StatusStrip _statusStrip;
        protected bool _includeContribCell;
        protected ContextMenuStrip gridStrip;
        private IContainer components;
        public DataGridParameterView grid;
        protected Keys keys;
        protected Point mouseCoords;
        protected ToolStripMenuItem visibleColumnsMenuItem;
        protected ToolStripSeparator toolStripSeparator1;
        protected ToolStripMenuItem spectrumFitMenuItem;
        public IGroupGUI groupGUI;
        protected ToolStripSeparator adjustSeparator;
        protected ToolStripMenuItem adjustMenuItem;
        protected ToolStripSeparator toolStripSeparator2;
        protected ToolStripMenuItem copyMenuItem;
        protected ToolStripMenuItem pasteMenuItem;
        protected TabControl _parentTabControl;
        protected ToolStripSeparator bindSeparator;
        protected ToolStripMenuItem bindMenuItem;
        protected ToolStripMenuItem removeExMenuItem;
        protected ToolStripSeparator statusSeparator;
        protected ToolStripMenuItem statusMenuItem;
        public bool notSavedChanges;
        protected ToolStripMenuItem commonMenuItem;
        protected ToolStripMenuItem singleFitMenuItem;
        protected ToolStripSeparator extremaSeparator;
        protected ToolStripMenuItem minimumMenuItem;
        protected ToolStripMenuItem maximumMenuItem;
        protected ToolStripTextBox maxParameterValueTextBox;
        protected ToolStripTextBox minParameterValueTextBox;
        protected bool allowRangeStatusChange;
        private FlowLayoutPanel bottomPanel;

        public GroupTabPage(List<ISpectrum> spectra, GroupDefinition definition, StatusStrip statusStrip, TabControl parentTabControl, ProjectForm parentProjectForm)
            : base(definition.name) {
            this._parentProjectForm = parentProjectForm;
            this._parentTabControl = parentTabControl;
            this._statusStrip = statusStrip;
            this.groupDefinition = definition;
            this._includeContribCell = ((groupDefinition.Type & GroupType.Contributet) == GroupType.Contributet) && ((groupDefinition.Type & GroupType.CalcContribution) != GroupType.CalcContribution);
            //this.container = container;
            //this.spectra = new List<ISpectrum>(spectra);
            this._spectra = spectra; // new List<ISpectrum>(spectra);
            this.notSavedChanges = false;
            SplitContainer splitContainer = new SplitContainer();
            splitContainer.IsSplitterFixed = true;
            splitContainer.Orientation = Orientation.Horizontal;
            splitContainer.SplitterDistance = splitContainer.Height - 70;
            splitContainer.FixedPanel = FixedPanel.Panel2;
            splitContainer.Dock = DockStyle.Fill;
            this.Controls.Add(splitContainer);
            this.allowRangeStatusChange = true;
            
            InitializeComponent();

            //top panel
            splitContainer.Panel1.Controls.Add(grid);
            splitContainer.Panel1.Padding = new Padding(5);
            splitContainer.Panel1.Margin = new Padding(5);

            //bottom panel
            this.bottomPanel = new FlowLayoutPanel();
            this.bottomPanel.Dock = DockStyle.Fill;
            this.bottomPanel.FlowDirection = FlowDirection.LeftToRight;
            splitContainer.Panel2.Controls.Add(this.bottomPanel);
            this.groupGUI = AvailableGUIAssemblies.GetGroupGUI(spectra[0].Container.ParentProject.GetType(), this.grid, this._spectra, this.groupDefinition, this);
            BuildTools();
            SetDataGrid(false);
            //SetStatuses();
            grid.SaveUndoStep();
            this.notSavedChanges = false;
        }

        public List<ISpectrum> Spectra {
            get { return this._spectra; }
        }

        protected void ReasignSpectra() {
            for (int i = 1; i < grid.RowCount; i++)
                if (grid.Rows[i] is DataGridViewSpectrumRow)
                    _spectra[i - 1] = ((DataGridViewSpectrumRow)grid.Rows[i]).Spectrum;
        }

        protected void BuildTools() {
            bottomPanel.Controls.Clear();
            foreach (GroupBox groupBox in groupGUI.GetToolBoxes(this._spectra[0], new EventHandler(toolPropertyChanged))) {
                this.bottomPanel.Controls.Add(groupBox);
            }
        }

        void grid_CellMouseMove(object sender, DataGridViewCellMouseEventArgs e) {
            if (e.RowIndex < 0 || e.RowIndex >= grid.RowCount) return;
            if (e.ColumnIndex < 0 || e.ColumnIndex >= grid.ColumnCount) return;
            DataGridViewCell cell = grid[e.ColumnIndex, e.RowIndex];
            if (cell is DataGridViewParameterCell) {
                //IParameter p = ((DataGridViewParameterCell)cell).parameter;
                DataGridViewParameterCell parameterCell = (DataGridViewParameterCell)cell;
                
                this._statusStrip.Items["statusStatusLabel"].Text = parameterCell.Parameter.Status.ToString();
                if (!Double.IsInfinity(((DataGridViewSpectrumRow)grid.Rows[e.RowIndex]).Spectrum.Fit))
                    this._statusStrip.Items["chisqStatusLabel"].Text = String.Format("F:{0:G8}", ((DataGridViewSpectrumRow)grid.Rows[e.RowIndex]).Spectrum.Fit);
                else
                    this._statusStrip.Items["chisqStatusLabel"].Text = "";
                
                System.Text.StringBuilder pv = new System.Text.StringBuilder();
                pv.AppendFormat("{0:G08}", parameterCell.Value);
                if (parameterCell.Parameter.Error != 0)
                    pv.AppendFormat(" ± {0:G02}", double.IsInfinity(parameterCell.UserError) ? "∞+" : parameterCell.UserError.ToString("G4"));
                if (!double.IsInfinity(parameterCell.Parameter.Minimum) || !double.IsInfinity(parameterCell.Parameter.Maximum)) {
                    string smin, smax;
                    double exvalue;
                    if (double.IsNegativeInfinity(parameterCell.Parameter.Minimum))
                        smin = "-∞";
                    else {
                        exvalue = parameterCell.Parameter.Minimum;
                        parameterCell.ConvertToUserValue(parameterCell.Parameter, ref exvalue);
                        smin = exvalue.ToString("G3");
                    }
                    if (double.IsPositiveInfinity(parameterCell.Parameter.Maximum))
                        smax = "∞+";
                    else {
                        exvalue = parameterCell.Parameter.Maximum;
                        parameterCell.ConvertToUserValue(parameterCell.Parameter, ref exvalue);
                        smax = exvalue.ToString("G3");
                    }
                    pv.AppendFormat("  <{0} .. {1}>", smin, smax);
                        
                }
                this._statusStrip.Items["valueStatusLabel"].Text = pv.ToString();
                //if (parameterCell.Parameter.Error != 0)
                //    this._statusStrip.Items["valueStatusLabel"].Text = String.Format("{0:G08} ± {1:G02}", parameterCell.Value, parameterCell.UserError);
                //else
                //    this._statusStrip.Items["valueStatusLabel"].Text = String.Format("{0:G08}", parameterCell.Value);
             
                if (grid[e.ColumnIndex, 0] is DataGridViewComboBoxCell) {
                    if ((ParameterStatus)int.Parse(grid[e.ColumnIndex, 0].Value.ToString()) == ParameterStatus.None &&
                        parameterCell.Parameter.ReferenceGroup > 0) {
                        this._statusStrip.Items["referenceStatusLabel"].Text = String.Format("Reference group: {0}", parameterCell.Parameter.ReferenceGroup);
                        
                    }
                }
                    
            }
            if (grid.Rows[e.RowIndex] is DataGridViewSpectrumRow)
                if (((DataGridViewSpectrumRow)grid.Rows[e.RowIndex]).Spectrum.RangeArea > 0)
                    this._statusStrip.Items["areaStatusLabel"].Text = String.Format("A:{0:F1}mln",((DataGridViewSpectrumRow)grid.Rows[e.RowIndex]).Spectrum.Statistic / 1e6);
        }

        void toolPropertyChanged(object sender, EventArgs e) {
                SetDataGrid(false);
                SetStatuses();
                refreshReferences();
        }

        void grid_CurrentCellDirtyStateChanged(object sender, EventArgs e) {
            if (grid.IsCurrentCellDirty) {
                //TODO : add step to undo stack
                _parentProjectForm.AddUndoStep(new CellChangeStep(grid.CurrentCell, "Cell value change", this));
                grid.CommitEdit(DataGridViewDataErrorContexts.Commit);
            }
        }

        void grid_CellValueChanged(object sender, DataGridViewCellEventArgs e) {
            if (e.RowIndex >= 0) {
                //int compId = (int)Math.Floor((double)(e.ColumnIndex - grid.FixedCols) / groupDefinition.parameters.Length);
                //int parId = (e.ColumnIndex - grid.FixedCols) % groupDefinition.parameters.Length;
                int row;
                DataGridViewCell cell = grid[e.ColumnIndex, e.RowIndex];
                if (cell is DataGridViewComboBoxCell) {
                    DataGridViewComboBoxCell cc = (DataGridViewComboBoxCell)cell;
                    bool validStatus = false;
                    foreach (CellParameterStatus cps in DefaultGroupGUI.StatusesSource) {
                        if (cps.ValueMember == cell.Value.ToString()) {
                            validStatus = true;
                            cell.Style.ForeColor = cps.ValueColor;
                            if (cps.status != ParameterStatus.None) {
                                row = 1;
                                IParameter topParameter; // = ((DataGridViewParameterCell)grid[e.ColumnIndex, 1]).Parameter;
                                do {
                                    if (row >= grid.RowCount) {
                                        topParameter = null;
                                        break;
                                    } else
                                        topParameter = ((DataGridViewParameterCell)grid[e.ColumnIndex, row]).Parameter;
                                    row++;
                                } while ((topParameter.Status & ParameterStatus.Binding) > 0 && row < grid.RowCount);
                                //jesli znaleziono parametr niezwiazany - on jest parametrem nadrzednym w kolumnie parametrow common
                                if (topParameter != null) {
                                    topParameter.ReferencedParameter = null;
                                    for (row = 1; row < grid.RowCount; row++) {
                                        if (!(grid[e.ColumnIndex, row] is DataGridViewParameterCell)) continue;
                                        IParameter parameter = ((DataGridViewParameterCell)grid[e.ColumnIndex, row]).Parameter;
                                        if ((parameter.Status & ParameterStatus.Binding) == 0) {
                                            parameter.ReferenceGroup = 0;
                                            if (parameter != topParameter) {
                                                if ((cps.status & ParameterStatus.Common) > 0)
                                                    parameter.ReferencedParameter = topParameter;
                                                else
                                                    parameter.ReferencedParameter = null;
                                            }
                                            parameter.Status = cps.status;
                                        }
                                    }
                                }
                                //change states of other selected controlers
                                //if (sender != null) {
                                if (this.allowRangeStatusChange)
                                    foreach (DataGridViewCell selectedCell in grid.SelectedCells)
                                        if (selectedCell is DataGridViewComboBoxCell && selectedCell.Value.ToString() != cell.Value.ToString())
                                            selectedCell.Value = cell.Value;
                                //grid_CellValueChanged(null, new DataGridViewCellEventArgs(selectedCell.ColumnIndex, selectedCell.RowIndex));
                                //}
                                //modify satuses if special conditions are defined (intensities)
                                if (groupDefinition.StatusChanged != null) {
                                    StatusChangeEventArgs scea = new StatusChangeEventArgs(_spectra[0].Parameters[groupDefinition.name],
                                            _spectra[0],
                                            _spectra,
                                            topParameter,
                                            cps.status);
                                    for (int s = 0; s < _spectra.Count; s++) {
                                        scea.group = _spectra[s].Parameters[groupDefinition.name];
                                        scea.spectrum = _spectra[s];
                                        scea.parameter = ((DataGridViewParameterCell)grid[e.ColumnIndex, s+1]).Parameter;
                                        groupDefinition.StatusChanged(
                                            this,
                                            scea);
                                    }
                                    this.allowRangeStatusChange = false;
                                    SetStatuses();
                                    this.allowRangeStatusChange = true;
                                }
                            }
                            break;
                        }
                    }
                    if (!validStatus) {
                        DataGridView view = (DataGridView)sender;
                        MessageBox.Show("Invalid status", "Lt10", MessageBoxButtons.OK, MessageBoxIcon.Error);
                        cell.Value = (int)(ParameterStatus.Local | ParameterStatus.Free);
                    }
                }
                if (cell is DataGridViewParameterCell) {
                    try {
                        DataGridViewParameterCell parameterCell = (DataGridViewParameterCell)cell;
                        //parameterCell.UserValue = Double.Parse(cell.Value.ToString());
                        if ((parameterCell.Parameter.Definition.Properties & ParameterProperties.IsDependency) == ParameterProperties.IsDependency)
                            grid.Invalidate();
                        parameterCell.Parameter.Error = 0;
                    } catch { }
                }
                groupGUI.GridCellValueChange(sender, e);
                grid.Invalidate(); //.Refresh();
            }
            this.notSavedChanges = true;
        }

        private void SetStatuses() {
            //grid.CellValueChanged -= grid_CellValueChanged;
            foreach (DataGridViewCell cell in grid.Rows[0].Cells) {
                if (cell is DataGridViewComboBoxCell) {
                    ParameterStatus status = 0;
                    IParameter parameter;
                    int refGroupSum = 0;
                    for (int row = 1; row < grid.Rows.Count; row++) {
                        parameter = ((DataGridViewParameterCell)grid.Rows[row].Cells[cell.ColumnIndex]).Parameter;
                        status |= parameter.Status;
                        refGroupSum += parameter.ReferenceGroup;
                    }
                    status &= ~ParameterStatus.Binding;
                    if ((status & (ParameterStatus.Local | ParameterStatus.Common)) == (ParameterStatus.Local | ParameterStatus.Common) ||
                        (status & (ParameterStatus.Free | ParameterStatus.Fixed)) == (ParameterStatus.Free | ParameterStatus.Fixed) ||
                        refGroupSum > 0)
                        status = ParameterStatus.None;

                    cell.Value = ((int)status).ToString();
                }
            }
            //grid.CellValueChanged += grid_CellValueChanged;
        }

        public void SetDataGrid(bool rebuild) {
            int columnCount = groupGUI.GetColumnCount();
            if (grid.ColumnCount == columnCount && !rebuild) return;
            grid.Rows.Clear();
            grid.Columns.Clear();          
            //headers
            grid.ColumnCount = columnCount;
            //string[] headers = _groupGUI.GetHeaders();
            
            for (int colId = 0; colId < grid.ColumnCount;colId++) {
                //grid.Columns[colId].HeaderText = headers[colId];
                //grid.Columns[colId].HeaderCell.Style.Font = grid.Font; // new Font("Tahoma", 9, FontStyle.Bold);
                grid.Columns[colId].SortMode = DataGridViewColumnSortMode.NotSortable;
                grid.Columns[colId].HeaderCell.Style.Alignment = DataGridViewContentAlignment.MiddleCenter;
                grid.Columns[colId].Width = (colId < grid.FixedCols) ? 10 : 80;
            }
            groupGUI.SetHeaders();
            //add controllers row
            grid.Rows.Add(groupGUI.CreateControlersRow());
            //add spectra rows
            for (int specId=0; specId<_spectra.Count; specId++) {
                ISpectrum spectrum = _spectra[specId];
                DataGridViewRow spectrumRow = groupGUI.CreateSpectrumRow(spectrum);
                spectrumRow.HeaderCell.Value = spectrum;
                grid.Rows.Add(spectrumRow);
            }
            //hide useless columns
            int c, r;
            double sum = 0.0;
            ParameterStatus status = 0;
            IParameter parameter;
            for (c = grid.FixedCols; c < grid.ColumnCount; c++) {
                sum = 0.0;
                status = 0;
                for (r = 1; r < grid.RowCount && (status & ParameterStatus.Free) == 0 && sum == 0.0; r++)
                    if (grid[c, r] is DataGridViewParameterCell) {
                        parameter = ((DataGridViewParameterCell)grid[c, r]).Parameter;
                                        //always show if column is if contribution
                        if (parameter.Definition.Name.ToLower().Contains("contrib")) {
                            sum = 1;
                            break;
                        }
                        if ((parameter.Definition.Properties & ParameterProperties.ComponentIntensity) > 0 && _spectra[0].Parameters[groupDefinition.name].Components.Size == 1) {
                            break;
                        } else {
                            sum += parameter.Value * parameter.Value;
                            status |= parameter.Status;
                        }
                        if ((parameter.Definition.DefaultStatus & ParameterStatus.Fixed) == 0)
                            sum += 1; //zeby przerwac petle i nie dopuscic do ukrycia kolumny
                        if (parameter.Definition.BindedStatus != ParameterStatus.None)
                            sum += 1;
                    }
                if (grid[c, 1] is DataGridViewParameterCell)
                    grid.Columns[c].Visible &= (sum != 0.0 || (status & ParameterStatus.Free) > 0);
            }  
            for (c = 0; c < grid.FixedCols; c++)
                grid.Columns[c].HeaderCell.Style.Alignment = DataGridViewContentAlignment.MiddleLeft;           
            SetStatuses();
        }

        public void Reset() {
            SetDataGrid(true);
            refreshReferences();
            groupGUI.ResetTools(bottomPanel.Controls, toolPropertyChanged);
        }

        public void CellFormatting(Object sender, DataGridViewCellFormattingEventArgs e) {
            groupGUI.CellFormatting(sender, e);
        }

        private void InitializeComponent() {
            this.components = new System.ComponentModel.Container();
            System.Windows.Forms.DataGridViewCellStyle dataGridViewCellStyle1 = new System.Windows.Forms.DataGridViewCellStyle();
            System.Windows.Forms.DataGridViewCellStyle dataGridViewCellStyle2 = new System.Windows.Forms.DataGridViewCellStyle();
            this.gridStrip = new System.Windows.Forms.ContextMenuStrip(this.components);
            this.visibleColumnsMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.toolStripSeparator1 = new System.Windows.Forms.ToolStripSeparator();
            this.spectrumFitMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.singleFitMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.adjustSeparator = new System.Windows.Forms.ToolStripSeparator();
            this.adjustMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.toolStripSeparator2 = new System.Windows.Forms.ToolStripSeparator();
            this.copyMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.pasteMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.bindSeparator = new System.Windows.Forms.ToolStripSeparator();
            this.bindMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.removeExMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.statusSeparator = new System.Windows.Forms.ToolStripSeparator();
            this.statusMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.commonMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.extremaSeparator = new System.Windows.Forms.ToolStripSeparator();
            this.maximumMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.maxParameterValueTextBox = new System.Windows.Forms.ToolStripTextBox();
            this.minimumMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.minParameterValueTextBox = new System.Windows.Forms.ToolStripTextBox();
            this.grid = new Evel.gui.DataGridParameterView();
            this.gridStrip.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.grid)).BeginInit();
            this.SuspendLayout();
            // 
            // gridStrip
            // 
            this.gridStrip.Font = new System.Drawing.Font("Tahoma", 8F);
            this.gridStrip.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.visibleColumnsMenuItem,
            this.toolStripSeparator1,
            this.spectrumFitMenuItem,
            this.singleFitMenuItem,
            this.adjustSeparator,
            this.adjustMenuItem,
            this.toolStripSeparator2,
            this.copyMenuItem,
            this.pasteMenuItem,
            this.bindSeparator,
            this.bindMenuItem,
            this.removeExMenuItem,
            this.statusSeparator,
            this.statusMenuItem,
            this.commonMenuItem,
            this.extremaSeparator,
            this.maximumMenuItem,
            this.minimumMenuItem});
            this.gridStrip.Name = "contextMenuStrip1";
            this.gridStrip.ShowItemToolTips = false;
            this.gridStrip.Size = new System.Drawing.Size(212, 304);
            this.gridStrip.Paint += new System.Windows.Forms.PaintEventHandler(this.gridStrip_Paint_1);
            this.gridStrip.Opening += new System.ComponentModel.CancelEventHandler(this.contextMenuStrip1_Opening);
            // 
            // visibleColumnsMenuItem
            // 
            this.visibleColumnsMenuItem.Name = "visibleColumnsMenuItem";
            this.visibleColumnsMenuItem.Size = new System.Drawing.Size(211, 22);
            this.visibleColumnsMenuItem.Text = "Visible columns";
            // 
            // toolStripSeparator1
            // 
            this.toolStripSeparator1.Name = "toolStripSeparator1";
            this.toolStripSeparator1.Size = new System.Drawing.Size(208, 6);
            // 
            // spectrumFitMenuItem
            // 
            this.spectrumFitMenuItem.Name = "spectrumFitMenuItem";
            this.spectrumFitMenuItem.Size = new System.Drawing.Size(211, 22);
            this.spectrumFitMenuItem.Text = "Show spectrum fit";
            this.spectrumFitMenuItem.Click += new System.EventHandler(this.spectrumFitMenuItem_Click);
            // 
            // singleFitMenuItem
            // 
            this.singleFitMenuItem.Name = "singleFitMenuItem";
            this.singleFitMenuItem.Size = new System.Drawing.Size(211, 22);
            this.singleFitMenuItem.Text = "Fit";
            this.singleFitMenuItem.Click += new System.EventHandler(this.singleFitMenuItem_Click);
            // 
            // adjustSeparator
            // 
            this.adjustSeparator.Name = "adjustSeparator";
            this.adjustSeparator.Size = new System.Drawing.Size(208, 6);
            // 
            // adjustMenuItem
            // 
            this.adjustMenuItem.Name = "adjustMenuItem";
            this.adjustMenuItem.Size = new System.Drawing.Size(211, 22);
            this.adjustMenuItem.Text = "Adjust values";
            this.adjustMenuItem.Click += new System.EventHandler(this.adjustMenuItem_Click);
            // 
            // toolStripSeparator2
            // 
            this.toolStripSeparator2.Name = "toolStripSeparator2";
            this.toolStripSeparator2.Size = new System.Drawing.Size(208, 6);
            // 
            // copyMenuItem
            // 
            this.copyMenuItem.Name = "copyMenuItem";
            this.copyMenuItem.ShortcutKeyDisplayString = "Ctrl + C";
            this.copyMenuItem.Size = new System.Drawing.Size(211, 22);
            this.copyMenuItem.Text = "Copy";
            this.copyMenuItem.Click += new System.EventHandler(this.copyMenuItem_Click);
            // 
            // pasteMenuItem
            // 
            this.pasteMenuItem.Name = "pasteMenuItem";
            this.pasteMenuItem.ShortcutKeyDisplayString = "Ctrl + V";
            this.pasteMenuItem.Size = new System.Drawing.Size(211, 22);
            this.pasteMenuItem.Text = "Paste";
            this.pasteMenuItem.Click += new System.EventHandler(this.pasteMenuItem_Click);
            // 
            // bindSeparator
            // 
            this.bindSeparator.Name = "bindSeparator";
            this.bindSeparator.Size = new System.Drawing.Size(208, 6);
            this.bindSeparator.Visible = false;
            // 
            // bindMenuItem
            // 
            this.bindMenuItem.Name = "bindMenuItem";
            this.bindMenuItem.Size = new System.Drawing.Size(211, 22);
            this.bindMenuItem.Text = "Edit expression";
            this.bindMenuItem.Visible = false;
            this.bindMenuItem.Click += new System.EventHandler(this.bindMenuItem_Click);
            // 
            // removeExMenuItem
            // 
            this.removeExMenuItem.Name = "removeExMenuItem";
            this.removeExMenuItem.Size = new System.Drawing.Size(211, 22);
            this.removeExMenuItem.Text = "Clear expression";
            this.removeExMenuItem.Visible = false;
            this.removeExMenuItem.Click += new System.EventHandler(this.bindMenuItem_Click);
            // 
            // statusSeparator
            // 
            this.statusSeparator.Name = "statusSeparator";
            this.statusSeparator.Size = new System.Drawing.Size(208, 6);
            // 
            // statusMenuItem
            // 
            this.statusMenuItem.Name = "statusMenuItem";
            this.statusMenuItem.Size = new System.Drawing.Size(211, 22);
            this.statusMenuItem.Text = "Fixed";
            this.statusMenuItem.Click += new System.EventHandler(this.statusMenuItem_Click);
            // 
            // commonMenuItem
            // 
            this.commonMenuItem.Name = "commonMenuItem";
            this.commonMenuItem.Size = new System.Drawing.Size(211, 22);
            this.commonMenuItem.Text = "Partially common";
            this.commonMenuItem.Click += new System.EventHandler(this.commonMenuItem_Click);
            // 
            // extremaSeparator
            // 
            this.extremaSeparator.Name = "extremaSeparator";
            this.extremaSeparator.Size = new System.Drawing.Size(208, 6);
            // 
            // maximumMenuItem
            // 
            this.maximumMenuItem.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.maxParameterValueTextBox});
            this.maximumMenuItem.Name = "maximumMenuItem";
            this.maximumMenuItem.Size = new System.Drawing.Size(211, 22);
            this.maximumMenuItem.Text = "Parameter maximum value";
            this.maximumMenuItem.Click += new System.EventHandler(this.extremumMenuItem_Click);
            // 
            // maxParameterValueTextBox
            // 
            this.maxParameterValueTextBox.Name = "maxParameterValueTextBox";
            this.maxParameterValueTextBox.Size = new System.Drawing.Size(100, 21);
            this.maxParameterValueTextBox.TextChanged += new System.EventHandler(this.extrParameterValueTextBox_TextChanged);
            // 
            // minimumMenuItem
            // 
            this.minimumMenuItem.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.minParameterValueTextBox});
            this.minimumMenuItem.Name = "minimumMenuItem";
            this.minimumMenuItem.Size = new System.Drawing.Size(211, 22);
            this.minimumMenuItem.Text = "Parameter minimum value";
            this.minimumMenuItem.Click += new System.EventHandler(this.extremumMenuItem_Click);
            // 
            // minParameterValueTextBox
            // 
            this.minParameterValueTextBox.Name = "minParameterValueTextBox";
            this.minParameterValueTextBox.Size = new System.Drawing.Size(100, 21);
            this.minParameterValueTextBox.TextChanged += new System.EventHandler(this.extrParameterValueTextBox_TextChanged);
            // 
            // grid
            // 
            this.grid.AllowUserToAddRows = false;
            this.grid.AllowUserToDeleteRows = false;
            this.grid.AllowUserToResizeRows = false;
            this.grid.BackgroundColor = System.Drawing.SystemColors.Window;
            this.grid.CellBorderStyle = System.Windows.Forms.DataGridViewCellBorderStyle.Raised;
            dataGridViewCellStyle1.Alignment = System.Windows.Forms.DataGridViewContentAlignment.MiddleLeft;
            dataGridViewCellStyle1.BackColor = System.Drawing.SystemColors.Control;
            dataGridViewCellStyle1.Font = new System.Drawing.Font("Tahoma", 9F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(238)));
            dataGridViewCellStyle1.ForeColor = System.Drawing.SystemColors.WindowText;
            dataGridViewCellStyle1.SelectionBackColor = System.Drawing.SystemColors.Highlight;
            dataGridViewCellStyle1.SelectionForeColor = System.Drawing.SystemColors.HighlightText;
            dataGridViewCellStyle1.WrapMode = System.Windows.Forms.DataGridViewTriState.True;
            this.grid.ColumnHeadersDefaultCellStyle = dataGridViewCellStyle1;
            this.grid.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.DisableResizing;
            this.grid.ContextMenuStrip = this.gridStrip;
            dataGridViewCellStyle2.Alignment = System.Windows.Forms.DataGridViewContentAlignment.MiddleLeft;
            dataGridViewCellStyle2.BackColor = System.Drawing.SystemColors.Window;
            dataGridViewCellStyle2.Font = new System.Drawing.Font("Tahoma", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(238)));
            dataGridViewCellStyle2.ForeColor = System.Drawing.SystemColors.ControlText;
            dataGridViewCellStyle2.SelectionBackColor = System.Drawing.SystemColors.Highlight;
            dataGridViewCellStyle2.SelectionForeColor = System.Drawing.SystemColors.HighlightText;
            dataGridViewCellStyle2.WrapMode = System.Windows.Forms.DataGridViewTriState.False;
            this.grid.DefaultCellStyle = dataGridViewCellStyle2;
            this.grid.Dock = System.Windows.Forms.DockStyle.Fill;
            this.grid.FixedCols = 2;
            this.grid.Location = new System.Drawing.Point(0, 0);
            this.grid.Name = "grid";
            this.grid.RowHeadersVisible = false;
            this.grid.SelectionMode = System.Windows.Forms.DataGridViewSelectionMode.CellSelect;
            this.grid.Size = new System.Drawing.Size(240, 150);
            this.grid.TabIndex = 0;
            this.grid.CellValueChanged += new System.Windows.Forms.DataGridViewCellEventHandler(this.grid_CellValueChanged);
            //this.grid.SortCompare += new System.Windows.Forms.DataGridViewSortCompareEventHandler(this.grid_SortCompare);
            this.grid.Sorted += new System.EventHandler(this.grid_Sorted);
            this.grid.MouseMove += new System.Windows.Forms.MouseEventHandler(this.grid_MouseMove);
            this.grid.ColumnHeaderMouseClick += new System.Windows.Forms.DataGridViewCellMouseEventHandler(this.grid_ColumnHeaderMouseClick);
            this.grid.CellMouseDown += new System.Windows.Forms.DataGridViewCellMouseEventHandler(this.grid_CellMouseDown);
            this.grid.CellMouseMove += new System.Windows.Forms.DataGridViewCellMouseEventHandler(this.grid_CellMouseMove);
            this.grid.CellFormatting += new System.Windows.Forms.DataGridViewCellFormattingEventHandler(this.CellFormatting);
            this.grid.CellEndEdit += new System.Windows.Forms.DataGridViewCellEventHandler(this.grid_CellEndEdit);
            this.grid.CurrentCellDirtyStateChanged += new System.EventHandler(this.grid_CurrentCellDirtyStateChanged);
            this.grid.DataError += new System.Windows.Forms.DataGridViewDataErrorEventHandler(this.grid_DataError);
            // 
            // GroupTabPage
            // 
            this.Font = new System.Drawing.Font("Microsoft Sans Serif", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(238)));
            this.HelpRequested += new System.Windows.Forms.HelpEventHandler(this.GroupTabPage_HelpRequested);
            this.gridStrip.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.grid)).EndInit();
            this.ResumeLayout(false);

        }

        private void contextMenuStrip1_Opening(object sender, System.ComponentModel.CancelEventArgs e) {
            //visible columns
            visibleColumnsMenuItem.DropDownItems.Clear();
            for (int i=0; i<grid.ColumnCount; i++) {
                if (grid[i, 1] is DataGridViewParameterCell)
                    if ((((DataGridViewParameterCell)grid[i, 1]).Parameter.Definition.Properties & ParameterProperties.Hidden) == ParameterProperties.Hidden)
                        continue;
                ObjectToolStripMenuItem item = new ObjectToolStripMenuItem(grid.Columns[i], grid.Columns[i].HeaderText);
                item.Click += new EventHandler(columnToolStripMenuClick);
                item.Checked = grid.Columns[i].Visible;
                visibleColumnsMenuItem.DropDownItems.Add(item);
            }
            //adjusting items
            adjustSeparator.Visible = groupGUI.HasGraphicAdjustment;
            adjustMenuItem.Visible = groupGUI.HasGraphicAdjustment;
            IParameter parameter = null;
            try {
                if (grid[mouseCoords.X, mouseCoords.Y] is DataGridViewParameterCell && !_spectra[0].Container.ParentProject.IsBusy)
                    parameter = ((DataGridViewParameterCell)grid[mouseCoords.X, mouseCoords.Y]).Parameter;
            } catch (Exception) { }
            //copy/paste items
            copyMenuItem.Visible = pasteMenuItem.Visible = toolStripSeparator2.Visible = mouseCoords.Y > 0;
            //fitting items
            if (singleFitMenuItem.Visible = toolStripSeparator1.Visible = spectrumFitMenuItem.Visible = mouseCoords.Y > 0 && mouseCoords.X == 0) {
                singleFitMenuItem.Text = String.Format("Fit {0}", ((DataGridViewSpectrumRow)grid.Rows[mouseCoords.Y]).Spectrum.Name);
                spectrumFitMenuItem.Text = String.Format("Show {0} fit", ((DataGridViewSpectrumRow)grid.Rows[mouseCoords.Y]).Spectrum.Name);
            }
            //statuses
            statusSeparator.Visible = statusMenuItem.Visible = commonMenuItem.Visible = extremaSeparator.Visible = maximumMenuItem.Visible = minimumMenuItem.Visible = parameter != null && !grid[mouseCoords.X, mouseCoords.Y].ReadOnly;
            if (parameter != null) {
                bool visibility = (parameter.Definition.BindedStatus == ParameterStatus.None);
                bool commonVisibility;
                if (commonVisibility = grid.SelectedCells.Count > 1) {
                    try {
                        ParameterStatus status = (ParameterStatus)int.Parse(grid[mouseCoords.X, 0].Value.ToString());
                        commonVisibility = grid[mouseCoords.X, mouseCoords.Y].Selected &&
                        (((status & ParameterStatus.Local) == ParameterStatus.Local) ||
                            ((status & ParameterStatus.None) == ParameterStatus.None)); //&&
                        //!parameter.Definition.Name.ToLower().Contains("int") &&
                        //!parameter.Definition.Name.ToLower().Contains("contrib");
                    } catch {
                        commonVisibility = false;
                    }
                    for (int i = 1; (i < grid.SelectedCells.Count) && commonVisibility; i++) {
                        if (grid.SelectedCells[i] is DataGridViewParameterCell)
                            commonVisibility &= grid.SelectedCells[0].ColumnIndex == grid.SelectedCells[i].ColumnIndex;
                        else
                            commonVisibility = false;
                    }
                }

                //bindSeparator.Visible = visibility;
                //bindMenuItem.Visible = visibility;
                //removeExMenuItem.Visible = visibility;
                //if (visibility)
                //    removeExMenuItem.Enabled = parameter.Expression != null;
                //status
                visibility = //((parameter.Status & ParameterStatus.Local) == ParameterStatus.Local) &&
                    ((parameter.Definition.BindedStatus | ParameterStatus.None) == ParameterStatus.None) &&
                    mouseCoords.X > grid.FixedCols;

                statusMenuItem.Visible = visibility;
                statusSeparator.Visible = visibility | commonVisibility;
                commonMenuItem.Visible = commonVisibility || parameter.ReferenceGroup > 0;
                commonMenuItem.Checked = parameter.ReferenceGroup > 0;
                if (visibility)
                    statusMenuItem.Checked = (parameter.Status & ParameterStatus.Fixed) == ParameterStatus.Fixed;
                //extrema
                double value;
                UserValueConversionHandler conversion = ((DataGridViewParameterCell)grid[mouseCoords.X, mouseCoords.Y]).ConvertToUserValue;
                if (double.IsNegativeInfinity(parameter.Minimum))
                    minParameterValueTextBox.Text = "";
                else {
                    value = parameter.Minimum;
                    if (conversion != null)
                        conversion(parameter, ref value);
                    minParameterValueTextBox.Text = value.ToString("G5");
                }

                if (double.IsPositiveInfinity(parameter.Maximum))
                    maxParameterValueTextBox.Text = "";
                else {
                    value = parameter.Maximum;
                    if (conversion != null)
                        conversion(parameter, ref value);
                    maxParameterValueTextBox.Text = value.ToString("G5");
                }
            }
        }

        private void columnToolStripMenuClick(object sender, EventArgs e) {
            ((DataGridViewColumn)((ObjectToolStripMenuItem)sender).BindedObject).Visible = !((DataGridViewColumn)((ObjectToolStripMenuItem)sender).BindedObject).Visible;
        }

        //whole menu paint
        private void gridStrip_Paint_1(object sender, PaintEventArgs e) {
            Rectangle rect = e.ClipRectangle;
            rect.Offset(40, 0);
            e.Graphics.FillRectangle(new SolidBrush(System.Drawing.Color.WhiteSmoke), rect);
        }

        private void grid_CellMouseDown(object sender, DataGridViewCellMouseEventArgs e) {
            if (e.ColumnIndex < 0 || e.RowIndex < 0) return;

            mouseCoords = new Point(e.ColumnIndex, e.RowIndex);
            if (e.Button == MouseButtons.Right) {
                if (!grid[e.ColumnIndex, e.RowIndex].Selected) {
                    grid.ClearSelection();
                    grid[e.ColumnIndex, e.RowIndex].Selected = true;
                }
            }

            //changing status with middle mouse button
            if (e.ColumnIndex > 1 && e.RowIndex == 0) {
                if (!grid[e.ColumnIndex, e.RowIndex].ReadOnly)
                    if (grid[mouseCoords.X, mouseCoords.Y] is DataGridViewComboBoxCell && (e.Button == MouseButtons.Middle)) {
                        ParameterStatus status = (ParameterStatus)int.Parse(grid[mouseCoords.X, mouseCoords.Y].Value.ToString());

                        if (status != ParameterStatus.None) {
                            if ((keys & Keys.Control) > 0) {
                                if ((status & ParameterStatus.Local) > 0)
                                    status = status & ~ParameterStatus.Local | ParameterStatus.Common;
                                else if ((status & ParameterStatus.Common) > 0)
                                    status = status & ~ParameterStatus.Common | ParameterStatus.Local;
                            } else {
                                if ((status & ParameterStatus.Free) == ParameterStatus.Free)
                                    status = status & ~ParameterStatus.Free | ParameterStatus.Fixed;
                                else if ((status & ParameterStatus.Fixed) == ParameterStatus.Fixed)
                                    status = status & ~ParameterStatus.Fixed | ParameterStatus.Free;
                            }
                        }

                        grid[mouseCoords.X, mouseCoords.Y].Value = ((int)status).ToString();
                    }
            }
            if (e.ColumnIndex > 0 && e.RowIndex > 0) {
                if (grid[mouseCoords.X, mouseCoords.Y] is DataGridViewParameterCell && (e.Button == MouseButtons.Middle)) {
                    while (grid.SelectedCells.Count > 0)
                        grid.SelectedCells[0].Selected = false;
                    //grid[e.ColumnIndex, i].Selected = false;
                    int refGroup = ((DataGridViewParameterCell)grid[mouseCoords.X, mouseCoords.Y]).Parameter.ReferenceGroup;
                    if (refGroup > 0) {
                        for (int i = 1; i < grid.RowCount; i++)
                            if (((DataGridViewParameterCell)grid[mouseCoords.X, i]).Parameter.ReferenceGroup == refGroup)
                                grid[mouseCoords.X, i].Selected = grid[0, i].Selected = true;
                    }
                }
            }
            if (groupGUI.HasGraphicAdjustment && grid[mouseCoords.X, mouseCoords.Y] is DataGridViewButtonCell) 
                adjustMenuItem_Click(grid, new EventArgs());
        }

        private void spectrumFitMenuItem_Click(object sender, EventArgs e) {
            if (!_spectra[0].Container.ParentProject.IsBusy) {
                ISpectrum spectrum = _spectra[mouseCoords.Y - 1];
                SpectrumFitForm sff = new SpectrumFitForm(spectrum, _spectra, SpectrumFitFormControls.Ok | SpectrumFitFormControls.SpectraSelector);
                try {
                    sff.ShowDialog();
                } catch (Exception ex) {
                    MessageBox.Show(ex.Message, Application.ProductName, MessageBoxButtons.OK);
                }
            } else {
                MessageBox.Show("Cannot show fit due to active calculations process.", Application.ProductName, MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
        }

        //private void grid_SortCompare(object sender, DataGridViewSortCompareEventArgs e) {
        //    if (e.CellValue1 == null) {
        //        e.SortResult = (e.Column.HeaderCell.SortGlyphDirection == SortOrder.Ascending) ? -1 : 1;
        //        //e.SortResult = -1;
        //    } else {
        //        if (e.CellValue2 == null) {
        //            e.SortResult = (e.Column.HeaderCell.SortGlyphDirection == SortOrder.Ascending) ? 1 : -1;
        //            //e.SortResult = -1;
        //        } else {
        //            double cell1Value;
        //            double cell2Value;
        //            if (Double.TryParse(e.CellValue1.ToString(), out cell1Value) && Double.TryParse(e.CellValue2.ToString(), out cell2Value))
        //                e.SortResult = (cell1Value > cell2Value) ? 1 : ((cell1Value == cell2Value) ? 0 : -1);
        //            else
        //                e.SortResult = String.Compare(e.CellValue1.ToString(), e.CellValue2.ToString());
        //        }
        //    }
        //    e.Handled = true;
        //}

        public void refreshReferences() {
            int i, j;
            if (grid.RowCount < 2) return;
            //IBinding binding;
            //int bindingParameterId;
            for (int colId = grid.FixedCols; colId < grid.ColumnCount; colId++) {
                //if (grid[colId, 0].Value != null) //if column doesn't have status controller there is no need in refreshing references since parameters doesnt refere to other parameters
                    if (grid[colId, 1] is DataGridViewParameterCell) {
                        //ParameterStatus status = ((ParameterStatus)((int)int.Parse(grid[colId, 0].Value.ToString())));
                        IParameter topParameter = null;
                        IParameter tmpParameter;
                        int currentRefGroup = 0;
                        int parameterLeft = grid.RowCount - 2;
                        while (parameterLeft > 0) {
                            topParameter = null;
                            for (int rowId = 1; rowId < grid.RowCount; rowId++)
                                if (grid[colId, rowId] is DataGridViewParameterCell) {
                                    tmpParameter = ((DataGridViewParameterCell)grid[colId, rowId]).Parameter;
                                    if (tmpParameter.ReferenceGroup == currentRefGroup &&
                                        (tmpParameter.Status & ParameterStatus.Common) > 0) {
                                        topParameter = tmpParameter;
                                        parameterLeft--;
                                        break;
                                    } else if ((tmpParameter.Status & ParameterStatus.Common) == 0 &&
                                        tmpParameter.ReferenceGroup == currentRefGroup) {
                                        parameterLeft--;
                                        if ((tmpParameter.Status & ParameterStatus.Binding) == 0)
                                            tmpParameter.ReferencedParameter = null;
                                        if (!tmpParameter.HasReferenceValue) {
                                            tmpParameter.Status = tmpParameter.Status & ~ParameterStatus.Common | ParameterStatus.Local;
                                            tmpParameter.ReferenceGroup = 0;
                                        }
                                    }
                                }
                            if (topParameter != null) {
                                //if ((topParameter.Status & (ParameterStatus.Common | ParameterStatus.Binding)) == ParameterStatus.Common) {
                                //if ((status & (ParameterStatus.Common | ParameterStatus.Binding)) == ParameterStatus.Common) {
                                double value = topParameter.Value;
                                if ((topParameter.Status & ParameterStatus.Binding) == 0)
                                    topParameter.ReferencedParameter = null;
                                for (int rowId = 1; rowId < grid.RowCount; rowId++) {
                                    if (grid[colId, rowId] is DataGridViewParameterCell) {
                                        tmpParameter = ((DataGridViewParameterCell)grid[colId, rowId]).Parameter;
                                        if (tmpParameter.ReferenceGroup == topParameter.ReferenceGroup &&
                                            tmpParameter != topParameter) {
                                            tmpParameter.ReferencedParameter = topParameter;
                                            parameterLeft--;
                                        }
                                    }
                                }
                                topParameter.Value = value;
                            }
                            currentRefGroup++;
                        }
                        //make parameters local if there is only one parameter in reference group
                        for (i = 1; i < grid.RowCount; i++) {
                            if (grid[colId, i] is DataGridViewParameterCell) {
                                tmpParameter = ((DataGridViewParameterCell)grid[colId, i]).Parameter;
                                int refCount = 0;
                                for (j = 1; j < grid.RowCount; j++)
                                    if (grid[colId, j] is DataGridViewParameterCell)
                                        if ((((DataGridViewParameterCell)grid[colId, j]).Parameter).ReferenceGroup == tmpParameter.ReferenceGroup &&
                                            tmpParameter != ((DataGridViewParameterCell)grid[colId, j]).Parameter)
                                            refCount++;
                                if (refCount == 0 && (tmpParameter.Status & ParameterStatus.Common) == ParameterStatus.Common) {
                                    tmpParameter.Status = tmpParameter.Status & ~ParameterStatus.Common | ParameterStatus.Local;
                                    tmpParameter.ReferencedParameter = null;
                                    tmpParameter.ReferenceGroup = 0;
                                }
                            }
                        }
                    }
            }
            //bool everyVarChecked = false;
            //while (!everyVarChecked) {
            //    everyVarChecked = true;
            //    foreach (string key in container.ParentProject.Variables.Keys) {
            //        IParameter p = (IParameter)container.ParentProject.Variables[key];
            //        string pKey = container.ParentProject.GetParameterAddress(p);
            //        if (pKey != key) {
            //            container.ParentProject.Variables.Remove(key);
            //            container.ParentProject.Variables.Add(pKey, p);
            //            everyVarChecked = false;
            //            break;
            //        }
            //    }
            //}
        }

        protected ListSortDirection getSortDirection(SortOrder sortOrder) {
            ListSortDirection result;
            switch (sortOrder) {
                case SortOrder.Ascending: result = ListSortDirection.Ascending; break;
                default: result = ListSortDirection.Descending; break;
            }
            return result;
        }

        protected SortOrder getSortOrder(ListSortDirection sortDirection) {
            switch (sortDirection) {
                case ListSortDirection.Ascending: return SortOrder.Ascending;
                default: return SortOrder.Descending;
            }
        }

        private void grid_Sorted(object sender, EventArgs e) {

        }

        //private void SpectraSwap(int i, int j) {
        //    ISpectrum spectrum = spectra[i];
        //    spectra[i] = spectra[j];
        //    spectra[j] = spectrum;
        //}

        protected virtual bool IncludeRule(object sender, ISpectrum spectrum) {
            return spectrum.Container == ((GroupTabPage)sender)._spectra[0].Container;
        }

        public virtual void Sort(object sender, Comparison<ISpectrum> comparison, DataGridParameterView.IncludeRule irule) {
            grid.Sort(sender, comparison, null, irule);
            ReasignSpectra();
            refreshReferences();
            Invalidate();
        }

        protected virtual Comparison<ISpectrum> GetColumnComparison(int columnId) {
            switch (columnId) {
                case 0: return delegate(ISpectrum s1, ISpectrum s2) {
                        return s1.Name.CompareTo(s2.Name);
                    };
                case 1: return delegate(ISpectrum s1, ISpectrum s2) {
                        return s1.Parameters[0].Components[0]["key value"].Value.CompareTo(s2.Parameters[0].Components[0]["key value"].Value);
                    };
            }
            return null;
        }

        protected virtual void grid_ColumnHeaderMouseClick(object sender, DataGridViewCellMouseEventArgs e) {
            Comparison<ISpectrum> comp;
            if ((comp = GetColumnComparison(e.ColumnIndex)) == null) return;
            if (_spectra[0].Container.ParentProject.IsBusy) {
                MessageBox.Show("Sorting is not allowed while running calculations.", Application.ProductName, MessageBoxButtons.OK, MessageBoxIcon.Stop);
                return;
            }



            foreach (TabPage proPage in _parentProjectForm.projectTabControl.TabPages)
                if (proPage is SpectraContainerTabPage) {
                    foreach (TabPage gpage in ((SpectraContainerTabPage)proPage)._groupsControl.TabPages)
                        if (gpage is GroupTabPage)
                            ((GroupTabPage)gpage).Sort(this, comp, IncludeRule);
                } else if (proPage is SharedGroupTabPage) {
                    ((GroupTabPage)proPage).Sort(this, comp, IncludeRule);
                }
            
            //foreach (TabPage page in _parentTabControl.TabPages)
            //    if (page is GroupTabPage)
            //        ((GroupTabPage)page).Sort(this, comp, IncludeRule);
        }

        private void adjustMenuItem_Click(object sender, EventArgs e) {
            if (groupGUI.HasGraphicAdjustment) {
                ISpectrum spectrum = ((DataGridViewSpectrumRow)grid.Rows[mouseCoords.Y]).Spectrum;
                Form adjuster = groupGUI.CreateValuesAdjuster(_spectra, spectrum);
                adjuster.ShowDialog();
                grid.Invalidate();
            }                
        }

        //39.967\t0.19736\r\n36.906\t0.19316\r\n43.216\t0.20566\r\n41\t0.20015
        public void pasteFromClipboard(DataGridViewCell startCell) {
            try {
                PasteChangeStep step = new PasteChangeStep(this, "Paste from clipboard", this);
                IDataObject iData = Clipboard.GetDataObject();
                string copiedRangeString;
                if ((copiedRangeString = (string)iData.GetData(DataFormats.UnicodeText)) != null) {

                    string[] sseparators = new string[] { "\r\n" };
                    char[] cseparators = new char[] { '\t' };
                    string[] rows = copiedRangeString.Split(sseparators, StringSplitOptions.RemoveEmptyEntries);
                    string[][] cells2 = new string[rows.Length][];
                    for (int r = 0; r < rows.Length; r++)
                        cells2[r] = rows[r].Split(cseparators, StringSplitOptions.RemoveEmptyEntries);
                    
                    int selectedCellRowIndex = startCell.RowIndex; // grid.SelectedCells[0].RowIndex;
                    int selectedCellColumnIndex = startCell.ColumnIndex; // grid.SelectedCells[0].ColumnIndex;

                    int rowId = 0;
                    CellParameterStatus status;
                    while (rowId < cells2.Length && selectedCellRowIndex + rowId < grid.RowCount) {
                        int c = 0;
                        int hiddenCount = 0;
                        while (c < cells2[rowId].Length && selectedCellColumnIndex + c + hiddenCount < grid.ColumnCount) {
                            if (grid[selectedCellColumnIndex + c + hiddenCount, selectedCellRowIndex + rowId].Visible) {
                                if (!grid[selectedCellColumnIndex + c + hiddenCount, selectedCellRowIndex + rowId].ReadOnly) {
                                    step.AddCell(
                                        new Point(selectedCellColumnIndex + c + hiddenCount, selectedCellRowIndex + rowId),
                                        grid[selectedCellColumnIndex + c + hiddenCount, selectedCellRowIndex + rowId].Value);
                                    status = CellParameterStatus.FromString(cells2[rowId][c]);
                                    if (status != null) {
                                        if (grid[selectedCellColumnIndex + c + hiddenCount, selectedCellRowIndex + rowId] is DataGridViewComboBoxCell) {
                                            ((DataGridViewComboBoxCell)grid[selectedCellColumnIndex + c + hiddenCount, selectedCellRowIndex + rowId]).Value = status;
                                        }
                                    } else {
                                        grid[selectedCellColumnIndex + c + hiddenCount, selectedCellRowIndex + rowId].Value = cells2[rowId][c];
                                    }
                                }
                                c++;
                            } else
                                hiddenCount++;
                        }
                        rowId++;
                    }
                    grid.Invalidate();
                }
                _parentProjectForm.AddUndoStep(step);
            } catch (System.Runtime.InteropServices.ExternalException e) {
                MessageBox.Show(e.Message, Application.ProductName, MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        public void pasteFromClipboard() {
            if (grid.SelectedCells.Count > 0)
                this.pasteFromClipboard(grid.SelectedCells[0]);
            else
                this.pasteFromClipboard(grid[grid.FixedCols, 0]);
        }

        public void copyToClipboard(bool addHeaders) {
            if (grid.GetCellCount(DataGridViewElementStates.Selected) > 0) {
                DataObject data = new DataObject();
                if (addHeaders) {
                    string headers = "";
                    bool[] selectedColumns = new bool[grid.ColumnCount];
                    foreach (DataGridViewCell cell in grid.SelectedCells) {
                        selectedColumns[cell.ColumnIndex] = true;
                    }
                    foreach (DataGridViewColumn column in grid.Columns) {
                        if (selectedColumns[column.Index])
                            headers += column.HeaderText + "\t";
                        //if (column.Index < grid.ColumnCount - 1)
                        //    headers += "\t";
                    }
                    headers = headers.Remove(headers.Length - 1, 1);
                    headers = headers.Replace("\0", "");
                    headers = headers.Replace("greek:", "");
                    headers = headers.Replace("sub:", "");
                    headers += "\r\n";
                    data = new DataObject();
                    data.SetText(headers);
                }
                string content = grid.GetClipboardContent().GetText();
                while (content.IndexOf("\t\t") != -1)
                    content = content.Replace("\t\t", "\t");
                data.SetText(data.GetText() + content);
                try {
                    if (data != null)
                        Clipboard.SetDataObject(data);
                } catch (System.Runtime.InteropServices.ExternalException) {
                    MessageBox.Show("Clipboard is not available", Application.ProductName, MessageBoxButtons.OK, MessageBoxIcon.Warning);
                }
            }
        }

        //protected void grid_KeyDown(object sender, KeyEventArgs e) {

        //}

        protected void copyMenuItem_Click(object sender, EventArgs e) {
            copyToClipboard(false);
        }

        protected void pasteMenuItem_Click(object sender, EventArgs e) {
            pasteFromClipboard();
        }

        private void grid_CellEndEdit(object sender, DataGridViewCellEventArgs e) {
            grid.SaveUndoStep();
        }

        private void GroupTabPage_HelpRequested(object sender, HelpEventArgs hlpevent) {
            if (grid.SelectedCells.Count>0)
                if (grid.SelectedCells[0] is DataGridViewComboBoxCell) {
                    Help.ShowHelp(this.Parent, MainForm.helpfile, HelpNavigator.KeywordIndex, "Status");
                    return;
                }
            Help.ShowHelp(this.Parent, MainForm.helpfile, HelpNavigator.KeywordIndex, "Grid");
        }

        private void bindMenuItem_Click(object sender, EventArgs e) {
            //ProjectForm pf = null;
            //try {
            //    pf = (ProjectForm)_parentTabControl.FindForm();
            //    pf.currentBindingParameter = ((DataGridViewParameterCell)grid[mouseCoords.X, mouseCoords.Y]).Parameter;
            //    if (sender == removeExMenuItem) {
            //        pf.currentBindingParameter.Expression = null;
            //        pf.project.ExpressionedParameters.Remove(pf.currentBindingParameter);
            //        grid.Invalidate();
            //    } else {

            //        pf.splitContainer1.Panel2Collapsed = false;
            //        if (pf.currentBindingParameter != null)
            //            if (pf.currentBindingParameter.Expression != null) {
            //                pf.expressionTxt.Text = pf.currentBindingParameter.Expression.ToString();
            //            } else {
            //                pf.expressionTxt.Text = "";
            //            }
            //        pf.expressionTxt.Focus();
            //    }
            //} catch {
            //    if (pf != null)
            //        pf.splitContainer1.Panel2Collapsed = true;
            //}
        }

        private void statusMenuItem_Click(object sender, EventArgs e) {
            int i, p;
            IParameter parameter;
            ParameterStatus status;
            DataGridViewCell cell;
            for (p = 0; p < grid.SelectedCells.Count; p++) {
                if (grid.SelectedCells[p] is DataGridViewParameterCell) {
                    parameter = ((DataGridViewParameterCell)grid.SelectedCells[p]).Parameter;
                    status = (ParameterStatus.Local | ParameterStatus.Common | ParameterStatus.Binding) & parameter.Status;
                    status |= ((ToolStripMenuItem)sender).Checked ? ParameterStatus.Free : ParameterStatus.Fixed;
                    parameter.Status = status;
                    if (parameter.ReferenceGroup > 0 && (status & ParameterStatus.Common) == ParameterStatus.Common) {
                        for (i = 1; i < grid.RowCount; i++)
                            if ((cell = grid[grid.SelectedCells[p].ColumnIndex, i]) is DataGridViewParameterCell && !cell.Selected) {
                                if (((DataGridViewParameterCell)cell).Parameter.ReferenceGroup == parameter.ReferenceGroup)
                                    ((DataGridViewParameterCell)cell).Parameter.Status = parameter.Status;
                            }
                    }
                }
            }
            SetStatuses();
            grid.Invalidate();
        }

        private void formatHeaderCell(ExcelCell cell) {
            cell.Style.FillPattern.SetPattern(FillPatternStyle.Solid, Color.Silver, Color.White);
            cell.Style.HorizontalAlignment = HorizontalAlignmentStyle.Center;
            cell.SetBorders(MultipleBorders.Outside, Color.Black, LineStyle.Thin);
            cell.Style.Font.Weight = ExcelFont.BoldWeight;
        }

        private int setBrighter(int colorComp) {

            return (colorComp + 50 > 255) ? 255 : colorComp + 50;
        }

        private void formatRegularCell(ExcelCell cell, Color color) {
            //Color brightedColor = Color.FromArgb(setBrighter(color.R), setBrighter(color.G), setBrighter(color.B));
            cell.Style.FillPattern.SetPattern(FillPatternStyle.Solid, color, Color.White);
            cell.SetBorders(MultipleBorders.Vertical, Color.Black, LineStyle.Thin);           
        }

        public void fillExcelWorksheet(ExcelWorksheet worksheet, ref int startColumn) {
            int rowId = 1;
            int colId = startColumn;
            int maxColumn = 0;
            bool alternate;
            bool headersFilled = false;
            //bool chisqHeader = false;
            int chisqColumnId = 0;
            int grow, gcol;
            //foreach (DataGridViewRow row in grid.Rows) {
            for (grow = 0; grow < grid.RowCount; grow++) {
                rowId++;
                colId = startColumn;
                alternate = false;
                //foreach (DataGridViewCell cell in row.Cells) {
                for (gcol = 0; gcol < grid.Rows[grow].Cells.Count; gcol++) {
                    if (!grid.Columns[gcol].Visible || grid.Columns[gcol].HeaderText == string.Empty) continue;
                    //header
                    if (!headersFilled) {
                        DefaultGroupGUI.writeFormattedText(grid.Columns[gcol].HeaderText, worksheet.Cells[1, colId]);
                        formatHeaderCell(worksheet.Cells[1, colId]);
                        bool filled = false;
                        if (grid[gcol, 1] is DataGridViewParameterCell)
                            if ((((DataGridViewParameterCell)grid[gcol, 1]).Parameter.Definition.Properties & ParameterProperties.Unsearchable) == 0) {
                                worksheet.Cells[1, colId + 1].Value = "dev";
                                formatHeaderCell(worksheet.Cells[1, colId + 1]);
                                filled = true;
                            }
                        //if (!filled && !chisqHeader) {
                        //    chisqColumnId = colId + 1;
                        //    worksheet.Cells[1, colId + 1].Value = "fit variance";
                        //    formatHeaderCell(worksheet.Cells[1, colId + 1]);
                        //}
                    }
                    //regular cells
                    if (grid[gcol, grow] is DataGridViewParameterCell) {
                        IParameter parameter = ((DataGridViewParameterCell)grid[gcol, grow]).Parameter;
                        worksheet.Cells[rowId, colId].Value = ((DataGridViewParameterCell)grid[gcol, grow]).Value;
                        formatRegularCell(worksheet.Cells[rowId, colId], MainForm.GetColor(parameter.Status));
                        colId++;
                        if ((parameter.Definition.Properties & ParameterProperties.Unsearchable) == 0) {
                        //if ((((DataGridViewParameterCell)cell).Parameter.Status & ParameterStatus.Free) == ParameterStatus.Free) {
                            worksheet.Cells[rowId, colId].Value = ((DataGridViewParameterCell)grid[gcol, grow]).UserError;
                            formatRegularCell(worksheet.Cells[rowId, colId], MainForm.GetColor(parameter.Status));
                            colId++;
                        }
                    } else if (grid[gcol, grow] is DataGridViewTextBoxCell) {

                        worksheet.Cells[rowId, colId].Value = grid[gcol, grow].Value;
                        formatRegularCell(worksheet.Cells[rowId, colId], SystemColors.ButtonFace);
                        colId++;
                        if (colId == chisqColumnId && grid.Rows[grow] is DataGridViewSpectrumRow) {
                            ISpectrum spectrum = ((DataGridViewSpectrumRow)grid.Rows[grow]).Spectrum;
                            worksheet.Cells[rowId, colId].Value = spectrum.Fit;
                            formatRegularCell(worksheet.Cells[rowId, colId], SystemColors.ButtonFace);
                            colId++;
                        } else if (grid[gcol, 1] is DataGridViewParameterCell) {
                            if (((DataGridViewParameterCell)grid[gcol, 1]).Parameter.Definition.BindedStatus == ParameterStatus.None)
                                colId++;
                        }
                    } else if (grid[gcol, grow] is DataGridViewComboBoxCell) {
                            colId += 2;
                    } else {
                        colId++;
                    }
                    //if (!chisqHeader) {
                    //    colId++;
                    //    chisqHeader = true;
                    //}
                    if (maxColumn < colId) maxColumn = colId;
                    alternate = !alternate;
                }
                if (!headersFilled) {
                    headersFilled = true;
                    rowId--;
                }
            }
            //set borders
            worksheet.Cells.GetSubrangeAbsolute(0, startColumn, rowId, maxColumn-1).SetBorders(MultipleBorders.Outside, Color.Black, LineStyle.Medium);
            worksheet.Columns[startColumn].AutoFit();
            //merge document name cell
            CellRange cr = worksheet.Cells.GetSubrangeAbsolute(0, startColumn, 0, maxColumn-1);
            cr.SetBorders(MultipleBorders.Vertical, Color.Black, LineStyle.Thin);
            cr.SetBorders(MultipleBorders.Outside, Color.Black, LineStyle.Medium);
            cr.Merged = true;
            cr.Style.HorizontalAlignment = HorizontalAlignmentStyle.Center;
            cr.Value = GetExcelParametersGroupName();
            cr.Style.Font.Weight = ExcelFont.BoldWeight;
            cr.Style.FillPattern.SetPattern(FillPatternStyle.Solid, Color.Brown, Color.Wheat);
            cr.Style.Font.Color = Color.Wheat;
            startColumn = colId;          
        }

        protected virtual string GetExcelParametersGroupName() {
            return this._spectra[0].Container.Name;
        }

        private void grid_DataError(object sender, DataGridViewDataErrorEventArgs anError) {
            //MessageBox.Show("Error happened " + anError.Context.ToString());

            //if (anError.Context == DataGridViewDataErrorContexts.Commit) {
            //    MessageBox.Show("Commit error");
            //}
            //if (anError.Context == DataGridViewDataErrorContexts.CurrentCellChange) {
            //    MessageBox.Show("Cell change");
            //}
            //if (anError.Context == DataGridViewDataErrorContexts.Parsing) {
            //    MessageBox.Show("parsing error");
            //}
            //if (anError.Context == DataGridViewDataErrorContexts.LeaveControl) {
            //    MessageBox.Show("leave control error");
            //}

            //if ((anError.Exception) is System.Data.ConstraintException) {
                //DataGridParameterView view = (DataGridParameterView)sender;
                //view.Rows[anError.RowIndex].ErrorText = "an error";
                //view.Rows[anError.RowIndex].Cells[anError.ColumnIndex].ErrorText = "an error";
                //view.Undo();
                
                anError.ThrowException = false;
                //view[anError.ColumnIndex, anError.RowIndex].Value = 0;
                //MessageBox.Show("Invalid cell value");
                //throw anError.Exception;
            //}

        }

        private void commonMenuItem_Click(object sender, EventArgs e) {
            IParameter parameter;
            if (commonMenuItem.Checked) {
                for (int i=0; i<grid.SelectedCells.Count; i++)
                    if (grid.SelectedCells[i] is DataGridViewParameterCell) {
                        parameter = ((DataGridViewParameterCell)grid.SelectedCells[i]).Parameter;
                        if ((parameter.Status & ParameterStatus.Binding) == 0) {
                            parameter.ReferenceGroup = 0;
                            parameter.ReferencedParameter = null;
                            parameter.Status = ParameterStatus.Local | ParameterStatus.Free;                            
                        }
                    }
            } else {
                int refGroup = 1;
                int column = grid.SelectedCells[0].ColumnIndex;
                //find first free refGroup id
                bool free = false;
                while (!free) {
                    free = true;
                    for (int row = 1; row < grid.RowCount; row++) {
                        parameter = ((DataGridViewParameterCell)grid[column, row]).Parameter;
                        if (parameter.ReferenceGroup == refGroup) {
                            refGroup++;
                            free = false;
                            break;
                        }
                    }
                }

                for (int i = 0; i < grid.SelectedCells.Count; i++) {
                    parameter = ((DataGridViewParameterCell)grid.SelectedCells[i]).Parameter;
                    if ((parameter.Status & ParameterStatus.Binding) == 0) {
                        parameter.ReferenceGroup = refGroup;
                        parameter.Status = ParameterStatus.Common | ParameterStatus.Free;
                    }
                }
            }
            refreshReferences();
            SetStatuses();
            grid.Invalidate();            
        }

        protected void grid_MouseMove(object sender, MouseEventArgs e) {
            for (int i = 0; i < this._statusStrip.Items.Count; i++)
                this._statusStrip.Items[i].Text = "";
        }

        private void singleFitMenuItem_Click(object sender, EventArgs e) {
            List<ISpectrum> spectrum = new List<ISpectrum>();
            try {
                //spectrum.Add(spectra[mouseCoords.Y - 1]);
                spectrum.Add(((DataGridViewSpectrumRow)grid.Rows[mouseCoords.Y]).Spectrum);
                //    ((SpectraContainerTabPage)Parent.Parent).ProjectForm.FitSingleSpectrum(spectrum);
                _parentProjectForm.FitSingleSpectrum(spectrum);
            } catch (Exception) {
                MessageBox.Show("Not a spectrum!", Application.ProductName, MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
            
        }

        private void extrParameterValueTextBox_TextChanged(object sender, EventArgs e) {
            foreach (DataGridViewCell c in grid.SelectedCells) {
                DataGridViewParameterCell cell = null;
                try {
                    //if (grid[mouseCoords.X, mouseCoords.Y] is DataGridViewParameterCell)
                    if (c is DataGridViewParameterCell)
                        cell = (DataGridViewParameterCell)c;
                } catch (Exception) { }
                if (cell != null) {
                    double value;
                    ToolStripTextBox tb = (ToolStripTextBox)sender;

                    if (tb.Text == "") {
                        if (tb == maxParameterValueTextBox)
                            cell.Parameter.Maximum = double.PositiveInfinity;
                        else if (tb == minParameterValueTextBox)
                            cell.Parameter.Minimum = double.NegativeInfinity;
                        ((ToolStripMenuItem)tb.OwnerItem).Checked = false;
                    } else {
                        if (double.TryParse(tb.Text, out value)) {
                            if (cell.ConvertFromUserValue != null)
                                cell.ConvertFromUserValue(cell.Parameter, ref value);
                            if (tb == maxParameterValueTextBox)
                                cell.Parameter.Maximum = value;
                            else if (tb == minParameterValueTextBox)
                                cell.Parameter.Minimum = value;
                            ((ToolStripMenuItem)tb.OwnerItem).Checked = true;
                        } else {
                            MessageBox.Show("It is not a valid float number", Application.ProductName, MessageBoxButtons.OK, MessageBoxIcon.Warning);
                            grid.Invalidate();
                            break;
                        }
                    }
                    grid.InvalidateCell(mouseCoords.X, mouseCoords.Y);
                }
            }
        }

        private void extremumMenuItem_CheckedChanged(object sender, EventArgs e) {
            ToolStripMenuItem ts = (ToolStripMenuItem)sender;
            if (ts.Checked) {
                ((ToolStripTextBox)ts.DropDownItems[0]).Text = "";
            }
        }

        private void extremumMenuItem_Click(object sender, EventArgs e) {
            ToolStripMenuItem ts = (ToolStripMenuItem)sender;
            if (ts.Checked) {
                ((ToolStripTextBox)ts.DropDownItems[0]).Text = "";
            }
        }

        public GroupDefinition GroupDefinition {
            get { return this.groupDefinition; }
            set { 
                this.groupDefinition = value;
                this.groupGUI = AvailableGUIAssemblies.GetGroupGUI(_spectra[0].Container.ParentProject.GetType(), this.grid, this._spectra, this.groupDefinition, this);
                BuildTools();
                Reset();
            }
        }

        //private void grid_KeyUp(object sender, KeyEventArgs e) {
        //    //keys = e.KeyData;
        //}

    }

}