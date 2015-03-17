using System;
using System.Collections.Generic;
using System.Text;
using Evel.gui;
using Evel.interfaces;
using System.Windows.Forms;
using Evel.engine.anh.stdmodels;
using Evel.gui.interfaces;

namespace Evel.engine.anh {
    public class AnhGroupGUI : DefaultGroupGUI {

        public AnhGroupGUI() { }

        public AnhGroupGUI(DataGridView grid, List<ISpectrum> spectra, GroupDefinition groupDefinition, GroupTabPage groupTabPage)
            : base(grid, spectra, groupDefinition, groupTabPage) {
            
        }
        
        public override int GetColumnCount() {
            bool includeContribCell = ((groupDefinition.Type & GroupType.Contributet) == GroupType.Contributet) && ((groupDefinition.Type & GroupType.CalcContribution) != GroupType.CalcContribution);
            int columnCount = base.GetColumnCount();
            return columnCount + ((includeContribCell && columnCount > ((DataGridParameterView)gridView).FixedCols) ? 1 : 0);
        }

        public override void SetHeaders() {
            base.SetHeaders();
            if (GetColumnCount() > ((DataGridParameterView)gridView).FixedCols) {
                bool includeContribCell = ((groupDefinition.Type & GroupType.Contributet) == GroupType.Contributet) && ((groupDefinition.Type & GroupType.CalcContribution) != GroupType.CalcContribution);
                if (includeContribCell)
                    gridView.Columns[gridView.ColumnCount - 1].HeaderText = "[p text='Contrib.']";
            }
        }

        public override DataGridViewSpectrumRow CreateSpectrumRow(ISpectrum spectrum) {
            DataGridViewSpectrumRow result = base.CreateSpectrumRow(spectrum);
            if (GetColumnCount() > ((DataGridParameterView)gridView).FixedCols) {
                if (((groupDefinition.Type & GroupType.Contributet) == GroupType.Contributet) && ((groupDefinition.Type & GroupType.CalcContribution) != GroupType.CalcContribution)) {
                    IParameter parameter = ((ContributedGroup)spectrum.Parameters[groupDefinition.name]).contribution;
                    DataGridViewParameterCell cell = new DataGridViewParameterCell(parameter);
                    //cell.UserValue = parameter.Value;
                    //if source add special conversion of contribution
                    if (groupDefinition.kind == 2) {
                        cell.ConvertFromUserValue += new UserValueConversionHandler(ConvertFromUserSource);
                        cell.ConvertToUserValue += new UserValueConversionHandler(ConvertToUserSource);
                    }
                    result.Cells.Add(cell);
                }
            }
            return result;
        }

        public override Type ProjectType {
            get {
                return typeof(AnhProject);
            }
        }

        public override List<ToolBox> GetToolBoxes(ISpectrum spectrum, EventHandler changeHandler) {
            if (this._toolBoxes == null) {
                this._toolBoxes = base.GetToolBoxes(spectrum, changeHandler);
                if (this.groupDefinition.kind == 3 || this.groupDefinition.kind == 4) { //if prompt group add control to switch parameters to default values
                    ToolBox groupBox = new ToolBox(groupTabPage);
                    FormatToolBox("Default", groupBox);
                    Button defaultButton = new Button();
                    defaultButton.Text = "Set default values";
                    defaultButton.Dock = DockStyle.Fill;
                    defaultButton.Click += new EventHandler(defaultButton_Click);
                    defaultButton.Click += changeHandler;
                    groupBox.Controls.Add(defaultButton);
                    this._toolBoxes.Add(groupBox);
                }
            }
            return this._toolBoxes;
        }

        void defaultButton_Click(object sender, EventArgs e) {
            foreach (ISpectrum spectrum in spectra) {
                spectrum.Container.ResetArrays();
                IGroup group;
                switch (this.groupDefinition.kind) {
                    case 3:
                        group = spectrum.Parameters[3];
                        if (group.Definition.SetDefaultComponents != null)
                            group.Definition.SetDefaultComponents(group, spectrum, null);
                        break;
                    case 4:
                        group = spectrum.Parameters[4];
                        if (group.Definition.SetDefaultComponents != null)
                            group.Definition.SetDefaultComponents(group, spectrum, new DefaultRangesEventArgs(false, false, false, true));
                        break;
                }
            }
            if (this.groupDefinition.kind == 3)
                foreach (GroupBox toolBox in _toolBoxes) {
                    if (toolBox.Controls[0] is NumericUpDown) {
                        NumericUpDown spin = (NumericUpDown)toolBox.Controls[0];
                        if (spin.Value != spectra[0].Parameters[3].Components.Size)
                            spin.Value = spectra[0].Parameters[3].Components.Size;
                        break;
                    }
                }
            gridView.Invalidate();
        }

        public override Form CreateValuesAdjuster(List<ISpectrum> spectra, ISpectrum adjustingSpectrum) {
            if (!HasGraphicAdjustment) return null;
            DefaultAdjusterGUI result = new DefaultAdjusterGUI(spectra, adjustingSpectrum);
            result.SetDefaultValue = new SetDefaultValueHandler(getDefaultParameterValue);
            ValueCoordinates value = new ValueCoordinates();
            value.spectrum = adjustingSpectrum;
            value.groupName = "ranges";
            value.componentId = 0;
            value.minValue = 1;
            //value.maxValue = adjustingSpectrum.ExperimentalSpectrum.Length-1;
            value.maxValue = adjustingSpectrum.BufferEndPos - adjustingSpectrum.BufferStartPos - 1;
            value.parameter = adjustingSpectrum.Parameters[4].Components[0][1];
            result.AddAdjuster(value, Orientation.Vertical, MouseButtons.Left);
            value.parameter = adjustingSpectrum.Parameters[4].Components[0][2];
            result.AddAdjuster(value, Orientation.Vertical, MouseButtons.Right);
            value.parameter = adjustingSpectrum.Parameters[4].Components[0][0];
            result.AddAdjuster(value, Orientation.Vertical, MouseButtons.Middle);
            value.maxValue = 1;  //if max == min scrollbar is not displayed
            value.parameter = adjustingSpectrum.Parameters[4].Components[0][3];
            result.AddAdjuster(value, Orientation.Horizontal);
            return result;
        }

        public override double getDefaultParameterValue(ISpectrum spectrum, IParameter parameter) {
            if (spectrum.Parameters[4].Components[0].ContainsParameter(parameter.Definition.Name) &&
                spectrum.Parameters[4].Definition.SetDefaultComponents != null) {
                bool zero = false;
                bool start = false;
                bool stop = false;
                bool background = false;
                switch (parameter.Definition.Name) {
                    case "start": start = true; break;
                    case "stop": stop = true; break;
                    case "zero": zero = true; break;
                    case "background": background = true; break;
                }
                DefaultRangesEventArgs args = new DefaultRangesEventArgs(zero, start, stop, background);
                spectrum.Parameters[4].Definition.SetDefaultComponents(
                    spectrum.Parameters[4],
                    spectrum,
                    args);
            } 
            return parameter.Value;

        }

        public override bool HasGraphicAdjustment {
            get { return groupDefinition.name == "ranges"; }
        }

        protected void ConvertToUserSource(IParameter parameter, ref double value) {
            //konwersja dotyczy wkładów podczas obliczeń, których status nie jest fixed
            //oraz nie będących aktualnie wyznaczanych na podstawie wyniku układu równań liniowych
            //(status local i brak flagi IncludeInts/IncludeSourceContribution w projekcie
            if (parentProject.SearchMode != SearchMode.Inactive && (parameter.Status & ParameterStatus.Fixed) == 0 && 
                !((parentProject.Flags & SearchFlags.IncludeSourceContribution) == 0 && (parameter.Status & ParameterStatus.Local) > 0))
                value = value / (1 + value);
            value *= 100;
        }

        protected void ConvertFromUserSource(IParameter parameter, ref double value) {
            value /= 100;
        }

        protected void Absolute(IParameter parameter, ref double value) {
            value = Math.Abs(value);
        }

        //protected void ConvertToUserShift(ref double value) {
        //    value /= container.Spectra[0].Constants[0];
        //}

        //protected void ConvertFromUserShift(ref double value) {
        //    value *= container.Spectra[0].Constants[0];
        //}

        public override void CellFormatting(Object sender, DataGridViewCellFormattingEventArgs e) {
            if (groupDefinition.name.ToLower() == "prompt") {
                DataGridParameterView grid = (DataGridParameterView)sender;
                if (grid[e.ColumnIndex, e.RowIndex] is DataGridViewParameterCell) {
                    IParameter parameter = ((DataGridViewParameterCell)grid[e.ColumnIndex, e.RowIndex]).Parameter;
                    if (parameter.HasReferenceValue || (e.RowIndex != 1 && spectra[0].Container.ParentProject.CalculatedValues &&
                        ((parameter.Status & (ParameterStatus.Local | ParameterStatus.Free)) == (ParameterStatus.Local | ParameterStatus.Free)))) {
                        e.Value = "-";
                        grid[e.ColumnIndex, e.RowIndex].Style.Alignment = DataGridViewContentAlignment.MiddleCenter;
                    } else {
                        DataGridViewParameterCell cell = (DataGridViewParameterCell)grid[e.ColumnIndex, e.RowIndex];
                        //e.Value = String.Format("{0:G05}", cell.Value);
                        if (cell.UserError > (double)cell.Value || cell.UserError == 0)
                            e.Value = ((double)cell.Value).ToString("G05", numberFormat);
                        else if (cell.UserError > 0 && cell.UserError < 1) {
                            e.Value = ((double)cell.Value).ToString(String.Format("F{0}", Math.Ceiling(Math.Abs(Math.Log10(cell.UserError))), numberFormat));
                        } else //if (cell.UserError > 1) {
                            e.Value = ((double)cell.Value).ToString("F0", numberFormat);
                        //}
                        grid[e.ColumnIndex, e.RowIndex].Style.Alignment = DataGridViewContentAlignment.MiddleLeft;
                    }
                    e.FormattingApplied = true;
                }
                if ((grid[e.ColumnIndex, e.RowIndex].ReadOnly = IsCellReadOnly(grid[e.ColumnIndex, e.RowIndex])) && e.ColumnIndex >= grid.FixedCols)
                    e.CellStyle.ForeColor = System.Drawing.Color.Black;
            } else {
                base.CellFormatting(sender, e);
            }
        }

        //public override bool IsCellReadOnly(DataGridViewCell cell) {
        //    bool result = base.IsCellReadOnly(cell);
            //if (cell.RowIndex == 1
            //    && cell is DataGridViewParameterCell
            //    && groupDefinition.name.ToLower() == "prompt") {
            //    IParameter parameter = ((DataGridViewParameterCell)cell).Parameter;
            //    if (parameter.Definition.Name.ToLower() == "int" && !container.ParentProject.IsBusy)
            //        //result = result || ((parameter.Status & ParameterStatus.Free) == ParameterStatus.Free);
            //        result = false;
            //}
        //    return result;
        //}

        public override DataGridViewParameterCell CreateParameterCell(IParameter parameter) {
            DataGridViewParameterCell cell = base.CreateParameterCell(parameter);
            if (parameter.Definition.Name.IndexOf("shift", StringComparison.CurrentCultureIgnoreCase) == -1)
                //if (parameter.Definition.Name.IndexOf("int", StringComparison.CurrentCultureIgnoreCase) == -1)
                cell.ConvertToUserValue += Absolute;
            //else {
            //    cell.ConvertFromUserValue += ConvertFromUserShift;
            //    cell.ConvertToUserValue += ConvertToUserShift;
            //}
            return cell;
        }

    }
}
