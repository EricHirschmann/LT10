using System.Windows.Forms;
using Evel.interfaces;
using System;

namespace Evel.gui.dialogs {
    public partial class GroupsDialog : Form {

        private GroupDefinition[] _patternGroupsDefinition;

        private int _groupCounter = 1;

        public GroupDefinition[] GroupsDefinition {
            get {
                int resultSize = groupsGrid.RowCount;
                foreach (GroupDefinition gd in _patternGroupsDefinition) {
                    if (gd.kind != 1) resultSize++;  //sample
                }
                GroupDefinition[] result = new GroupDefinition[resultSize];
                int position = 0;
                for (int r = 0; r < groupsGrid.RowCount; r++) {
                    result[r] = (GroupDefinition)groupsGrid[0, r].Value;
                    position++;
                }
                foreach (GroupDefinition gd in _patternGroupsDefinition) {
                    if (gd.kind != 1) { //sample
                        result[position++] = gd;
                    }
                }
                return result;
            }
        }

        public GroupsDialog(GroupDefinition[] groupsDefinition, GroupDefinition[] patternGroupsDefinition) {
            InitializeComponent();
            this._patternGroupsDefinition = patternGroupsDefinition;
            SetGrid(groupsDefinition);
        }

        private void SetGrid(GroupDefinition[] groupsDefinition) {
            foreach (GroupDefinition gd in groupsDefinition) {
                if (gd.kind == 1) { //sample
                    groupsGrid.RowCount++;
                    groupsGrid.Rows[groupsGrid.RowCount - 1].Cells[0].Value = gd;
                    groupsGrid.Rows[groupsGrid.RowCount - 1].Cells[1].Value = gd.name;
                    groupsGrid.Rows[groupsGrid.RowCount - 1].Cells[2].Value = (gd.Type & GroupType.CalcContribution) == GroupType.CalcContribution;
                }
            }
        }

        private void toolStripButton1_Click(object sender, System.EventArgs e) {
            GroupDefinition gd = new GroupDefinition();
            gd = this._patternGroupsDefinition[1];
            gd.Type &= ~GroupType.CalcContribution;
            gd.name = String.Format("Package {0}", this._groupCounter++);
            groupsGrid.RowCount++;
            groupsGrid.Rows[groupsGrid.RowCount - 1].Cells[0].Value = gd;
            groupsGrid.Rows[groupsGrid.RowCount - 1].Cells[1].Value = gd.name;
            groupsGrid.Rows[groupsGrid.RowCount - 1].Cells[2].Value = (gd.Type & GroupType.CalcContribution) == GroupType.CalcContribution;
        }

        private void groupsGrid_CellContentClick(object sender, DataGridViewCellEventArgs e) {
            if (groupsGrid[e.ColumnIndex, e.RowIndex] is DataGridViewButtonCell) {
                if (groupsGrid.RowCount > 1)
                    groupsGrid.Rows.Remove(groupsGrid.Rows[e.RowIndex]);
                else
                    MessageBox.Show("At least one group must be defined.", "LT10", MessageBoxButtons.OK, MessageBoxIcon.Information);
            } else {
                GroupDefinition gd;
                if (groupsGrid[e.ColumnIndex, e.RowIndex] is DataGridViewCheckBoxCell) {
                    DataGridViewCheckBoxCell cell = (DataGridViewCheckBoxCell)groupsGrid[e.ColumnIndex, e.RowIndex];
                    if ((!(bool)cell.Value)) {
                        foreach (DataGridViewRow row in groupsGrid.Rows) {
                            if (row.Cells[2] != cell) {
                                row.Cells[2].Value = false;
                                gd = (GroupDefinition)row.Cells[0].Value;
                                gd.Type &= ~GroupType.CalcContribution;
                                row.Cells[0].Value = gd;
                            }
                        }
                        cell.Value = true;
                        gd = (GroupDefinition)groupsGrid.Rows[e.RowIndex].Cells[0].Value;
                        gd.Type |= GroupType.CalcContribution;
                        groupsGrid.Rows[e.RowIndex].Cells[0].Value = gd;
                    }
                }
            }
        }

        private void panel1_Paint(object sender, PaintEventArgs e) {
            MainForm.DialogPanel_Paint(sender, e);
        }


    }
}
