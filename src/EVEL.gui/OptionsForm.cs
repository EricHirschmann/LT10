 using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Text;
using System.Windows.Forms;
using Evel.engine;
using Evel.interfaces;
using System.Collections;
using Evel.gui.interfaces;

namespace Evel.gui {
    public partial class OptionsForm : Form {

        private Hashtable BackupStatusColors;
        List<CellParameterStatus> optionsStatusesSource;
        private Lt10Updater _updater = null;

        public OptionsForm() {
            InitializeComponent();
            optionsStatusesSource = new List<CellParameterStatus>(DefaultGroupGUI.StatusesSource);
            optionsStatusesSource.Add(new CellParameterStatus(ParameterStatus.Binding | ParameterStatus.Free));
            optionsStatusesSource.Add(new CellParameterStatus(ParameterStatus.Binding | ParameterStatus.Fixed));
            BackupStatusColors = new Hashtable(MainForm.StatusColors);
            SetGrid();
            SetStatusesGrid();
            maxRecentProjectUpDown.Value = MainForm.maxRecentProjects;
            checkBox1.Checked = MainForm.switchToSearch;
            switch (MainForm.savebeforefitting) {
                case 0: radioButton1.Checked = true; break;
                case 1: radioButton2.Checked = true; break;
                case 2: radioButton3.Checked = true; break;
            }
            txtProjectsPath.Text = MainForm.ProjectsPath;
            txtSpectraPath.Text = MainForm.CommonSpectraPath;
            if (restoresCB.Checked = restoresNUD.Enabled = MainForm.parameterRestoreCount > 0)
                restoresNUD.Value = MainForm.parameterRestoreCount;
            updatesCB.Checked = !"".Equals(MainForm.uversion);
        }

        private void SetStatusesGrid() {
            foreach (ParameterStatus status in BackupStatusColors.Keys) {
                statusesGrid.Rows.Add();
                statusesGrid.Rows[statusesGrid.Rows.Count - 1].HeaderCell.Value = status;
                statusesGrid.Rows[statusesGrid.Rows.Count - 1].Cells[0].Value = status.ToString().Replace(", ", " ");
                statusesGrid.Rows[statusesGrid.Rows.Count - 1].Cells[0].Style.BackColor = System.Drawing.SystemColors.ControlDark;
                statusesGrid.Rows[statusesGrid.Rows.Count - 1].Cells[1].Value = MainForm.GetColor(status).ToArgb().ToString("X");
                statusesGrid.Rows[statusesGrid.Rows.Count - 1].Cells[2].Value = "Change";
            }
            repaintGrids();
        }

        private void repaintGrids() {
            grid.Invalidate();
            foreach (DataGridViewRow row in statusesGrid.Rows) {
                row.Cells[0].Style.ForeColor = MainForm.GetColor((ParameterStatus)row.HeaderCell.Value);
                row.Cells[0].Style.SelectionForeColor = MainForm.GetColor((ParameterStatus)row.HeaderCell.Value);
            }
            foreach (DataGridViewCell cell in grid.Rows[0].Cells) {
                ParameterStatus status = (ParameterStatus)(Int32.Parse(cell.Value.ToString()));
                cell.Style.ForeColor = MainForm.GetColor(status);
                cell.Style.SelectionForeColor = MainForm.GetColor(status);
            }
        }

        private void SetGrid() {
            int statusSpread = 2;
            grid.ColumnCount = statusSpread * 4 + 2;
            //grid
            ParameterDefinition[] componentDefinition = new ParameterDefinition[] {
                new ParameterDefinition("u1"),
                new ParameterDefinition("u2"),
                new ParameterDefinition("u3"),
                new ParameterDefinition("u4"),
                new ParameterDefinition("u5"),
                new ParameterDefinition("u6"),
                new ParameterDefinition("u7"),
                new ParameterDefinition("u8"),
                new ParameterDefinition("u9"),
                new ParameterDefinition("u10"),
                new ParameterDefinition("u11"),
                new ParameterDefinition("u10")};
            for (int r = 0; r < 5; r++) {
                DataGridViewRow row = new DataGridViewRow();
                Evel.engine.Component comp = new Evel.engine.Component(componentDefinition, false, false);
                int parameterId = 0;
                foreach (ParameterStatus status in MainForm.StatusColors.Keys) {
                    for (int i = 0; i < statusSpread - ((status & ParameterStatus.Binding) > 0 ? 1 : 0); i++) {
                        DataGridViewCell cell;
                        if (r == 0) {
                            cell = new DataGridViewComboBoxCell();
                            ((DataGridViewComboBoxCell)cell).DataSource = optionsStatusesSource;
                            ((DataGridViewComboBoxCell)cell).ValueMember = "ValueMember";
                            ((DataGridViewComboBoxCell)cell).DisplayMember = "DisplayMember";
                            cell.Value = ((int)status).ToString();
                            cell.Style.BackColor = System.Drawing.SystemColors.ControlDark;
                            cell.Style.SelectionBackColor = System.Drawing.SystemColors.ControlDark;
                            cell.Style.Font = new Font(grid.Font, FontStyle.Bold);
                        } else {
                            IParameter parameter = comp[parameterId++];
                            parameter.Status = status;
                            cell = new DataGridViewParameterCell(parameter);
                            cell.Value = 0.2;
                            
                        }
                        row.Cells.Add(cell);
                    }
                }
                grid.Rows.Add(row);
            }

            //for (int c = 0; c < grid.ColumnCount; c++)
            //    grid.Columns[c].Width = 70;
        }

        private void panel1_Paint(object sender, PaintEventArgs e) {
            MainForm.DialogPanel_Paint(sender, e);
        }

        private void statusesGrid_CellValueChanged(object sender, DataGridViewCellEventArgs e) {
            if (e.RowIndex == -1 || e.ColumnIndex == -1) return;
            if (e.ColumnIndex == 1) {
                string colorHex = statusesGrid[e.ColumnIndex, e.RowIndex].Value.ToString();
                try {
                    Color color = Color.FromArgb(Int32.Parse(colorHex, System.Globalization.NumberStyles.AllowHexSpecifier));
                    MainForm.StatusColors.Remove(statusesGrid.Rows[e.RowIndex].HeaderCell.Value);
                    MainForm.StatusColors.Add(statusesGrid.Rows[e.RowIndex].HeaderCell.Value, color);
                    repaintGrids();
                } catch {
                    MessageBox.Show(String.Format("\"{0}\" is not a valid ARGB notation", colorHex), "LT10", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    statusesGrid[e.ColumnIndex, e.RowIndex].Value = MainForm.GetColor((ParameterStatus)statusesGrid.Rows[e.RowIndex].HeaderCell.Value).ToArgb().ToString("X");
                }
            }
        }

        private void statusesGrid_CellContentClick(object sender, DataGridViewCellEventArgs e) {
            if (statusesGrid[e.ColumnIndex, e.RowIndex] is DataGridViewButtonCell) {
                ColorDialog dialog = new ColorDialog();
                dialog.FullOpen = true;
                dialog.Color = MainForm.GetColor((ParameterStatus)statusesGrid.Rows[e.RowIndex].HeaderCell.Value);
                if (dialog.ShowDialog() == DialogResult.OK) {
                    statusesGrid[1, e.RowIndex].Value = dialog.Color.ToArgb().ToString("X");
                }
            }
        }

        private void button1_Click(object sender, EventArgs e) {
            MainForm.maxRecentProjects = (int)maxRecentProjectUpDown.Value;
            MainForm.savebeforefitting = (byte)(((radioButton2.Checked) ? 1 : 0) + ((radioButton3.Checked) ? 2 : 0));
            MainForm.switchToSearch = checkBox1.Checked;
            MainForm.ProjectsPath = txtProjectsPath.Text;
            MainForm.CommonSpectraPath = txtSpectraPath.Text;
            MainForm.parameterRestoreCount = (restoresCB.Checked) ? (short)restoresNUD.Value : (short)0;
            MainForm.uversion = updatesCB.Checked ? System.Reflection.Assembly.GetExecutingAssembly().GetName().Version.ToString() : "";
        }

        private void button2_Click(object sender, EventArgs e) {
            MainForm.StatusColors.Clear();
            foreach (ParameterStatus status in BackupStatusColors.Keys)
                MainForm.StatusColors.Add(status, BackupStatusColors[status]);
        }

        private void BrowseFolder_Click(object sender, EventArgs e) {
            FolderBrowserDialog dialog = new FolderBrowserDialog();
            dialog.SelectedPath = (sender == btnProjects) ? txtProjectsPath.Text : txtSpectraPath.Text;
            if (dialog.ShowDialog() == DialogResult.OK) {
                if (sender == btnProjects)
                    txtProjectsPath.Text = dialog.SelectedPath;
                else
                    txtSpectraPath.Text = dialog.SelectedPath;
            }
        }

        private void restoresCB_CheckedChanged(object sender, EventArgs e) {
            restoresNUD.Enabled = restoresCB.Checked;
        }

        private void updatesBtn_Click(object sender, EventArgs e) {
            if (Lt10Updater.DownloadedExecutablePath != string.Empty) {
                MessageBox.Show("Updates is already downloaded and will be installed after program exit.", Application.ProductName, MessageBoxButtons.OK, MessageBoxIcon.Information);
            } else {
                if (_updater == null) {
                    _updater = new Lt10Updater(false, new EventHandler(showUpdater), new EventHandler(noUpdatesFound));
                } else
                    _updater.LookForUpdates(false);
            }
        }

        private void noUpdatesFound(object sender, EventArgs e) {
            MessageBox.Show("No new updates found.", Application.ProductName, MessageBoxButtons.OK, MessageBoxIcon.Information); 
        }

        private void showUpdater(object sender, EventArgs e) {
            updatesBtn.Enabled = _updater.ShowDialog() == DialogResult.OK;
        }

    }
}
