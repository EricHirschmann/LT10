using System;
using System.Collections.Generic;
using System.Text;
using Evel.gui.interfaces;
using Evel.interfaces;
using System.Windows.Forms;
using System.Drawing;
using System.Globalization;
using System.Text.RegularExpressions;
using GemBox.Spreadsheet;

namespace Evel.gui {

    public class DefaultGroupGUI : IGroupGUI {

        public enum StringFormatTarget { Html, ParameterDataGrid };

        //private static Dictionary<string, char> _greekLeters;
        private static List<CellParameterStatus> _statusesSource;

        //public static Regex parameterHeaderRegex = new Regex(@"\[p(?:\s\w+='[\w\d\s\.\-\=\:\,\(\)]+')+\]", RegexOptions.Compiled);
        public static Regex parameterHeaderRegex = new Regex(@"\[p(?:\s\w+='[^']+')+\]", RegexOptions.Compiled);
        public static Regex attributeRegex = new Regex(@"(?<name>\w+)='(?<value>[^']+)'", RegexOptions.Compiled);
        //public static Regex attributeRegex = new Regex(@"(?<name>\w+)='(?<value>[\w\d\s\.\-\=\:\,\(\)]+)'", RegexOptions.Compiled);

        protected int _fixedColCount = int.MinValue;
        protected GroupDefinition groupDefinition;
        //protected ISpectraContainer container;
        protected List<ISpectrum> spectra;
        protected DataGridView gridView;
        //public List<Comparer<ISpectrum>> comparers;
        protected List<ToolBox> _toolBoxes;
        protected NumberFormatInfo numberFormat;
        protected IProject parentProject;
        protected GroupTabPage groupTabPage;

        public DefaultGroupGUI(DataGridView gridView, List<ISpectrum> spectra, GroupDefinition groupDefinition, GroupTabPage groupTabPage) {
            this.gridView = gridView;
            this.spectra = spectra;
            this.groupDefinition = groupDefinition;
            //this.comparers = new List<Comparer<ISpectrum>>();
            this.numberFormat = new CultureInfo("en-US", false).NumberFormat;
            this.numberFormat.NumberDecimalSeparator = ".";
            this.parentProject = spectra[0].Container.ParentProject;
            this.groupTabPage = groupTabPage;
        }

        public DefaultGroupGUI() { }

        #region IGroupGUI Members

        public GroupDefinition GroupDefinition {
            get { return this.groupDefinition; }
        }

        public virtual int FixedColCount {
            get {
                if (_fixedColCount == int.MinValue) {
                    _fixedColCount = 2;
                    for (int i=1; i<spectra.Count; i++)
                        if (spectra[i - 1].Container != spectra[i].Container) {
                            _fixedColCount++;
                            break;
                        }
                }
                return _fixedColCount; 
            }
        }

        public virtual int GetColumnCount() {
            int compCount = spectra[0].Parameters[groupDefinition.name].Components.Size;
            int uniqueParameterCount = spectra[0].Parameters[groupDefinition.name].GroupUniqueParameters.Size;
            int nonUniqueParameterCount = groupDefinition.parameters.Length - uniqueParameterCount;
            int result = compCount * nonUniqueParameterCount + FixedColCount + 1; //1 - chisq
            if (result>=FixedColCount)
                result += uniqueParameterCount;
            if (HasGraphicAdjustment)
                result += 2;
            return result;
        }

        public virtual DataGridViewRow CreateControlersRow() {
            DataGridViewRow result = new DataGridViewRow();
            //i <= FixedColCount: fixed cols + chisq column
            for (int i = 0; i <= FixedColCount; i++) {
                DataGridViewTextBoxCell cell = new DataGridViewTextBoxCell();
                cell.Style.BackColor = System.Drawing.SystemColors.ControlDark;
                cell.Style.SelectionBackColor = cell.Style.BackColor;
                result.Cells.Add(cell);
            }
            int colId = FixedColCount;
            int s, c, p;
            IParameter parameter;
            ParameterStatus status;
            for (c = 0; c < spectra[0].Parameters[groupDefinition.name].Components.Size; c++) {
                //foreach (Evel.interfaces.IComponent component in spectra[0].Parameters[groupDefinition.name].Components) {
                //parId = 0;
                //foreach (IParameter parameter in component) {
                for (p = 0; p < spectra[0].Parameters[groupDefinition.name].Components[c].Size; p++) {
                    //parameter = spectra[0].Parameters[groupDefinition.name].Components[c][p];
                    status = ParameterStatus.Binding;
                    for (s = 0; s < spectra.Count && status == ParameterStatus.Binding; s++)
                        status &= spectra[s].Parameters[groupDefinition.name].Components[c][p].Status;

                    //if (parameter.Definition.BindedStatus != ParameterStatus.None || parameter.BindingParameter) {
                    if (spectra[0].Parameters[groupDefinition.name].Components[c][p].Definition.BindedStatus != ParameterStatus.None
                        || status == ParameterStatus.Binding || 
                        ((spectra[0].Parameters[groupDefinition.name].Components[c][p].Definition.Properties & ParameterProperties.ComponentIntensity) > 0 && c==0))
                        result.Cells.Add(CreateDisabledControler());
                    else
                        result.Cells.Add(CreateStatusControler());
                    colId++;
                }
            }
            //foreach (parameter in spectra[0].Parameters[groupDefinition.name].GroupUniqueParameters) {
            for (p = 0; p < spectra[0].Parameters[groupDefinition.name].GroupUniqueParameters.Size; p++) {
                parameter = spectra[0].Parameters[groupDefinition.name].GroupUniqueParameters[p];
                if (parameter.Definition.BindedStatus != ParameterStatus.None) {
                    result.Cells.Add(CreateDisabledControler());
                } else {
                    result.Cells.Add(CreateStatusControler());
                }
                colId++;
            }
            for (c = colId; c < Grid.ColumnCount - ((HasGraphicAdjustment) ? 3 : 1); c++) {
                result.Cells.Add(CreateStatusControler());
            }
            if (HasGraphicAdjustment) {
                result.Cells.Add(CreateDisabledControler());
                result.Cells.Add(CreateDisabledControler());
            }
            return result;
        }

        protected DataGridViewTextBoxCell CreateDisabledControler() {
            DataGridViewTextBoxCell cell = new DataGridViewTextBoxCell();
            cell.Style.BackColor = System.Drawing.SystemColors.ControlDark;
            cell.Style.SelectionBackColor = cell.Style.BackColor;
            return cell;
        }

        protected DataGridViewComboBoxCell CreateStatusControler() {
            DataGridViewComboBoxCell cell = new DataGridViewComboBoxCell();
            
            cell.DataSource = StatusesSource;
            cell.Style.Font = new System.Drawing.Font("Arial Narrow", 8, FontStyle.Bold);
            cell.Style.Alignment = DataGridViewContentAlignment.MiddleCenter;
            cell.ValueMember = "ValueMember";
            cell.DisplayMember = "DisplayMember";
            cell.Sorted = false;
            cell.Style.BackColor = System.Drawing.SystemColors.ControlDark;
            cell.Style.SelectionBackColor = cell.Style.BackColor;
            return cell;
        }
        
        public virtual void SetHeaders() {
            int colId = 0;
            //if (comparers.Count == 0) {
            //    comparers.Add(new SpectrumNameComparer(gridView, 0)); // new SpectrumNameComparer(gridView.Columns[0]));
            //    comparers.Add(new SpectrumKeyComparer(gridView, 1));
            //}
            gridView.Columns[colId].HeaderText = "[p text='Spectrum']";
            gridView.Columns[colId].HeaderCell.ToolTipText = "";
            //gridView.Columns[colId].SortMode = DataGridViewColumnSortMode.Programmatic;
            gridView.Columns[colId].AutoSizeMode = DataGridViewAutoSizeColumnMode.AllCellsExceptHeader;
            gridView.Columns[colId].MinimumWidth = 80;
            gridView.Columns[colId].HeaderCell.ToolTipText = "Spectrum name";
            colId++;

            gridView.Columns[colId].HeaderText = "[p text='Key value']";
            //gridView.Columns[colId].SortMode = DataGridViewColumnSortMode.Programmatic;
            gridView.Columns[colId].AutoSizeMode = DataGridViewAutoSizeColumnMode.AllCellsExceptHeader;
            gridView.Columns[colId].MinimumWidth = 80;
            gridView.Columns[colId].HeaderCell.ToolTipText = "Key value";
            colId++;

            if (FixedColCount == 3) {
                gridView.Columns[colId].HeaderText = "[p text='Model']";
                gridView.Columns[colId].SortMode = DataGridViewColumnSortMode.NotSortable;
                gridView.Columns[colId].AutoSizeMode = DataGridViewAutoSizeColumnMode.AllCellsExceptHeader;
                gridView.Columns[colId].MinimumWidth = 60;
                gridView.Columns[colId].HeaderCell.ToolTipText = "Model";
                colId++;
            }

            gridView.Columns[colId].HeaderText = "[p text='Fit']";
            gridView.Columns[colId].AutoSizeMode = DataGridViewAutoSizeColumnMode.AllCellsExceptHeader;
            gridView.Columns[colId].MinimumWidth = 50;
            gridView.Columns[colId].HeaderCell.ToolTipText = "Fit variance";
            colId++;

            int compId = 1;
            int parId;
            //int colId = FixedColCount;
            foreach (Evel.interfaces.IComponent component in spectra[0].Parameters[groupDefinition.name].Components) {
                parId = 1;
                foreach (IParameter parameter in component) {
                    if (groupDefinition.componentCount == 1)
                        gridView.Columns[colId].HeaderText = BuildFormatedString(parameter.Definition.Header, StringFormatTarget.ParameterDataGrid);
                    else
                        gridView.Columns[colId].HeaderText = BuildFormatedString(parameter.Definition.Header, StringFormatTarget.ParameterDataGrid, compId.ToString());
                    gridView.Columns[colId].HeaderCell.ToolTipText = String.Format("{0}{1}", parameter.Definition.Name, (groupDefinition.componentCount == 1) ? "" : compId.ToString());
                    colId++;
                    parId++;
                }
                compId++;
            }
            foreach (IParameter parameter in spectra[0].Parameters[groupDefinition.name].GroupUniqueParameters) {
                //gridView.Columns[colId].HeaderText = BuildFormatedString(parameter.Definition.Name, -1, StringFormatTarget.ParameterDataGrid);
                gridView.Columns[colId].HeaderText = BuildFormatedString(parameter.Definition.Header, StringFormatTarget.ParameterDataGrid);
                gridView.Columns[colId].HeaderCell.ToolTipText = parameter.Definition.Name;
                colId++;
            }

            if (HasGraphicAdjustment) {
                colId += 1;
                gridView.Columns[colId].HeaderText = "[p text='Statistic']";
                gridView.Columns[colId].ToolTipText = "Statistic";
            }

            //int parametersCount = groupDefinition.parameters.Length;
            //for (int colId = FixedColCount; colId < gridView.ColumnCount; colId++) {
            //    int parId = (colId - FixedColCount) % parametersCount;
            //    int compId = (int)Math.Floor((double)(colId - FixedColCount) / parametersCount);
            //    if (groupDefinition.fixedComponentCount)
            //        gridView.Columns[colId].HeaderText = BuildFormatedString(groupDefinition.parameters[parId].Name, -1);
            //    else
            //        gridView.Columns[colId].HeaderText = BuildFormatedString(groupDefinition.parameters[parId].Name, compId + 1);
            //}
        }

        public virtual DataGridViewSpectrumRow CreateSpectrumRow(ISpectrum spectrum) {
            DataGridViewSpectrumRow spectrumRow = new DataGridViewSpectrumRow(spectrum);
            DataGridViewTextBoxCell spectrumcell = new DataGridViewTextBoxCell();
            spectrumcell.Value = spectrum.Name;
            spectrumcell.Style.BackColor = System.Drawing.SystemColors.Control;
            //spectrumcell.ReadOnly = true;
            spectrumRow.Cells.Add(spectrumcell);
         
            //spectrumcell = new DataGridViewTextBoxCell();
            DataGridViewParameterCell keyValueCell = CreateParameterCell(spectrum.Parameters[0].Components[0]["key value"]);
            keyValueCell.Style.BackColor = System.Drawing.SystemColors.Control;
            spectrumRow.Cells.Add(keyValueCell);

            if (FixedColCount == 3) {
                //dodatkowa kolumna z nazwa contenera
                spectrumcell = new DataGridViewTextBoxCell();
                spectrumcell.Value = spectrum.Container.Name;
                spectrumcell.Style.BackColor = System.Drawing.SystemColors.Control;
                spectrumRow.Cells.Add(spectrumcell);
            }

            spectrumcell = new DataGridViewCustomValueCell(delegate(ISpectrum s) {
                if (double.IsInfinity(s.Fit) || double.IsNaN(s.Fit))
                    return " ";
                else
                    return s.Fit.ToString("F4");
            });
            spectrumcell.Style.BackColor = System.Drawing.SystemColors.Control;
            spectrumRow.Cells.Add(spectrumcell);

            //spectrumcell.ReadOnly = true;
            foreach (IComponent component in spectrum.Parameters[groupDefinition.name].Components) {
                foreach (IParameter parameter in component) {
                    //if (!parameter.Definition.Visible) continue;
                    spectrumRow.Cells.Add(CreateParameterCell(parameter));
                }
            }
            foreach (IParameter parameter in spectrum.Parameters[groupDefinition.name].GroupUniqueParameters) {
                spectrumRow.Cells.Add(CreateParameterCell(parameter));
            }
            if (HasGraphicAdjustment) {
                DataGridViewButtonCell adjustButton = new DataGridViewButtonCell();
                adjustButton.Value = "adjust...";
                adjustButton.Style.Font = new Font("Arial", gridView.Font.Size);
                adjustButton.Style.Alignment = DataGridViewContentAlignment.MiddleCenter;
                adjustButton.Style.ForeColor = System.Drawing.Color.Black;
                adjustButton.Style.SelectionForeColor = adjustButton.Style.ForeColor;
                adjustButton.Style.BackColor = System.Drawing.SystemColors.ButtonFace;
                adjustButton.Style.SelectionBackColor = adjustButton.Style.BackColor;
                adjustButton.FlatStyle = FlatStyle.Popup;
                adjustButton.ToolTipText = "";
                spectrumRow.Cells.Add(adjustButton);

                spectrumcell = new DataGridViewCustomValueCell(delegate(ISpectrum s) {
                    return String.Format("{0:F3} mln", s.Statistic/1e6);
                });
                spectrumcell.Style.BackColor = System.Drawing.SystemColors.Control;
                spectrumRow.Cells.Add(spectrumcell);
            }
            return spectrumRow;
        }

        public virtual DataGridViewParameterCell CreateParameterCell(IParameter parameter) {
            DataGridViewParameterCell cell = new DataGridViewParameterCell(parameter);
            //cell.UserValue = parameter.Value;
            if (parameter.Definition.Name.ToLower().Contains("int")) {
                cell.ConvertToUserValue += ConvertToUserIntensity;
                cell.ConvertFromUserValue += ConvertFromUserIntensity;
            }
            return cell;
        }

        public DataGridView Grid {
            get { return this.gridView; }
        }

        public virtual Type ProjectType {
            get { return null; }
        }

        public void GridCellValueChange(object sender, DataGridViewCellEventArgs e) {
            if (gridView[e.ColumnIndex, e.RowIndex] is DataGridViewParameterCell) {
                IParameter parameter = ((DataGridViewParameterCell)gridView[e.ColumnIndex, e.RowIndex]).Parameter;
                if (parameter.Definition.Name == "int") {
                    double sum = parameter.Value;
                    IComponents components = spectra[e.RowIndex - 1].Parameters[groupDefinition.name].Components;
                    foreach (IComponent component in components) {
                        if (component[0] != parameter && component != components[0])
                            sum += component[0].Value;
                    }
                    components[0][0].Value = 1 - sum;
                }
            }
        }

        public virtual bool IsCellReadOnly(DataGridViewCell cell) {
            if (spectra[0].Container.ParentProject.IsBusy)
                return true;
            if (cell is DataGridViewParameterCell) {
                IParameter parameter = ((DataGridViewParameterCell)cell).Parameter;
                //if (parameter.Expression != null)
                //    return true;
                //IGroup group = container.Spectra[cell.RowIndex - 1].Parameters[groupDefinition.name];
                if (!(cell.OwningRow is DataGridViewSpectrumRow)) return true;
                IGroup group = ((DataGridViewSpectrumRow)cell.OwningRow).Spectrum.Parameters[groupDefinition.name];
                if ((parameter.Definition.Properties & ParameterProperties.Readonly) == ParameterProperties.Readonly)
                    return true;
                //if intensity of first component then readonly
                if ((parameter.Definition.Properties & ParameterProperties.ComponentIntensity) > 0 &&
                    ((parameter.Status & ParameterStatus.Free) == ParameterStatus.Free) &&
                    (parameter.Parent == group.Components[0] || (spectra[0].Container.ParentProject.CalculatedValues && groupDefinition.name != "prompt")))
                    return true;
                if ((parameter.HasReferenceValue && !(parameter.BindingParameter && cell.RowIndex == 1)) ||
                    (cell.RowIndex != 1 && spectra[0].Container.ParentProject.CalculatedValues && ((parameter.Status & (ParameterStatus.Local | ParameterStatus.Free)) == (ParameterStatus.Local | ParameterStatus.Free))))
                    return true;
            } else if (cell is DataGridViewCustomValueCell) {
                return true;
            } else if (cell is DataGridViewComboBoxCell) {
                DataGridParameterView grid = (DataGridParameterView)cell.DataGridView;
                int compId = (int)Math.Floor((double)(cell.ColumnIndex - grid.FixedCols) / groupDefinition.parameters.Length);
                int parId = (cell.ColumnIndex - grid.FixedCols) % groupDefinition.parameters.Length;
                if (grid[cell.ColumnIndex, cell.RowIndex + 1] is DataGridViewParameterCell)
                    return (compId == 0 && parId == 0 && ((DataGridViewParameterCell)grid[cell.ColumnIndex, cell.RowIndex + 1]).Parameter.Definition.Name.ToLower().Contains("int"));


                //if (compId == 0 && parId == 0 && spectra[0].Container.Spectra[0].Parameters[groupDefinition.name].Components[compId][parId].Definition.Name == "int")
                //    return true;
            } else if (cell is DataGridViewTextBoxCell)
                return true;
            return false;
        }

        //public void Sort(TabPage tabPage, int columnId, SortOrder order) {
        //    Comparison<ISpectrum> nameComparison = delegate(ISpectrum s1, ISpectrum s2) {
        //        switch (order) {
        //            case SortOrder.Ascending: return s1.Name.CompareTo(s2.Name);
        //            case SortOrder.Descending: return s2.Name.CompareTo(s1.Name);
        //            default: return 0;
        //        }
        //    };
        //    Comparison<ISpectrum> keyComparison = delegate(ISpectrum s1, ISpectrum s2) {
        //        switch (order) {
        //            case SortOrder.Ascending: return s1.Parameters[0].Components[0]["key value"].Value.CompareTo(s2.Parameters[0].Components[0]["key value"].Value);
        //            case SortOrder.Descending: return s2.Parameters[0].Components[0]["key value"].Value.CompareTo(s1.Parameters[0].Components[0]["key value"].Value);
        //            default: return 0;
        //        }
        //    };
        //    Comparison<ISpectrum> comp;


        //    int i;
        //    GroupTabPage gt = (GroupTabPage)tabPage;
        //    List<ISpectraContainer> containers = new List<ISpectraContainer>();
        //    for (i = 0; i < gt.spectra.Count; i++)
        //        if (!containers.Contains(gt.spectra[i].Container))
        //            containers.Add(gt.spectra[i].Container);
        //    DataGridViewColumn column = gt.grid.Columns[columnId];
        //    column.HeaderCell.SortGlyphDirection = order;
        //    if (columnId == 0)
        //        comp = nameComparison;
        //    else
        //        comp = keyComparison;
        //    for (i = 0; i < containers.Count; i++)
        //        containers[i].Spectra.Sort(comp);
        //    //if (containers.Count > 1)
        //    this.spectra.Sort(comp);
        //    //if (comparers.Count > columnId) {
        //    //    if (comparers[columnId] != null) {
        //    //        //throw new NotImplementedException("sortowanie jest inne jeśli tabela zawiera parametry widm należących do różnych modeli");
        //    //        for (i=0; i<containers.Count; i++)
        //    //            containers[i].Spectra.Sort(comparers[columnId]);
        //    //    }
        //    //}
        //}

        public virtual Form CreateValuesAdjuster(List<ISpectrum> spectra, ISpectrum adjustingSpectrum) {
            return null;
        }

        public virtual bool HasGraphicAdjustment {
            get { return false; }
        }

        /// <summary>
        /// when overriden in deriving class returns default value of passed parameter. 
        /// </summary>
        /// <param name="spectrum"></param>
        /// <param name="parameter"></param>
        /// <returns></returns>
        public virtual double getDefaultParameterValue(ISpectrum spectrum, IParameter parameter) {
            return 1;
        }

        public virtual void CellFormatting(Object sender, DataGridViewCellFormattingEventArgs e) {
            DataGridParameterView grid = (DataGridParameterView)sender;
            if (e.Value != null) {
                if (grid[e.ColumnIndex, e.RowIndex] is DataGridViewParameterCell) {
                    IParameter parameter = ((DataGridViewParameterCell)grid[e.ColumnIndex, e.RowIndex]).Parameter;
                    if ((parameter.HasReferenceValue && !(parameter.BindingParameter && e.RowIndex == 1)) || (e.RowIndex != 1 && spectra[0].Container.ParentProject.CalculatedValues &&
                        ((parameter.Status & (ParameterStatus.Local | ParameterStatus.Free)) == (ParameterStatus.Local | ParameterStatus.Free)))
                        || (e.RowIndex == 1 && (parameter.Definition.Properties & ParameterProperties.ComponentIntensity) > 0 && spectra[0].Container.ParentProject.CalculatedValues && (parameter.Status & ParameterStatus.Free) == ParameterStatus.Free)) {
                        e.Value = "-";
                        grid[e.ColumnIndex, e.RowIndex].Style.Alignment = DataGridViewContentAlignment.MiddleCenter;
                    } else {
                        DataGridViewParameterCell cell = (DataGridViewParameterCell)grid[e.ColumnIndex, e.RowIndex];
                        if (cell.UserError > (double)cell.Value || cell.UserError == 0 || double.IsNaN(cell.UserError) || double.IsInfinity(cell.UserError))
                            e.Value = ((double)cell.Value).ToString("G06", numberFormat);
                        else if (cell.UserError > 0 && cell.UserError < 1) {
                            e.Value = ((double)cell.Value).ToString(String.Format("F{0}", Math.Ceiling(Math.Abs(Math.Log10(cell.UserError))), numberFormat));
                        } else //if (cell.UserError > 1) {
                            e.Value = ((double)cell.Value).ToString("F0", numberFormat);
                        //}
                        grid[e.ColumnIndex, e.RowIndex].Style.Alignment = DataGridViewContentAlignment.MiddleLeft;
                    }
                } else if (grid[e.ColumnIndex, e.RowIndex] is DataGridViewCustomValueCell) {
                    //e.Value = e.Value.ToString("F3");
                    grid[e.ColumnIndex, e.RowIndex].Style.Alignment = DataGridViewContentAlignment.MiddleCenter;
                } else {
                    if ((e.RowIndex == 0) && (e.ColumnIndex >= grid.FixedCols)) {
                        if (e.Value is CellParameterStatus)
                            e.CellStyle.ForeColor = ((CellParameterStatus)e.Value).ValueColor;
                    } else {
                        e.Value = e.Value.ToString();
                    }
                }              
                e.FormattingApplied = true;
            }
            if ((grid[e.ColumnIndex, e.RowIndex].ReadOnly = IsCellReadOnly(grid[e.ColumnIndex, e.RowIndex])) && e.ColumnIndex >= grid.FixedCols)
                e.CellStyle.ForeColor = Color.Black;
        }

        #endregion

        protected void ConvertFromUserIntensity(IParameter parameter, ref double value) {
            value /= 100;
        }

        protected void ConvertToUserIntensity(IParameter parameter, ref double value) {
            value *= 100;
        }

        //private static void SetGreekLeters() {
        //    _greekLeters = new Dictionary<string, char>();
        //    _greekLeters.Add("alpha", 'a');
        //    _greekLeters.Add("beta", 'b');
        //    _greekLeters.Add("gamma", 'g');
        //    _greekLeters.Add("delta", 'd');
        //    _greekLeters.Add("epsilon", 'e');
        //    _greekLeters.Add("zeta", 'z');
        //    _greekLeters.Add("eta", 'h');
        //    _greekLeters.Add("theta", 'q');
        //    _greekLeters.Add("iota", 'i');
        //    _greekLeters.Add("kappa", 'k');
        //    _greekLeters.Add("lambda", 'l');
        //    _greekLeters.Add("mu", 'm');
        //    _greekLeters.Add("nu", 'n');
        //    _greekLeters.Add("xi", 'x');
        //    _greekLeters.Add("omicron", 'o');
        //    _greekLeters.Add("pi", 'p');
        //    _greekLeters.Add("rho", 'r');
        //    _greekLeters.Add("sigma", 's');
        //    _greekLeters.Add("tau", 't');
        //    _greekLeters.Add("upsilon", 'u');
        //    _greekLeters.Add("phi", 'f');
        //    _greekLeters.Add("chi", 'c');
        //    _greekLeters.Add("psi", 'y');
        //    _greekLeters.Add("omega", 'w');
        //}

        //public static Dictionary<string, char> GreekLeters {
        //    get {
        //        if (_greekLeters == null)
        //            SetGreekLeters();
        //        return _greekLeters;
        //    }
        //}

        private static void SetStatusesSource() {
            _statusesSource = new List<CellParameterStatus>();
            _statusesSource.Add(new CellParameterStatus(ParameterStatus.Local | ParameterStatus.Free));
            _statusesSource.Add(new CellParameterStatus(ParameterStatus.Local | ParameterStatus.Fixed));
            _statusesSource.Add(new CellParameterStatus(ParameterStatus.Common | ParameterStatus.Free));
            _statusesSource.Add(new CellParameterStatus(ParameterStatus.Common | ParameterStatus.Fixed));
            _statusesSource.Add(new CellParameterStatus(ParameterStatus.None));
        }

        public static List<CellParameterStatus> StatusesSource {
            get {
                if (_statusesSource == null)
                    SetStatusesSource();
                return _statusesSource;
            }
        }

        //public static string getParameterInfo(IParameter parameter, out int compId) {
        //    if (parameter.Parent is Evel.engine.ContributedGroup) {
        //        compId = -1;
        //        return ((Evel.engine.ContributedGroup)parameter.Parent).Definition.name;
        //    } else {
        //        IComponent comp = (IComponent)parameter.Parent;
        //        IComponents comps = (IComponents)comp.Parent;
        //        compId = (comps.Size > 1) ? comps.IndexOf(comp) + 1 : -1;
        //        return comps.Parent.Definition.name;
        //    }
        //}

        //public static void getParameterInfo(IParameter parameter, out string doc, out string group, out string compId) {
        //    if (parameter.Parent is Evel.engine.ContributedGroup) {
        //        compId = "-1";
        //        IGroup gr = (Evel.engine.ContributedGroup)parameter.Parent;
        //        group = gr.Definition.name;
        //        doc = gr.OwningSpectrum.Container.Name;
        //    } else {
        //        IComponent comp = (IComponent)parameter.Parent;
        //        IComponents comps = (IComponents)comp.Parent;
        //        compId = ((comps.Size > 1) ? comps.IndexOf(comp) + 1 : -1).ToString();
        //        group = comps.Parent.Definition.name;
        //        doc = comps.Parent.OwningSpectrum.Container.Name;
        //    }
        //}

        //public static string BuildFormatedString(string header, int compId, StringFormatTarget format) {
        //    return BuildFormatedString(header, compId, null, format);
        //}

        //public static string BuildFormatedString2(string header, int compId, string info, StringFormatTarget format) {
        //    StringBuilder builder = new StringBuilder(header);
        //    if (compId != -1)
        //        builder.AppendFormat("[p text='{0}' index='sub']", compId);
        //    builder.AppendFormat("[p text='{0}' index='sub']", info);
        //    return builder.ToString();
        //}

        public static string ConvertHeaderToHTML(string header) {
            StringBuilder builder = new StringBuilder();
            string text, font, index;
            foreach (Match outmatch in parameterHeaderRegex.Matches(header)) {
                text = font = index = null;
                foreach (Match attrMatch in attributeRegex.Matches(outmatch.Value)) {
                    switch (attrMatch.Groups["name"].Value) {
                        case "index": index = attrMatch.Groups["value"].Value; break;
                        case "font": font = attrMatch.Groups["value"].Value; break;
                        case "text": text = attrMatch.Groups["value"].Value; break;
                    }
                }
                if (index != null) {
                    builder.AppendFormat("<{0}{1}>{2}</{0}>",
                        index,
                        (font != null) ? String.Format(" style=\" font-family: {0};\"", font) : "",
                        text);
                } else {
                    if (font != null) {
                        builder.AppendFormat("<span style=\"font-family: {0}\">{1}</span>", font, text);
                    } else
                        builder.Append(text);
                }
            }
            return builder.ToString();
        }

        public static string BuildFormatedString(string header, StringFormatTarget format, params string[] indexedInfo) {
            StringBuilder builder = new StringBuilder();
            if (Regex.IsMatch(header, @"^\[p text='.+'\s*\]$") || format == StringFormatTarget.Html)
                builder.Append(header);
            else
                builder.AppendFormat("[p text='{0}']", header);
            if (format == StringFormatTarget.Html)
                builder.Append("<sub>");
            foreach (string o in indexedInfo) {
                if (o != "" && o != "-1" && o != "0") {
                    switch (format) {
                        case StringFormatTarget.ParameterDataGrid:
                            builder.AppendFormat("[p text='{0}' index='sub']", o); break;

                        case StringFormatTarget.Html:
                            builder.AppendFormat("{0} &nbsp;", o); break;
                    }
                }
            }
            if (format == StringFormatTarget.Html)
                builder.Append("</sub>");
            return builder.ToString();
        }

        //public static string BuildFormatedString(string header, int compId, string info, StringFormatTarget format) {
        //    string result = header;
        //    foreach (string gl in GreekLeters.Keys) {
        //        int index; // = result.IndexOf(gl);
        //        int safetyId = 0;
        //        while ((index = result.ToLower().IndexOf(gl)) != -1) {
        //            if (result[index] == gl[0]) {
        //                if (format == StringFormatTarget.ParameterDataGrid)
        //                    result = result.Replace(gl, String.Format("\0greek:{0}\0", GreekLeters[gl].ToString()));
        //                else
        //                    result = result.Replace(gl, String.Format("<span style=\"font-family: symbol;\">{0}</span>", GreekLeters[gl].ToString()));
        //            } else {
        //                if (format == StringFormatTarget.ParameterDataGrid)
        //                    result = result.Replace(gl.ToUpper(), String.Format("\0greek:{0}\0", GreekLeters[gl].ToString().ToUpper()));
        //                else
        //                    result = result.Replace(gl, String.Format("<span style=\"font-family: symbol;\">{0}</span>", GreekLeters[gl].ToString().ToUpper()));
        //            }
        //            safetyId++;
        //            if (safetyId > result.Length)
        //                break;
        //        }
        //    }
        //    //string[] splits = result.Split(new char[] { '\0', ',', '.' }); //, StringSplitOptions.RemoveEmptyEntries);
        //    if (format == StringFormatTarget.ParameterDataGrid) {
        //        if (result[0] != '\0')
        //            result = '\0' + result;
        //        if (result[result.Length - 1] != '\0')
        //            result += "\0";
        //        if (compId != -1)
        //            result += String.Format("sub:{0}\0", compId.ToString());
        //        if (info != null)
        //            result += String.Format("sub:{0}\0", info);
        //    } else {
        //        if (compId != -1)
        //            result += String.Format("<sub>{0}</sub>", compId.ToString());
        //        if (info != null)
        //            result += String.Format("<sub>{0}</sub>", info);
        //    }
        //    return result;
        //}

        public static void DrawHeaderContent(string s, Graphics g, Font font, Rectangle rect, bool center, Brush brush) {
            try {
                Bitmap bmap = new Bitmap(rect.Width, rect.Height);

                Graphics img = Graphics.FromImage(bmap);
                //g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.HighQuality;
                float xoffset = 0;
                float yoffset;
                string text;
                Font tmpFont;
                bool isIndex;
                int xoffsetStep = 0;
                foreach (Match outmatch in parameterHeaderRegex.Matches(s)) {
                    isIndex = false;
                    yoffset = 0.5f * font.Size;
                    tmpFont = null;
                    text = "";
                    foreach (Match attrMatch in attributeRegex.Matches(outmatch.Value)) {
                        switch (attrMatch.Groups["name"].Value) {
                            case "index":
                                if (attrMatch.Groups["value"].Value == "sub") yoffset = 1.1f * font.Size;
                                else yoffset = 0;
                                isIndex = true;
                                break;
                            //case "font": tmpFont = new Font(attrMatch.Groups["value"].Value, (isIndex) ? font.Size : font.Size, font.Style); break;
                            case "font": tmpFont = new Font(attrMatch.Groups["value"].Value, (isIndex) ? font.Size - 1.0f : font.Size, font.Style); break;
                            case "text": text = attrMatch.Groups["value"].Value; break;
                        }
                    }
                    if (tmpFont == null) {
                        if (isIndex)
                            tmpFont = new Font(font.FontFamily, font.Size - 1.0f, font.Style);
                        else
                            tmpFont = new Font(font, font.Style);
                    }
                    img.TextRenderingHint = System.Drawing.Text.TextRenderingHint.SingleBitPerPixelGridFit;
                    img.DrawString(text, tmpFont, brush, xoffset, yoffset);
                    xoffset += img.MeasureString(text, tmpFont).Width + xoffsetStep;
                    xoffsetStep = 10;
                }
                SizeF size = new SizeF(xoffset + 3, 3 * font.Size);
                if (center)
                    g.DrawImageUnscaled(bmap, new Rectangle((int)(rect.Left + (rect.Width - size.Width) / 2), (int)(rect.Top + (rect.Height - size.Height) / 2), (int)size.Width, (int)size.Height));
                else
                    g.DrawImageUnscaled(bmap, new Rectangle(rect.Left + 10, rect.Top + (int)((rect.Height - size.Height) / 2), (int)size.Width, (int)size.Height));
            } catch { }
        }

        public static void writeFormattedText(string text, ExcelCell cell) {
            StringBuilder sb = new StringBuilder();
            foreach (System.Text.RegularExpressions.Match pMatch in parameterHeaderRegex.Matches(text)) {
                foreach (System.Text.RegularExpressions.Match aMatch in attributeRegex.Matches(pMatch.Value)) {
                    switch (aMatch.Groups["name"].Value) {
                        case "text":
                            sb.Append(aMatch.Groups["value"].Value);
                            break;
                        //case "font":
                        //    cell.Style.Font.Name = aMatch.Groups["value"].Value;
                        //    break;
                        //case "index":
                        //    cell.Style.Font.ScriptPosition = (aMatch.Groups["value"].Value == "sup") ? ScriptPosition.Superscript : ScriptPosition.Subscript;
                        //    break;
                        case "style":
                            switch (aMatch.Groups["value"].Value) {
                                case "underline": cell.Style.Font.UnderlineStyle = UnderlineStyle.Single; break;
                                case "bold": cell.Style.Font.Weight = 5; break;
                                case "italic": cell.Style.Font.Italic = true; break;
                            }
                            break;
                    }
                }
            }
            cell.Value = sb.ToString();
        }

        public static void setExcelCellBorder(MultipleBorders borders, params ExcelCell[] cells) {
            foreach (ExcelCell cell in cells)
                cell.Style.Borders.SetBorders(borders, Color.Black, LineStyle.Medium);
        }

        public static void writeFormattedText(string text, RichTextBox textBox) {
            int newTextSize;
            foreach (System.Text.RegularExpressions.Match pMatch in parameterHeaderRegex.Matches(text)) {
                foreach (System.Text.RegularExpressions.Match aMatch in attributeRegex.Matches(pMatch.Value)) {
                    switch (aMatch.Groups["name"].Value) {
                        case "text":
                            newTextSize = aMatch.Groups["value"].Value.Length;
                            textBox.AppendText(aMatch.Groups["value"].Value);
                            textBox.Select(textBox.Text.Length - newTextSize, newTextSize);
                            break;
                        case "font":
                            textBox.SelectionFont = new Font(aMatch.Groups["value"].Value, textBox.Font.Size, textBox.Font.Style);
                            break;
                        case "index":
                            textBox.SelectionCharOffset = (aMatch.Groups["value"].Value == "sup") ? 5 : -3;
                            textBox.SelectionFont = new Font(textBox.SelectionFont.FontFamily, textBox.SelectionFont.Size - 2, textBox.SelectionFont.Style);
                            break;
                        case "style":
                            switch (aMatch.Groups["value"].Value) {
                                case "underline": textBox.SelectionFont = new Font(textBox.SelectionFont, textBox.SelectionFont.Style | FontStyle.Underline); break;
                                case "bold": textBox.SelectionFont = new Font(textBox.SelectionFont, textBox.SelectionFont.Style | FontStyle.Bold); break;
                                case "italic": textBox.SelectionFont = new Font(textBox.SelectionFont, textBox.SelectionFont.Style | FontStyle.Italic); break;
                            }
                            break;
                    }
                }
                textBox.Select(textBox.Text.Length, 0);
                textBox.SelectionFont = textBox.Font;
                textBox.SelectionCharOffset = 0;
            }
        }

        public virtual List<ToolBox> GetToolBoxes(ISpectrum spectrum, EventHandler changeHandler) {
            if (this._toolBoxes == null) {
                List<ToolBox> result = new List<ToolBox>();
                if (groupDefinition.componentCount == 0) {
                    //component count switcher
                    NumericUpDown componentCount = new NumericUpDown();
                    componentCount.Name = "nudComponentCount";
                    ToolBox groupBox = new ToolBox(groupTabPage);
                    FormatToolBox("Component count", groupBox);
                    groupBox.Name = "ComponentCount";
                    componentCount = new NumericUpDown();
                    componentCount.Dock = DockStyle.Fill;
                    groupBox.Controls.Add(componentCount);
                    componentCount.TextAlign = HorizontalAlignment.Center;
                    componentCount.Value = spectrum.Parameters[groupDefinition.name].Components.Size;
                    //bottomPanel.Controls.Add(groupBox);
                    componentCount.ValueChanged += new EventHandler(componentCount_ValueChanged);
                    componentCount.ValueChanged += changeHandler;
                    result.Add(groupBox);
                    //sorter for multicomponent groups except prompt
                    if (groupDefinition.kind != 3) {
                        groupBox = new PostSearchSortToolBox(groupTabPage);
                        FormatToolBox("Post-search sorting", groupBox);
                        parentProject.SearchCompleted += ((PostSearchToolBox)groupBox).RunPostSearchEvent;
                        parentProject.FirstSpectraSearchCompleted += ((PostSearchToolBox)groupBox).RunPostSearchEvent;
                        result.Add(groupBox);
                    }
                }
                this._toolBoxes = result;
            }
            return this._toolBoxes;
        }

        void componentCount_ValueChanged(object sender, EventArgs e) {
            foreach (ISpectrum spectrum in spectra) {
                spectrum.Parameters[groupDefinition.name].Components.Size = (int)((NumericUpDown)sender).Value;
                spectrum.Container.ResetArrays();
            }
        }

        public void ResetTools(Control.ControlCollection controls, EventHandler additionalChangeHandler) {
            try {
                if (controls.Count > 0)
                    if (controls[0].Controls[0] is NumericUpDown) {
                        NumericUpDown nud = (NumericUpDown)controls["ComponentCount"].Controls[0];
                        nud.ValueChanged -= componentCount_ValueChanged;
                        if (additionalChangeHandler != null)
                            nud.ValueChanged -= additionalChangeHandler;
                        nud.Value = spectra[0].Parameters[groupDefinition.name].Components.Size;
                        nud.ValueChanged += componentCount_ValueChanged;
                        if (additionalChangeHandler != null)
                            nud.ValueChanged += additionalChangeHandler;
                    }
            } catch { }
        }

        protected void FormatToolBox(string caption, ToolBox tb) {
            //T result = new T(gridView);
            tb.Height = 50;
            tb.Width = 140;
            tb.Text = caption;
            tb.Padding = new Padding(20,5,20,10);
            //return result;
        }

    }
}
