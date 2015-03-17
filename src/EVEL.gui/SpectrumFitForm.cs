using System.Windows.Forms;
using Evel.interfaces;
using System.Collections.Generic;
using System;
//using chartings;
using System.Drawing;
using System.Threading;
using System.IO;
using System.Text;
using digit;

namespace Evel.gui {

    [Flags]
    public enum SpectrumFitFormControls {
        FitAgain = 0x01,
        Ok = 0x2,
        SpectraSelector = 0x04
    }

    public partial class SpectrumFitForm : Form {

        //private ISpectraContainer _container;
        private SpectrumFitFormControls _buttons;
        private ISpectrum[] _spectra;
        //private List<LineSeries> theoreticalCurves;
        private PointSeries experimentSeries, residualsSeries;
        private List<digit.Series> theoreticalCurves;
        private List<Color> seriesColors;
        private static float[][] curves = null;
        private static string[] curveNames = null;
        private static float[] diffs = null;
        private int curvesCount = 0;

        public SpectrumFitForm(ISpectrum selectedSpectrum, ICollection<ISpectrum> spectra, SpectrumFitFormControls buttons) {
            InitializeComponent();
            theoreticalCurves = new List<Series>();
            seriesColors = new List<Color>();
            this._buttons = buttons;

            try
            {
                this.experimentSeries = new PointSeries(10000, Evel.engine.SpectraContainerBase.EXP_LITERAL, chart1, Color.Red, 1);
                this.residualsSeries = new PointSeries(10000, Evel.engine.SpectraContainerBase.EXP_LITERAL, chart2, Color.BlueViolet, 1);

                foreach (ISpectrum spectrum in spectra)
                    spectraSelector.Items.Add(spectrum.Name);
                this._spectra = new ISpectrum[spectra.Count];
                spectra.CopyTo(this._spectra, 0);
                if (selectedSpectrum == null || !spectra.Contains(selectedSpectrum))
                    selectedSpectrum = _spectra[0];
                button2.Visible = (_buttons & SpectrumFitFormControls.FitAgain) == SpectrumFitFormControls.FitAgain;
                spectraSelector.Visible = (_buttons & SpectrumFitFormControls.SpectraSelector) == SpectrumFitFormControls.SpectraSelector;
                button1.Visible = (_buttons & SpectrumFitFormControls.Ok) == SpectrumFitFormControls.Ok;
                spectraSelector.SelectedItem = selectedSpectrum.Name;
            }
            catch (Exception)
            { }
        }

        private void ShowSpectrum(ISpectrum spectrum) {
            int i,ch,s;
            chart1.Title = spectrum.Title;
            
            chart1.ClearSeries();
            chart2.ClearSeries();
            //theoreticalCurves.Clear();
            int start = (int)spectrum.Parameters[4].Components[0][1].Value;
            int stop = (int)spectrum.Parameters[4].Components[0][2].Value;

            //Dictionary<string, double[]> ths = new Dictionary<string, double[]>();// new double[stop + 1];
            //double[] diffs = new double[stop + 1];
            bool intensitiesFromSearch = !((this._buttons & SpectrumFitFormControls.FitAgain) == SpectrumFitFormControls.FitAgain);
            //spectrum.Container.getTheoreticalSpectrum(spectrum, ref ths, ref diffs, intensitiesFromSearch);
            spectrum.Container.getTheoreticalSpectrum(spectrum, ref curves, ref curveNames, ref diffs, intensitiesFromSearch);

            Random random = new Random();
            int seriesId = 0;
            for (i = 0; i < curves.Length; i++) {
                if (curveNames[i] != String.Empty) {
                    Series series;
                    if (theoreticalCurves.Count <= seriesId) {
                        series = new Series(curves[0].Length, curveNames[i], chart1);
                        theoreticalCurves.Add(series);
                    } else {
                        series = theoreticalCurves[seriesId];
                        series.Clear();
                        series.name = curveNames[i];
                    }

                    //series.Title = curveNames[i];
                    if (seriesId >= seriesColors.Count) {
                        if (curveNames[i] != Evel.engine.SpectraContainerBase.TH_LITERAL)
                            seriesColors.Add(System.Drawing.Color.FromArgb(random.Next(0, 200), random.Next(0, 200), random.Next(0, 200)));
                        else
                            seriesColors.Add(System.Drawing.Color.DodgerBlue);
                    } else {
                        if (curveNames[i] != Evel.engine.SpectraContainerBase.TH_LITERAL && seriesColors[seriesId] == Color.DodgerBlue)
                            seriesColors[seriesId] = System.Drawing.Color.FromArgb(random.Next(0, 200), random.Next(0, 200), random.Next(0, 200));
                    }
                    series.Color = seriesColors[seriesId++];
                    //series.ParentChart = chart1;
                    //series.Width = 1;
                    
                }
            }

            double bs = spectrum.Parameters[0].Components[0]["bs"].Value;
            float time;
            for (ch = start; ch < stop; ch++) {
                time = (float)((ch - spectrum.Parameters["ranges"].Components[0]["zero"].Value) * bs);
                for (i = 0, s=0; i<curves.Length; i++)
                    if (curveNames[i] != String.Empty)
                        theoreticalCurves[s++].AddPoint(time, curves[i][ch], false);
                    

                this.experimentSeries.AddPoint(time, spectrum.Container.Data[ch+spectrum.BufferStartPos-1], false);
                //chart1.Series[1].AddXY(ch, (float)ths["Theoretical spectrum"][ch]);
                
                //foreach (LineSeries series in theoreticalCurves) {
                //    if (ths[series.Name][ch] > 0.1)
                //        series.AddXY((float)time, (float)(ths[series.Name][ch]));
                //}
                this.residualsSeries.AddPoint(time, diffs[ch - start], false);
            }
            chart1.AddSeries(this.experimentSeries);
            chart2.AddSeries(this.residualsSeries);
            for (i = 0; i < theoreticalCurves.Count; i++)
                chart1.AddSeries(theoreticalCurves[i]);
            chart1.Refresh();
            chart2.Refresh();
        }

        private void panel1_Paint(object sender, PaintEventArgs e) {
            MainForm.DialogPanel_Paint(sender, e);
        }

        private void spectraSelector_SelectedIndexChanged(object sender, System.EventArgs e) {
            if (toolStripButton3.Checked) {
                toolStripButton3.PerformClick();
            }
            ShowSpectrum(_spectra[spectraSelector.SelectedIndex]);
        }

        private void toolStripButton3_Click(object sender, EventArgs e) {
            seriesGrid.Rows.Clear();
            seriesGridPanel.Visible = toolStripButton3.Checked;
            int rowId = 0;
            foreach (Series series in chart1.Series) {
                if (series != null) {
                    if (series.PointCount > 0) {
                        seriesGrid.Rows.Add();
                        seriesGrid.Rows[rowId].HeaderCell.Value = series;
                        seriesGrid[0, rowId].Value = series.name;
                        seriesGrid[0, rowId].Style.BackColor = SystemColors.ButtonFace;
                        seriesGrid[0, rowId].Style.SelectionBackColor = SystemColors.ButtonFace;
                        seriesGrid[0, rowId].Style.SelectionForeColor = SystemColors.ControlText;
                        ((DataGridViewCheckBoxCell)seriesGrid[1, rowId]).Value = series.visible;
                        seriesGrid[1, rowId].Style.SelectionBackColor = Color.White;
                        seriesGrid[2, rowId].Style.BackColor = series.Color;
                        seriesGrid[2, rowId].Style.SelectionBackColor = series.Color;
                        rowId++;
                    }
                }
            }
            seriesGridPanel.Size = new Size(seriesGrid.ClientSize.Width + 25, (42 + (seriesGrid.Rows[0].Height) * seriesGrid.RowCount));
        }

        private void seriesGrid_CellContentClick(object sender, DataGridViewCellEventArgs e) {
            if (e.RowIndex == -1 || e.ColumnIndex == -1) return;
            Series series = (Series)seriesGrid.Rows[e.RowIndex].HeaderCell.Value;
            if (seriesGrid[e.ColumnIndex, e.RowIndex] is DataGridViewCheckBoxCell) {
                series.visible = (bool)seriesGrid[e.ColumnIndex, e.RowIndex].Value;
                chart1.Invalidate();
            } else {
                if (seriesGrid[e.ColumnIndex, e.RowIndex] is DataGridViewButtonCell) {
                    ColorDialog dialog = new ColorDialog();
                    dialog.Color = series.Color;
                    if (dialog.ShowDialog() == DialogResult.OK) {
                        series.Color = dialog.Color;
                        seriesGrid[e.ColumnIndex, e.RowIndex].Style.BackColor = dialog.Color;
                        seriesGrid[e.ColumnIndex, e.RowIndex].Style.SelectionBackColor = dialog.Color;
                        seriesGrid.Invalidate();
                        chart1.Refresh();
                    }
                }
            }
        }

        private void seriesGrid_CurrentCellDirtyStateChanged(object sender, EventArgs e) {
            seriesGrid.CommitEdit(DataGridViewDataErrorContexts.Commit);
        }

        private void toolStripButton1_Click(object sender, EventArgs e) {
            Thread saveThread = new Thread(new ThreadStart(SaveSpectrumBitmap));
            saveThread.SetApartmentState(ApartmentState.STA);
            saveThread.Start();
        }

        private delegate void DrawToBitmapEventHandler(Bitmap bitmap, Rectangle rectangle);

        private void SaveSpectrumBitmap() {
            SaveFileDialog dialog = new SaveFileDialog();
            dialog.Filter = "Bitmap (*.bmp)|*.bmp";
            if (dialog.ShowDialog() == DialogResult.OK) {
                Bitmap bitmap = new Bitmap(chart1.Width, chart1.Height + chart2.Height);
                this.Invoke(
                    new DrawToBitmapEventHandler(chart1.DrawToBitmap), 
                    new object[] {
                        bitmap, 
                        new Rectangle(0, 0, chart1.Width, chart1.Height) });
                this.Invoke(
                    new DrawToBitmapEventHandler(chart2.DrawToBitmap),
                    new object[] {
                        bitmap, 
                        new Rectangle(0, chart1.Height+1, chart1.Width, chart2.Height)});
                bitmap.Save(Path.ChangeExtension(dialog.FileName, "bmp"));
            }
        }

        private void toolStripButton2_Click(object sender, EventArgs e) {
            Thread thread = new Thread(new ThreadStart(SaveSpectrumTabs));
            thread.SetApartmentState(ApartmentState.STA);
            thread.Start();
        }

        private void SaveSpectrumTabs() {
            SaveFileDialog dialog = new SaveFileDialog();
            dialog.Filter = "Tab separated file (*.txt)|*.txt";
            if (dialog.ShowDialog() == DialogResult.OK) {
                
                StringBuilder stringBuilder = new StringBuilder();

                int channelCount = int.MaxValue;
                stringBuilder.Append("channel\t");
                foreach (Series series in chart1.Series) {
                    if (series != null) {
                        stringBuilder.Append(series.name.Replace(" ", "_"));
                        stringBuilder.Append("\t");
                        if (channelCount > series.PointCount)
                            channelCount = series.PointCount;
                    }
                }
                stringBuilder.Append("differences\n");
                for (int i = 0; i < channelCount; i++) {
                    stringBuilder.Append(chart1.Series[0].points[i].X);
                    stringBuilder.Append("\t");
                    foreach (Series series in chart1.Series)
                        if (series != null) {
                            stringBuilder.Append(series.points[i].Y);
                            stringBuilder.Append("\t");
                        }
                    stringBuilder.Append(chart2.Series[0].points[i].Y);
                    stringBuilder.Append("\n");
                }
                TextWriter writer = new StreamWriter(Path.ChangeExtension(dialog.FileName, "txt"));
                writer.Write(stringBuilder);
            }
        }

        private void SpectrumFitForm_HelpRequested(object sender, HelpEventArgs hlpevent) {
            Help.ShowHelp(this.Parent, MainForm.helpfile, HelpNavigator.KeywordIndex, "Fit window");
        }

        private void chart1_Zoomed(object sender, ZoomEventArgs eventArgs) {
            chart2.Zoom(new RectangleF(
                eventArgs.rect.X, 
                chart2.YAxisMin,
                eventArgs.rect.Width,
                chart2.YAxisMax - chart2.YAxisMin));
        }

        private void chart1_ZoomReset(object sender, EventArgs e) {
            chart2.ResetZoom();
        }
    }
}
