using System;
using System.Windows.Forms;
using Evel.interfaces;
using System.Drawing;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.ComponentModel;
using System.Drawing.Drawing2D;
using Evel.gui.interfaces;

namespace Evel.gui {  

    [Serializable]
    public class DataGridParameterView : DataGridView {

        public delegate void SwapHandler(int i, int j);
        public delegate bool IncludeRule(object sender, ISpectrum spectrum);

        //private static List<CellParameterStatus> _statusesSource;
        private SortOrder currentSortOrder = SortOrder.None;
        private Color readonlyColor = SystemColors.ButtonFace;
        private List<string> _undoList;
        private int _undoPosition;
        private static Brush[] ReferenceBrushes = {
                                                      Brushes.OrangeRed,
                                                      Brushes.Olive,
                                                      Brushes.Magenta,
                                                      Brushes.Lime,
                                                      Brushes.DimGray,
                                                      Brushes.Silver,
                                                      Brushes.Maroon,
                                                      Brushes.Yellow,
                                                      Brushes.DarkSeaGreen,
                                                      Brushes.SpringGreen,
                                                      Brushes.Indigo,
                                                      Brushes.Plum,
                                                      Brushes.CadetBlue,
                                                      Brushes.Blue,
                                                      new HatchBrush(HatchStyle.Percent50, Color.White, Color.OrangeRed),
                                                      new HatchBrush(HatchStyle.Percent50, Color.White, Color.Olive),
                                                      new HatchBrush(HatchStyle.Percent50, Color.White, Color.Magenta),
                                                      new HatchBrush(HatchStyle.Percent50, Color.White, Color.Lime),
                                                      new HatchBrush(HatchStyle.Percent50, Color.White, Color.DimGray),
                                                      new HatchBrush(HatchStyle.Percent50, Color.White, Color.Silver),
                                                      new HatchBrush(HatchStyle.Percent50, Color.White, Color.Maroon),
                                                      new HatchBrush(HatchStyle.Percent50, Color.White, Color.Yellow),
                                                      new HatchBrush(HatchStyle.Percent50, Color.White, Color.DarkSeaGreen),
                                                      new HatchBrush(HatchStyle.Percent50, Color.White, Color.SpringGreen),
                                                      new HatchBrush(HatchStyle.Percent50, Color.White, Color.Indigo),
                                                      new HatchBrush(HatchStyle.Percent50, Color.White, Color.Plum),
                                                      new HatchBrush(HatchStyle.Percent50, Color.White, Color.CadetBlue),
                                                      new HatchBrush(HatchStyle.Percent50, Color.White, Color.Blue)
                                                  };
        //private static Regex parameterHeaderRegex = new Regex(@"\[p(?:\s\w+='[\w\d]+')+\]", RegexOptions.Compiled);
        //private static Regex attributeRegex = new Regex(@"(?<name>\w+)='(?<value>[\w\d]+)'", RegexOptions.Compiled);
        
        //private Font _greekFont;
        //private Font _indexGreekFont;
        //private Font _indexFotn;

        public int FixedCols { get; set; }

        public Color ReadonlyCellBackColor { get { return this.readonlyColor; } set { this.readonlyColor = value; } }
       
        public string GridValues {
            get {
                System.Text.StringBuilder result = new System.Text.StringBuilder();
                for (int rowId = 0; rowId < RowCount; rowId++) {
                    for (int colId = 0; colId < ColumnCount; colId++) {
                        if (this[colId, rowId] is DataGridViewParameterCell)
                            result.Append(((DataGridViewParameterCell)this[colId, rowId]).Parameter.Value.ToString());
                        else {
                            if (this[colId, rowId].Value != null) {
                                //if (this[colId, rowId] is DataGridViewComboBoxCell) {
                                //    result.Append("status:");
                                //    //if (this[colId, rowId].Value is CellParameterStatus)
                                //    result.Append(((ParameterStatus)Int32.Parse(this[colId, rowId].Value.ToString())).ToString());
                                //}
                                result.Append(this[colId, rowId].Value.ToString());
                            }
                        }
                        if (colId<ColumnCount-1)
                            result.Append('\t');
                    }
                    result.Append("\r\n");
                }
                return result.ToString();
            }
        }

        public bool CanUndo {
            get { return this._undoPosition > 0 && this._undoList.Count > 1; }
        }

        public bool CanRedo {
            get { return this._undoPosition < this._undoList.Count - 1; }
        }

        public void SaveUndoStep() {
            if (_undoList.Count > 5) {
                _undoList.RemoveAt(0);
            } else {
                _undoPosition++;
            }
            _undoList.Add(GridValues);
        }

        private void refill() {
            string[] cellValues = _undoList[_undoPosition].Split(new string[] { "\t", "\r\n" }, StringSplitOptions.None);
            int cellValueId = 0;
            for (int rowId = 0; rowId < RowCount; rowId++) {
                for (int colId = 0; colId < ColumnCount; colId++) {
                    if (rowId == 0 && colId < 2) {
                        cellValueId++;
                        continue;
                    }
                    if (cellValues[cellValueId].Contains("status:") && this[colId, rowId] is DataGridViewComboBoxCell) {
                        this[colId, rowId].Value = CellParameterStatus.FromString(cellValues[cellValueId]);
                    } else {
                        if (this[colId, rowId] is DataGridViewParameterCell) {
                            ((DataGridViewParameterCell)this[colId, rowId]).Parameter.Value = Double.Parse(cellValues[cellValueId].Replace(',', '.'));
                        } else {
                            this[colId, rowId].Value = cellValues[cellValueId];
                        }
                    }
                    cellValueId++;
                }
            }
            Invalidate();
        }

        //public void Undo() {
        //    if (_undoPosition > 0) {
        //        _undoPosition--;
        //        refill();
        //    }
        //}

        //public void Redo() {
        //    if (_undoPosition+1 < _undoList.Count) {
        //        _undoPosition++;
        //        refill();
        //    }
        //}

        #region Construction

        public DataGridParameterView() : base() {
            base.DoubleBuffered = true;
            this._undoList = new List<string>();
            this._undoPosition = -1;
            //_context = BufferedGraphicsManager.Current;
            //_graphicBuffer = _context.Allocate(this.CreateGraphics(), this.ClientRectangle);
        }

        private void InitializeComponent() {
            System.Windows.Forms.DataGridViewCellStyle dataGridViewCellStyle1 = new System.Windows.Forms.DataGridViewCellStyle();
            ((System.ComponentModel.ISupportInitialize)(this)).BeginInit();
            this.SuspendLayout();
            // 
            // DataGridParameterView
            // 
            dataGridViewCellStyle1.Alignment = System.Windows.Forms.DataGridViewContentAlignment.MiddleLeft;
            dataGridViewCellStyle1.BackColor = System.Drawing.SystemColors.Control;
            dataGridViewCellStyle1.Font = new System.Drawing.Font("Microsoft Sans Serif", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(238)));
            dataGridViewCellStyle1.ForeColor = System.Drawing.SystemColors.WindowText;
            dataGridViewCellStyle1.SelectionBackColor = System.Drawing.SystemColors.Highlight;
            dataGridViewCellStyle1.SelectionForeColor = System.Drawing.SystemColors.HighlightText;
            dataGridViewCellStyle1.WrapMode = System.Windows.Forms.DataGridViewTriState.True;
            this.ColumnHeadersDefaultCellStyle = dataGridViewCellStyle1;
            ((System.ComponentModel.ISupportInitialize)(this)).EndInit();
            this.ResumeLayout(false);

        }

        #endregion Construction

        #region DrawingMethods

        //public static void DrawHeaderContent(string s, Graphics g, Font font, Rectangle rect, bool center, Brush brush) {
        //    //if (e.Value == null) return;
        //    string[] subs = s.Split(new char[] { '\0' }); //, StringSplitOptions.RemoveEmptyEntries);
        //    int[] xOffset = new int[subs.Length];
        //    int[] yOffset = new int[subs.Length];
        //    Font[] fonts = new Font[subs.Length];
        //    int stringWidth = 0;
        //    int topPadding = 3;
        //    for (int i = 0; i < subs.Length; i++) {
        //        //switch (subs[i][0]) {
        //        if (subs[i].IndexOf("greek:") == 0) {
        //            //case '@':
        //            subs[i] = subs[i].Substring(6);
        //            fonts[i] = new Font("Symbol", font.Size, FontStyle.Bold);
        //            yOffset[i] = topPadding;
        //        } else {
        //            if (subs[i].IndexOf("sub:") == 0) {
        //                subs[i] = subs[i].Substring(4);
        //                fonts[i] = (i == 0) ? font : new Font(font.FontFamily, font.Size - 2, FontStyle.Bold);
        //                yOffset[i] = topPadding + 5;
        //            } else {
        //                yOffset[i] = topPadding;
        //                fonts[i] = new Font(font, FontStyle.Bold);
        //            }
        //        }

        //        xOffset[i] = stringWidth - i * 2;
        //        stringWidth += (int)g.MeasureString(subs[i], fonts[i], 1000, new StringFormat(StringFormatFlags.MeasureTrailingSpaces)).Width;
        //    }
        //    int x0;
        //    if (center) {
        //        x0 = rect.Left + (rect.Width - stringWidth) / 2;
        //    } else {
        //        x0 = rect.Left;
        //    }
        //    g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.SingleBitPerPixelGridFit;
        //    for (int i = 0; i < subs.Length; i++) {
        //        //g.DrawString(subs[i], fonts[i], brush, x0 + xOffset[i], rect.Top + yOffset[i]);
        //        g.DrawString("R", SystemFonts.CaptionFont, SystemBrushes.ControlText, x0 + xOffset[i], rect.Top + yOffset[i]);
        //    }
        //}

        protected override void OnCellPainting(DataGridViewCellPaintingEventArgs e) {
            if (e.ColumnIndex == -1) {
                return;
            }
            //e.Graphics.SmoothingMode = SmoothingMode.HighSpeed;
            //headers
            if ((e.RowIndex == -1) || this[e.ColumnIndex, e.RowIndex].FormattedValue.ToString().Contains("[p")) { 
                    e.Paint(e.ClipBounds, DataGridViewPaintParts.ContentBackground | DataGridViewPaintParts.Border | DataGridViewPaintParts.Background);
                    DefaultGroupGUI.DrawHeaderContent(e.Value.ToString(), e.Graphics, e.CellStyle.Font, e.CellBounds, e.CellStyle.Alignment == DataGridViewContentAlignment.MiddleCenter, Brushes.Black);
                e.Handled = true;
            } else {
                if (this[e.ColumnIndex, e.RowIndex] is DataGridViewParameterCell) {
                    DataGridViewParameterCell cell = (DataGridViewParameterCell)this[e.ColumnIndex, e.RowIndex];
                    if ((cell.Parameter.Definition.Properties & ParameterProperties.KeyValue) == 0) {
                        Rectangle newRect = new Rectangle(e.CellBounds.X,
                            e.CellBounds.Y, e.CellBounds.Width - 2,
                            e.CellBounds.Height - 2);
                        Color backColor;
                        //if (cell.Parameter.Expression != null) {
                        //    backColor = SystemColors.ButtonFace;
                        //} else {
                        if ((e.State & DataGridViewElementStates.Selected) == DataGridViewElementStates.Selected)
                            backColor = System.Drawing.SystemColors.Highlight;
                        else
                            backColor = (cell.ReadOnly) ? readonlyColor : e.CellStyle.BackColor;
                        //}
                        using (
                            Brush gridBrush = new SolidBrush(GridColor),
                            backColorBrush = new SolidBrush(backColor)) {
                            IParameter referencedParameter = (cell.Parameter.HasReferenceValue) ? cell.Parameter.ReferencedParameter : cell.Parameter;
                            using (Pen gridLinePen = new Pen(gridBrush), rectPen = new Pen(MainForm.GetColor(referencedParameter.Status))) {

                                // Erase the cell.
                                e.Graphics.FillRectangle(backColorBrush, newRect);

                                //if partially referenced parameter draw a reference sign
                                if (cell.Parameter.ReferenceGroup > 0) {
                                    e.Graphics.FillRectangle(ReferenceBrushes[(cell.Parameter.ReferenceGroup - 1) % ReferenceBrushes.Length],
                                        newRect.Right - 6,
                                        newRect.Top + 3,
                                        4,
                                        newRect.Height - 5);
                                }

                                //min max lines
                                if (!cell.Parameter.HasReferenceValue) {
                                    if (!double.IsPositiveInfinity(cell.Parameter.Maximum))
                                        e.Graphics.DrawLine(Pens.Black,
                                            newRect.Left + 3, newRect.Top + 3, newRect.Right - 3, newRect.Top + 3);
                                    if (!double.IsNegativeInfinity(cell.Parameter.Minimum))
                                        e.Graphics.DrawLine(Pens.Black,
                                            newRect.Left + 3, newRect.Bottom - 3, newRect.Right - 3, newRect.Bottom - 3);
                                }
                                // Draw the grid lines (only the right and bottom lines;
                                // DataGridView takes care of the others).
                                e.Graphics.DrawLine(Pens.White, e.CellBounds.Left,
                                    e.CellBounds.Bottom - 1, e.CellBounds.Right - 1,
                                    e.CellBounds.Bottom - 1);
                                e.Graphics.DrawLine(Pens.White, e.CellBounds.Right - 1,
                                    e.CellBounds.Top, e.CellBounds.Right - 1,
                                    e.CellBounds.Bottom);

                                // Draw the inset highlight box.
                                e.Graphics.DrawRectangle(rectPen, newRect);

                                // Draw the text content of the cell
                                e.PaintContent(e.CellBounds);
                                e.Handled = true;

                            }
                        }
                    }
                }
            }
        }

        #endregion DrawingMethods

        #region SortingMethods

        public void Sort(object sender, Comparison<ISpectrum> comparison, SwapHandler swap, IncludeRule irule) {
            short direction;
            if (currentSortOrder == SortOrder.Ascending) {
                currentSortOrder = SortOrder.Descending;
                direction = -1;
            } else {
                currentSortOrder = SortOrder.Ascending;
                direction = 1;
            }
            int left = 1;
            while (!irule(sender, ((DataGridViewSpectrumRow)Rows[left]).Spectrum))
                if (++left >= RowCount) break;
            if (left < RowCount)
                QuickSort(sender, left, RowCount, comparison, swap, irule, direction);
        }

        private void SwapRows(int i, int j) {
            if (i == j) return;
            DataGridViewRow hrow, lrow;
            if (i < j) {
                lrow = Rows[i];
                hrow = Rows[j];
                Rows.RemoveAt(j);
                Rows.RemoveAt(i);
                Rows.Insert(i, hrow);
                Rows.Insert(j, lrow);
            } else {
                lrow = Rows[j];
                hrow = Rows[i];
                Rows.RemoveAt(i);
                Rows.RemoveAt(j);
                Rows.Insert(j, hrow);
                Rows.Insert(i, lrow);
            }
        }

        private void QuickSort(object sender, int left, int right, Comparison<ISpectrum> comparison, SwapHandler swap, IncludeRule irule, short direction) {
            if (left < right) {
                ISpectrum ispectrum;
                ISpectrum leftspectrum = ((DataGridViewSpectrumRow)Rows[left]).Spectrum;
                int m = left;
                for (int i = left + 1; i < right; i++) {
                    if (irule(sender, ispectrum = ((DataGridViewSpectrumRow)Rows[i]).Spectrum)) {
                    //ispectrum = ((DataGridViewSpectrumRow)Rows[i]).Spectrum;
                        if (direction * comparison(ispectrum, leftspectrum) < 0) {
                            m++;
                            while (!irule(sender, ((DataGridViewSpectrumRow)Rows[m]).Spectrum))
                                if (++m >= i) break;
                            if (i != m) {
                                SwapRows(m, i);
                                if (swap != null)
                                    swap(m - 1, i - 1);
                            }
                        }
                    }// else m++;
                }
                SwapRows(left, m);
                if (swap != null)
                    swap(left - 1, m - 1);
                QuickSort(sender, left, m, comparison, swap, irule, direction);
                m++;
                if (m < right) {
                    while (!irule(sender, ((DataGridViewSpectrumRow)Rows[m]).Spectrum))
                        if (++m >= right) break;
                    QuickSort(sender, m, right, comparison, swap, irule, direction);
                }
            }
        }

        #endregion SortingMethods
    }

    public class CellParameterStatus {
        public readonly ParameterStatus status;
        public CellParameterStatus(ParameterStatus status) {
            this.status = status;
        }
        public string ValueMember {
            get {
                return ((int)status).ToString();
            }
        }
        public Color ValueColor {
            get {
                return MainForm.GetColor(status);
            }
        }
        public string DisplayMember { 
            get {
                if (status == ParameterStatus.None)
                    return "mixed";
                else
                    return status.ToString().Replace(", ", " "); 
            } 
        }

        public override string ToString() {
            return ValueMember;
        }

        public static CellParameterStatus FromString(string statusName) {
            ParameterStatus status = 0;
            if (statusName.ToLower().Contains("local"))
                status |= ParameterStatus.Local;
            if (statusName.ToLower().Contains("common"))
                status |= ParameterStatus.Common;
            if (statusName.ToLower().Contains("free"))
                status |= ParameterStatus.Free;
            if (statusName.ToLower().Contains("fixed"))
                status |= ParameterStatus.Fixed;
            foreach (CellParameterStatus cps in DefaultGroupGUI.StatusesSource)
                if (cps.status == status)
                    return cps;
            return null;
        }

    }


}
