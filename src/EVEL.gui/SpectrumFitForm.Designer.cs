namespace Evel.gui {
    partial class SpectrumFitForm {
        /// <summary>
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing) {
            if (disposing && (components != null)) {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        /// <summary>
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent() {
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(SpectrumFitForm));
            System.Windows.Forms.DataGridViewCellStyle dataGridViewCellStyle1 = new System.Windows.Forms.DataGridViewCellStyle();
            this.panel1 = new System.Windows.Forms.Panel();
            this.button2 = new System.Windows.Forms.Button();
            this.button1 = new System.Windows.Forms.Button();
            this.splitContainer1 = new System.Windows.Forms.SplitContainer();
            this.chart1 = new digit.Chart();
            this.chart2 = new digit.Chart();
            this.toolStrip1 = new System.Windows.Forms.ToolStrip();
            this.toolStripButton1 = new System.Windows.Forms.ToolStripButton();
            this.toolStripButton2 = new System.Windows.Forms.ToolStripButton();
            this.spectraSelector = new System.Windows.Forms.ToolStripComboBox();
            this.toolStripButton3 = new System.Windows.Forms.ToolStripButton();
            this.seriesGridPanel = new System.Windows.Forms.Panel();
            this.seriesGrid = new Evel.gui.DataGridParameterView();
            this.dataGridViewTextBoxColumn1 = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.isvisible = new System.Windows.Forms.DataGridViewCheckBoxColumn();
            this.change = new System.Windows.Forms.DataGridViewButtonColumn();
            this.panel2 = new System.Windows.Forms.Panel();
            this.Seriesname = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.panel1.SuspendLayout();
            this.splitContainer1.Panel1.SuspendLayout();
            this.splitContainer1.Panel2.SuspendLayout();
            this.splitContainer1.SuspendLayout();
            this.toolStrip1.SuspendLayout();
            this.seriesGridPanel.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.seriesGrid)).BeginInit();
            this.panel2.SuspendLayout();
            this.SuspendLayout();
            // 
            // panel1
            // 
            this.panel1.AutoSize = true;
            this.panel1.BorderStyle = System.Windows.Forms.BorderStyle.Fixed3D;
            this.panel1.Controls.Add(this.button2);
            this.panel1.Controls.Add(this.button1);
            this.panel1.Dock = System.Windows.Forms.DockStyle.Bottom;
            this.panel1.Location = new System.Drawing.Point(0, 446);
            this.panel1.Name = "panel1";
            this.panel1.Size = new System.Drawing.Size(666, 37);
            this.panel1.TabIndex = 5;
            this.panel1.Paint += new System.Windows.Forms.PaintEventHandler(this.panel1_Paint);
            // 
            // button2
            // 
            this.button2.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.button2.DialogResult = System.Windows.Forms.DialogResult.Retry;
            this.button2.FlatStyle = System.Windows.Forms.FlatStyle.Popup;
            this.button2.Location = new System.Drawing.Point(502, 5);
            this.button2.Margin = new System.Windows.Forms.Padding(1, 5, 1, 5);
            this.button2.Name = "button2";
            this.button2.Size = new System.Drawing.Size(75, 23);
            this.button2.TabIndex = 4;
            this.button2.Text = "Fit again";
            this.button2.UseVisualStyleBackColor = true;
            // 
            // button1
            // 
            this.button1.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.button1.DialogResult = System.Windows.Forms.DialogResult.OK;
            this.button1.FlatStyle = System.Windows.Forms.FlatStyle.Popup;
            this.button1.Location = new System.Drawing.Point(579, 5);
            this.button1.Margin = new System.Windows.Forms.Padding(1, 5, 1, 5);
            this.button1.Name = "button1";
            this.button1.Size = new System.Drawing.Size(75, 23);
            this.button1.TabIndex = 3;
            this.button1.Text = "Ok";
            this.button1.UseVisualStyleBackColor = true;
            // 
            // splitContainer1
            // 
            this.splitContainer1.BackColor = System.Drawing.SystemColors.Window;
            this.splitContainer1.Dock = System.Windows.Forms.DockStyle.Fill;
            this.splitContainer1.Location = new System.Drawing.Point(0, 0);
            this.splitContainer1.Name = "splitContainer1";
            this.splitContainer1.Orientation = System.Windows.Forms.Orientation.Horizontal;
            // 
            // splitContainer1.Panel1
            // 
            this.splitContainer1.Panel1.Controls.Add(this.chart1);
            // 
            // splitContainer1.Panel2
            // 
            this.splitContainer1.Panel2.Controls.Add(this.chart2);
            this.splitContainer1.Size = new System.Drawing.Size(640, 408);
            this.splitContainer1.SplitterDistance = 261;
            this.splitContainer1.SplitterWidth = 1;
            this.splitContainer1.TabIndex = 6;
            // 
            // chart1
            // 
            this.chart1.AxesExtrema = ((digit.AxesExtrema)((((digit.AxesExtrema.AutoMinX | digit.AxesExtrema.AutoMaxX)
                        | digit.AxesExtrema.AutoMinY)
                        | digit.AxesExtrema.AutoMaxY)));
            this.chart1.Dock = System.Windows.Forms.DockStyle.Fill;
            this.chart1.Location = new System.Drawing.Point(0, 0);
            this.chart1.LogarythmicY = true;
            this.chart1.Name = "chart1";
            this.chart1.Padding = new System.Windows.Forms.Padding(10, 10, 10, 0);
            this.chart1.Size = new System.Drawing.Size(640, 261);
            this.chart1.TabIndex = 0;
            this.chart1.TicksWidth = 20;
            this.chart1.Title = "Chart";
            this.chart1.TitleFont = new System.Drawing.Font("Microsoft Sans Serif", 8.25F, System.Drawing.FontStyle.Bold);
            this.chart1.VisibleElements = ((digit.ChartElements)((((((digit.ChartElements.Title | digit.ChartElements.YTitle)
                        | digit.ChartElements.XAxis)
                        | digit.ChartElements.YAxis)
                        | digit.ChartElements.Grid)
                        | digit.ChartElements.Series)));
            this.chart1.XAxisMax = 6.28F;
            this.chart1.XAxisMin = 0F;
            this.chart1.XAxisTitle = "Time [ns]";
            this.chart1.YAxisMax = 1F;
            this.chart1.YAxisMin = -1F;
            this.chart1.YAxisTitle = "Counts";
            this.chart1.Zoomable = true;
            this.chart1.ZoomReset += new System.EventHandler(this.chart1_ZoomReset);
            this.chart1.Zoomed += new digit.ZoomEventHandler(this.chart1_Zoomed);
            // 
            // chart2
            // 
            this.chart2.AxesExtrema = ((digit.AxesExtrema)((((digit.AxesExtrema.AutoMinX | digit.AxesExtrema.AutoMaxX)
                        | digit.AxesExtrema.AutoMinY)
                        | digit.AxesExtrema.AutoMaxY)));
            this.chart2.Dock = System.Windows.Forms.DockStyle.Fill;
            this.chart2.Location = new System.Drawing.Point(0, 0);
            this.chart2.Name = "chart2";
            this.chart2.Padding = new System.Windows.Forms.Padding(10, 0, 10, 10);
            this.chart2.Size = new System.Drawing.Size(640, 146);
            this.chart2.TabIndex = 0;
            this.chart2.TicksWidth = 20;
            this.chart2.Title = "Residuals";
            this.chart2.TitleFont = new System.Drawing.Font("Microsoft Sans Serif", 8.25F, System.Drawing.FontStyle.Bold);
            this.chart2.VisibleElements = ((digit.ChartElements)((((((digit.ChartElements.XTitle | digit.ChartElements.YTitle)
                        | digit.ChartElements.XAxis)
                        | digit.ChartElements.YAxis)
                        | digit.ChartElements.Grid)
                        | digit.ChartElements.Series)));
            this.chart2.XAxisMax = 6.28F;
            this.chart2.XAxisMin = 0F;
            this.chart2.XAxisTitle = "Time [ns]";
            this.chart2.YAxisMax = 1F;
            this.chart2.YAxisMin = -1F;
            this.chart2.YAxisTitle = "";
            this.chart2.Zoomable = false;
            // 
            // toolStrip1
            // 
            this.toolStrip1.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.toolStripButton1,
            this.toolStripButton2,
            this.spectraSelector,
            this.toolStripButton3});
            this.toolStrip1.Location = new System.Drawing.Point(0, 0);
            this.toolStrip1.Name = "toolStrip1";
            this.toolStrip1.Size = new System.Drawing.Size(666, 25);
            this.toolStrip1.TabIndex = 7;
            this.toolStrip1.Text = "toolStrip1";
            // 
            // toolStripButton1
            // 
            this.toolStripButton1.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
            this.toolStripButton1.Image = global::Evel.gui.Properties.Resources.savepic;
            this.toolStripButton1.ImageTransparentColor = System.Drawing.Color.Black;
            this.toolStripButton1.Name = "toolStripButton1";
            this.toolStripButton1.Size = new System.Drawing.Size(23, 22);
            this.toolStripButton1.Text = "toolStripButton1";
            this.toolStripButton1.ToolTipText = "Save fit to bitmap";
            this.toolStripButton1.Click += new System.EventHandler(this.toolStripButton1_Click);
            // 
            // toolStripButton2
            // 
            this.toolStripButton2.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
            this.toolStripButton2.Image = global::Evel.gui.Properties.Resources.savetabs;
            this.toolStripButton2.ImageTransparentColor = System.Drawing.Color.Black;
            this.toolStripButton2.Name = "toolStripButton2";
            this.toolStripButton2.Size = new System.Drawing.Size(23, 22);
            this.toolStripButton2.Text = "Save series values to tab separated file";
            this.toolStripButton2.Click += new System.EventHandler(this.toolStripButton2_Click);
            // 
            // spectraSelector
            // 
            this.spectraSelector.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.spectraSelector.MaxDropDownItems = 15;
            this.spectraSelector.Name = "spectraSelector";
            this.spectraSelector.Size = new System.Drawing.Size(200, 25);
            this.spectraSelector.SelectedIndexChanged += new System.EventHandler(this.spectraSelector_SelectedIndexChanged);
            // 
            // toolStripButton3
            // 
            this.toolStripButton3.CheckOnClick = true;
            this.toolStripButton3.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Text;
            this.toolStripButton3.Image = ((System.Drawing.Image)(resources.GetObject("toolStripButton3.Image")));
            this.toolStripButton3.ImageTransparentColor = System.Drawing.Color.Magenta;
            this.toolStripButton3.Name = "toolStripButton3";
            this.toolStripButton3.Size = new System.Drawing.Size(40, 22);
            this.toolStripButton3.Text = "Series";
            this.toolStripButton3.Click += new System.EventHandler(this.toolStripButton3_Click);
            // 
            // seriesGridPanel
            // 
            this.seriesGridPanel.BorderStyle = System.Windows.Forms.BorderStyle.Fixed3D;
            this.seriesGridPanel.Controls.Add(this.seriesGrid);
            this.seriesGridPanel.Location = new System.Drawing.Point(254, 28);
            this.seriesGridPanel.Name = "seriesGridPanel";
            this.seriesGridPanel.Padding = new System.Windows.Forms.Padding(10);
            this.seriesGridPanel.Size = new System.Drawing.Size(274, 42);
            this.seriesGridPanel.TabIndex = 1;
            this.seriesGridPanel.Visible = false;
            // 
            // seriesGrid
            // 
            this.seriesGrid.AllowUserToAddRows = false;
            this.seriesGrid.AllowUserToDeleteRows = false;
            this.seriesGrid.AllowUserToResizeColumns = false;
            this.seriesGrid.AllowUserToResizeRows = false;
            this.seriesGrid.BackgroundColor = System.Drawing.SystemColors.ButtonFace;
            this.seriesGrid.BorderStyle = System.Windows.Forms.BorderStyle.None;
            this.seriesGrid.CellBorderStyle = System.Windows.Forms.DataGridViewCellBorderStyle.Raised;
            dataGridViewCellStyle1.Alignment = System.Windows.Forms.DataGridViewContentAlignment.MiddleCenter;
            dataGridViewCellStyle1.BackColor = System.Drawing.SystemColors.Control;
            dataGridViewCellStyle1.Font = new System.Drawing.Font("Microsoft Sans Serif", 8.25F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(238)));
            dataGridViewCellStyle1.ForeColor = System.Drawing.SystemColors.WindowText;
            dataGridViewCellStyle1.SelectionBackColor = System.Drawing.SystemColors.Highlight;
            dataGridViewCellStyle1.SelectionForeColor = System.Drawing.SystemColors.HighlightText;
            dataGridViewCellStyle1.WrapMode = System.Windows.Forms.DataGridViewTriState.True;
            this.seriesGrid.ColumnHeadersDefaultCellStyle = dataGridViewCellStyle1;
            this.seriesGrid.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            this.seriesGrid.Columns.AddRange(new System.Windows.Forms.DataGridViewColumn[] {
            this.dataGridViewTextBoxColumn1,
            this.isvisible,
            this.change});
            this.seriesGrid.Dock = System.Windows.Forms.DockStyle.Fill;
            this.seriesGrid.FixedCols = 1;
            this.seriesGrid.Location = new System.Drawing.Point(10, 10);
            this.seriesGrid.Name = "seriesGrid";
            this.seriesGrid.ReadonlyCellBackColor = System.Drawing.SystemColors.ButtonFace;
            this.seriesGrid.RowHeadersVisible = false;
            this.seriesGrid.ScrollBars = System.Windows.Forms.ScrollBars.Vertical;
            this.seriesGrid.Size = new System.Drawing.Size(250, 18);
            this.seriesGrid.TabIndex = 0;
            this.seriesGrid.CurrentCellDirtyStateChanged += new System.EventHandler(this.seriesGrid_CurrentCellDirtyStateChanged);
            this.seriesGrid.CellContentClick += new System.Windows.Forms.DataGridViewCellEventHandler(this.seriesGrid_CellContentClick);
            // 
            // dataGridViewTextBoxColumn1
            // 
            this.dataGridViewTextBoxColumn1.AutoSizeMode = System.Windows.Forms.DataGridViewAutoSizeColumnMode.None;
            this.dataGridViewTextBoxColumn1.HeaderText = "Series name";
            this.dataGridViewTextBoxColumn1.Name = "dataGridViewTextBoxColumn1";
            this.dataGridViewTextBoxColumn1.ReadOnly = true;
            this.dataGridViewTextBoxColumn1.Resizable = System.Windows.Forms.DataGridViewTriState.False;
            this.dataGridViewTextBoxColumn1.SortMode = System.Windows.Forms.DataGridViewColumnSortMode.NotSortable;
            this.dataGridViewTextBoxColumn1.Width = 160;
            // 
            // isvisible
            // 
            this.isvisible.HeaderText = "Visible";
            this.isvisible.Name = "isvisible";
            this.isvisible.Resizable = System.Windows.Forms.DataGridViewTriState.False;
            this.isvisible.Width = 50;
            // 
            // change
            // 
            this.change.HeaderText = "Color";
            this.change.Name = "change";
            this.change.Text = "Color";
            this.change.Width = 40;
            // 
            // panel2
            // 
            this.panel2.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom)
                        | System.Windows.Forms.AnchorStyles.Left)
                        | System.Windows.Forms.AnchorStyles.Right)));
            this.panel2.BorderStyle = System.Windows.Forms.BorderStyle.Fixed3D;
            this.panel2.Controls.Add(this.splitContainer1);
            this.panel2.Location = new System.Drawing.Point(12, 28);
            this.panel2.Name = "panel2";
            this.panel2.Size = new System.Drawing.Size(644, 412);
            this.panel2.TabIndex = 8;
            // 
            // Seriesname
            // 
            this.Seriesname.AutoSizeMode = System.Windows.Forms.DataGridViewAutoSizeColumnMode.None;
            this.Seriesname.HeaderText = "Series name";
            this.Seriesname.Name = "Seriesname";
            this.Seriesname.ReadOnly = true;
            this.Seriesname.Resizable = System.Windows.Forms.DataGridViewTriState.False;
            this.Seriesname.SortMode = System.Windows.Forms.DataGridViewColumnSortMode.NotSortable;
            this.Seriesname.Width = 150;
            // 
            // SpectrumFitForm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(666, 483);
            this.Controls.Add(this.seriesGridPanel);
            this.Controls.Add(this.panel2);
            this.Controls.Add(this.toolStrip1);
            this.Controls.Add(this.panel1);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog;
            this.Name = "SpectrumFitForm";
            this.ShowIcon = false;
            this.ShowInTaskbar = false;
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
            this.Text = "Fit";
            this.TopMost = true;
            this.HelpRequested += new System.Windows.Forms.HelpEventHandler(this.SpectrumFitForm_HelpRequested);
            this.panel1.ResumeLayout(false);
            this.splitContainer1.Panel1.ResumeLayout(false);
            this.splitContainer1.Panel2.ResumeLayout(false);
            this.splitContainer1.ResumeLayout(false);
            this.toolStrip1.ResumeLayout(false);
            this.toolStrip1.PerformLayout();
            this.seriesGridPanel.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.seriesGrid)).EndInit();
            this.panel2.ResumeLayout(false);
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.Panel panel1;
        private System.Windows.Forms.Button button1;
        private System.Windows.Forms.Button button2;
        private System.Windows.Forms.SplitContainer splitContainer1;
        private System.Windows.Forms.ToolStrip toolStrip1;
        private System.Windows.Forms.ToolStripButton toolStripButton1;
        private System.Windows.Forms.ToolStripButton toolStripButton2;
        private System.Windows.Forms.ToolStripComboBox spectraSelector;
        private System.Windows.Forms.ToolStripButton toolStripButton3;
        private System.Windows.Forms.Panel seriesGridPanel;
        private DataGridParameterView seriesGrid;
        private System.Windows.Forms.DataGridViewTextBoxColumn Seriesname;
        private System.Windows.Forms.DataGridViewTextBoxColumn dataGridViewTextBoxColumn1;
        private System.Windows.Forms.DataGridViewCheckBoxColumn isvisible;
        private System.Windows.Forms.DataGridViewButtonColumn change;
        private digit.Chart chart1;
        private digit.Chart chart2;
        private System.Windows.Forms.Panel panel2;
    }
}