using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;
using Evel.interfaces;
//using chartings;
using digit;

namespace Evel.gui {

    public delegate double SetDefaultValueHandler(ISpectrum spectrum, IParameter parameter);

    public partial class DefaultAdjusterGUI : Form {

        private List<ValueAdjuster> _adjusters;
        //private ISpectraContainer _container;
        private List<ISpectrum> _spectra;
        private ISpectrum _spectrum;
        public SetDefaultValueHandler SetDefaultValue;
        private Dictionary<MouseButtons, ValueAdjuster> _mouseAdjusters;
        private PointSeries pseries;
        private bool zoomClick;

        public DefaultAdjusterGUI(List<ISpectrum> spectra, ISpectrum adjustingSpectrum) {
            InitializeComponent();
            _adjusters = new List<ValueAdjuster>();
            _mouseAdjusters = new Dictionary<MouseButtons, ValueAdjuster>();
            //this._container = container;
            this._spectra = spectra;
            this._spectrum = adjustingSpectrum;
            this.pseries = new PointSeries(adjustingSpectrum.DataLength, adjustingSpectrum.Name, chart1, Color.Red, 1);
            //chartings.ISeries series = chart1.Series["Series1"];
            //series.Clear();
            for (int ch = adjustingSpectrum.BufferStartPos; ch <= adjustingSpectrum.BufferEndPos; ch++) {
                pseries.AddPoint(ch - adjustingSpectrum.BufferStartPos, adjustingSpectrum.Container.Data[ch], false);
            }
            this.zoomClick = false;
            chart1.AddSeries(pseries);
            chart1.Title = adjustingSpectrum.Name;
            chart1.Invalidate();
        }

        public ValueAdjuster AddAdjuster(ValueCoordinates value, Orientation orientation) {
            ValueAdjuster adjuster = new ValueAdjuster(value, orientation, new EventHandler(RearangeAdjusters), this.SetDefaultValue, this.chart1);
            adjuster.Parent = toolsArea;
            _adjusters.Add(adjuster);
            RearangeAdjusters(null, null);
            return adjuster;
        }

        public ValueAdjuster AddAdjuster(ValueCoordinates value, Orientation orientation, MouseButtons mouseButton) {
            ValueAdjuster adjuster = AddAdjuster(value, orientation);
            this._mouseAdjusters.Add(mouseButton, adjuster);
            return adjuster;
        }

        private void RearangeAdjusters(object sender, EventArgs args) {
            int position = 6;
            foreach (ValueAdjuster adjuster in _adjusters) {
                adjuster.Location = new Point(position, 6);
                position += adjuster.Size.Width + 6;
            }
        }

        private void button2_Click(object sender, EventArgs e) {

        }

        private void panel2_Paint(object sender, PaintEventArgs e) {
            MainForm.DialogPanel_Paint(sender, e);
        }

        private void button3_Click(object sender, EventArgs e) {
            foreach (ValueAdjuster adjuster in _adjusters) {
                adjuster.ValueProperties.parameter.Value = adjuster.BackupValue;
            }
        }

        private void OkButton_Click(object sender, EventArgs e) {
            foreach (ValueAdjuster adjuster in _adjusters) {
                if (adjuster.ApplyForAll) {
                    foreach (ISpectrum spectrum in _spectra) {
                        if (spectrum == this._spectrum) continue;
                        string groupName = adjuster.ValueProperties.groupName;
                        int compId = adjuster.ValueProperties.componentId;
                        string parameterName = adjuster.ValueProperties.parameter.Definition.Name;
                        spectrum.Parameters[groupName].Components[compId][parameterName].Value = this._spectrum.Parameters[groupName].Components[compId][parameterName].Value;
                    }
                }
            }
        }

        private void chart1_MouseClick(object sender, MouseEventArgs e) {
            if (_mouseAdjusters.ContainsKey(e.Button) && !this.zoomClick) {
                //float value = 0;
                PointF sc = new PointF();
                ((Chart)sender).Screen2Axis(e.Location, ref sc);
                //((Chart)sender).Axes["BottomAxis1"].ScreenToValue(e.X, ref value);
                _mouseAdjusters[e.Button].ToolBox.Value = Math.Round(sc.X);
                chart1.Invalidate();
            }
        }

        private void chart1_Paint(object sender, PaintEventArgs e) {
            PointF p = new PointF();
            int v;
            Rectangle gridRect = chart1.GridRect;
            foreach (ValueAdjuster va in this._adjusters) {
                if (!va.Minimized) {

                    switch (va.ToolBox.Orientation) {
                        case Orientation.Vertical:
                            chart1.Axis2Screen(new PointF((float)va.ToolBox.Value, 0.0f), ref p);
                            if ((v = (int)p.X + gridRect.Left) > gridRect.Left && v < gridRect.Right)
                                e.Graphics.DrawLine(Pens.Black,
                                    v,
                                    gridRect.Top + 1,
                                    v,
                                    gridRect.Bottom - 1);
                            break;
                        case Orientation.Horizontal:
                            chart1.Axis2Screen(new PointF(0.0f, (float)va.ToolBox.Value), ref p);
                            if ((v = (int)p.Y + gridRect.Top) < gridRect.Bottom && v > gridRect.Top)
                                e.Graphics.DrawLine(Pens.Black,
                                    gridRect.Left + 1,
                                    p.Y + gridRect.Top,
                                    gridRect.Right - 1,
                                    p.Y + gridRect.Top);

                            break;
                    }
                }
            }
        }

        private void chart1_MouseMove(object sender, MouseEventArgs e) {
            if (e.Button != MouseButtons.None)
                this.zoomClick = true;
        }

        private void chart1_MouseUp(object sender, MouseEventArgs e) {
            this.zoomClick = false;
        }

    }

    public struct ValueCoordinates {
        public string groupName;
        public int componentId;
        public double minValue;
        public double maxValue;
        public IParameter parameter;
        public ISpectrum spectrum;
    }

    public class ValueAdjuster {

        private Orientation _orientation;
        private ValueCoordinates _value;
        private ValueToolBox toolBox;
        private double _backupValue;

        public ValueAdjuster(ValueCoordinates value, Orientation orientation, EventHandler hideEventHandler, SetDefaultValueHandler setDefaultValueHandler, Chart chart) {
            this._value = value;
            this._backupValue = value.parameter.Value;
            this._orientation = orientation;
            toolBox = new ValueToolBox(hideEventHandler, value.minValue, value.maxValue, value.spectrum, 
                value.parameter, setDefaultValueHandler, chart, orientation);
            toolBox.Text = _value.parameter.Definition.Name;
            toolBox.Minimized = false;
        }

        public double BackupValue {
            get { return this._backupValue; }
        }

        public ValueCoordinates ValueProperties {
            get { return this._value; }
        }

        public bool ApplyForAll {
            get { return this.toolBox.ApplyForAll; }
        }

        public Point Location {
            get { return this.toolBox.Location; }
            set { this.toolBox.Location = value; }
        }

        public Size Size {
            get { return this.toolBox.Size; }
            set { this.toolBox.Size = value; }
        }

        public Control Parent {
            get { return toolBox.Parent; }
            set { toolBox.Parent = value; }
        }

        //public Panel SplitPanel {
        //    get { return toolBox.splitPanel; }
        //}

        public bool Minimized {
            get { return toolBox.Minimized; }
            set { toolBox.Minimized = value; }
        }

        public ValueToolBox ToolBox {
            get { return this.toolBox; }
        }
    }

    public class ValueToolBox : TabControl {

        Button hideButton;
        Button resetButton;
        CheckBox applyForAll;
        HScrollBar scrollBar;
        TextBox textBox;
        Panel toolBoxPanel;
        bool _minimized;
        IParameter parameter;
        int minimum;
        int maximum;
        SetDefaultValueHandler setDefaultValue;
        ISpectrum spectrum;
        Chart chart;
        //Series series;
        Orientation orientation;
        //public Panel splitPanel;

        public ValueToolBox(EventHandler hideEventHandler, double minimum, double maximum, 
            ISpectrum spectrum, IParameter parameter, SetDefaultValueHandler setDefaultValue, Chart chart,
            Orientation orientation)
            : base() {
            this.parameter = parameter;
            this.chart = chart;
            this.maximum = (int)maximum;
            this.minimum = (int)minimum;
            this.spectrum = spectrum;
            this.orientation = orientation;
            this.setDefaultValue = setDefaultValue;
            //series
            //series = new Series(2, parameter.Definition.Name, chart, Color.Black);
            
            //series.visible = false;
            //series.Color = Color.Black;
            //series.Width
            //series.ParentChart = chart;
            //chart.AddSeries(series);
            //initialization of components
            Initialize();
            hideButton.Click += hideEventHandler;
            
            if (scrollBar != null) {
                scrollBar.Maximum = this.maximum;
                scrollBar.Minimum = this.minimum;
               
            }
            textBox.Text = parameter.Value.ToString("G5");
            if (scrollBar != null)
                scrollBar.Value = (int)parameter.Value;  
        }

        public Orientation Orientation {
            get { return this.orientation; }
        }

        public double Value {
            get { return this.parameter.Value; }
            set {
                this.parameter.Value = value;

                if (scrollBar != null) {
                    if (scrollBar.Value != (int)value)
                        try {
                            scrollBar.Value = (int)value;
                        } catch {
                            this.Value = scrollBar.Value;
                            return;
                        }
                }
                if (textBox.Text != value.ToString("G5"))
                    textBox.Text = value.ToString("G5");
                //updateSeries();
                chart.Invalidate();
            }
        }

        //void updateSeries() {
        //    series.Clear();

        //    switch (this.orientation) {
        //        case Orientation.Vertical:
        //            series.AddPoint(
        //                (float)this.Value,
        //                chart.YAxisMax, false);
        //                //series.VerticalAxis.Maximum);
        //            series.AddPoint(
        //                (float)this.Value,
        //                chart.YAxisMin, false);
        //                //series.VerticalAxis.Minimum);
        //            //series.HorizontalAxis.ValueToScreen((float)this.Value, ref x);
        //            break;
        //        case Orientation.Horizontal:
        //            series.AddPoint(
        //                //series.HorizontalAxis.Minimum,
        //                chart.XAxisMin,
        //                (float)this.Value, false);
        //            series.AddPoint(
        //                //series.HorizontalAxis.Maximum,
        //                chart.XAxisMin,
        //                (float)this.Value, false);
        //            //series.VerticalAxis.ValueToScreen((float)this.Value, ref y);
        //            break;
        //    }
            
        //    chart.Refresh();
        //    //location of splitter
        //    //splitPanel.Location = new Point(x, y);
        //}

        public bool ApplyForAll {
            get { return this.applyForAll.Checked; }
        }

        public new string Text {
            get { return this.TabPages[0].Text; }
            set { this.TabPages[0].Text = value; }
        }

        public void Initialize() {
            this.TabPages.Add("");
            this.Font = new Font(this.Font, FontStyle.Bold);
            ////splitPanel
            //splitPanel = new Panel();
            //splitPanel.BorderStyle = BorderStyle.FixedSingle;
            //splitPanel.BackColor = chart.BackColor;
            //if (orientation == Orientation.Horizontal) {
            //    splitPanel.Cursor = Cursors.HSplit;
            //    splitPanel.Size = new Size(40, 7);
            //} else {
            //    splitPanel.Cursor = Cursors.VSplit;
            //    splitPanel.Size = new Size(7, 40);
            //}
            //splitPanel.MouseMove += new MouseEventHandler(splitPanel_MouseMove);
            //splitPanel.Parent = chart;
            //splitPanel.Location = new Point(-20, -20);
            //splitPanel.Visible = false;
            //splitPanel.VisibleChanged += new EventHandler(splitPanel_VisibleChanged);
            //toolBoxPanel
            toolBoxPanel = new Panel();
            toolBoxPanel.Size = new Size(120, 80);
            toolBoxPanel.Dock = DockStyle.Bottom;
            toolBoxPanel.Parent = this.TabPages[0];
            this.toolBoxPanel.Font = new Font(this.Font, FontStyle.Regular);
            //hideButton
            hideButton = new Button();
            hideButton.Click += new EventHandler(hideButton_Click);
            hideButton.Size = new Size(16, 60);
            hideButton.Parent = toolBoxPanel;
            hideButton.Dock = DockStyle.Right;
            hideButton.Font = this.Font;
            //resetButton
            if (setDefaultValue != null) {
                resetButton = new Button();
                resetButton.Size = new Size(94, 22);
                resetButton.Location = new Point(11, 57);
                resetButton.Text = "Reset";
                resetButton.Parent = toolBoxPanel;
                resetButton.Click += new EventHandler(resetButton_Click);
            }
            //applyForAll
            applyForAll = new CheckBox();
            applyForAll.Text = "Apply for all";
            applyForAll.Size = new Size(80, 17);
            applyForAll.Location = new Point(11, 0);
            applyForAll.Parent = toolBoxPanel;
            applyForAll.Checked = orientation == Orientation.Vertical;
            //scrollBar
            if (minimum != maximum) {
                scrollBar = new HScrollBar();
                scrollBar.Size = new Size(94, 10);
                scrollBar.Location = new Point(11, 19);
                scrollBar.Parent = toolBoxPanel;
                scrollBar.ValueChanged += new EventHandler(scrollBar_ValueChanged);
            }
            //textBox
            textBox = new TextBox();
            textBox.Size = new Size(94, 20);
            textBox.Location = new Point(11, 31);
            textBox.Parent = toolBoxPanel;
            textBox.BringToFront();
            textBox.TextChanged += new EventHandler(textBox_TextChanged);
            Minimized = true;
        }

        //void splitPanel_MouseMove(object sender, MouseEventArgs e) {
        //    if ((e.Button & MouseButtons.Left) == MouseButtons.Left) {
        //        switch (orientation) {
        //            case Orientation.Horizontal:
        //                splitPanel.Top = e.Y-2;
        //                float y = (float)this.Value;
        //                series.VerticalAxis.ScreenToValue(e.Y - 2, ref y);
        //                this.Value = y;
        //                break;
        //            case Orientation.Vertical:
        //                float x = (float)this.Value;
        //                splitPanel.Left = e.X-2;
        //                series.HorizontalAxis.ScreenToValue(e.X - 2, ref x);
        //                this.Value = x;
        //                break;
        //        }
        //    }
        //}



        //void splitPanel_VisibleChanged(object sender, EventArgs e) {
        //    int x = chart.ClientRectangle.Width / 2 - 10;
        //    int y = chart.ClientRectangle.Height / 2 - 10;
        //    switch (this.orientation) {
        //        case Orientation.Vertical:
        //            series.HorizontalAxis.ValueToScreen((float)this.Value, ref x);
        //            x -= 3;
        //            break;
        //        case Orientation.Horizontal:
        //            series.VerticalAxis.ValueToScreen((float)this.Value, ref y);
        //            y -= 3;
        //            break;
        //    }
        //    splitPanel.Location = new Point(x, y);
        //}

        void scrollBar_ValueChanged(object sender, EventArgs e) {
            this.Value = ((HScrollBar)sender).Value;
        }

        void textBox_TextChanged(object sender, EventArgs e) {
            this.Value = Double.Parse(((TextBox)sender).Text);
        }

        void resetButton_Click(object sender, EventArgs e) {
            this.Value = setDefaultValue(this.spectrum, this.parameter);
        }

        void hideButton_Click(object sender, EventArgs e) {
            Minimized = !Minimized;
        }

        public bool Minimized {
            get { return this._minimized; }
            set {
                this._minimized = value;
                //this.series.visible = !value;
                if (value) {
                    hideButton.Text = ">";
                    this.Size = new Size(45, 111);
                    this.Alignment = TabAlignment.Left;

                } else {
                    hideButton.Text = "<";
                    this.Size = new Size(140, 111);
                    this.Alignment = TabAlignment.Top;
                }
                foreach (Control control in toolBoxPanel.Controls)
                    if (control != hideButton)
                        control.Visible = !value;


                chart.Invalidate();
            }
        }

        

    }

}
