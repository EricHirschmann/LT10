using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Evel.engine;
using Evel.interfaces;
using System.Windows.Forms;
using System.ComponentModel;

namespace Evel.gui {
    class SharedGroupTabPage : GroupTabPage {

        private GroupBinding _binding;

        public GroupBinding Binding {
            get { return this._binding; }
        }

        public SharedGroupTabPage(List<ISpectrum> spectra,
            GroupDefinition definition,
            StatusStrip statusStrip,
            TabControl parentTabControl,
            ProjectForm parentProjectForm,
            GroupBinding binding)
            : base(spectra, definition, statusStrip, parentTabControl, parentProjectForm) {
            this._binding = binding;
            grid.FixedCols = 3;
            UpdateName();
            refreshReferences();
        }

        public void UpdateName() {
            if (_binding.HasName)
                this.Text = String.Format("{0} [{1}]", groupDefinition.name, _binding.Name);
        }

        //protected override void grid_ColumnHeaderMouseClick(object sender, DataGridViewCellMouseEventArgs e) {
        //    DataGridViewColumnHeaderCell clickedHeaderCell = grid.Columns[e.ColumnIndex].HeaderCell;
        //    base.grid_ColumnHeaderMouseClick(sender, e);
        //    if (grid.Columns[e.ColumnIndex].SortMode != DataGridViewColumnSortMode.Programmatic) return;
        //    //SortOrder order;// = SortOrder.None;
        //    //switch (clickedHeaderCell.SortGlyphDirection) {
        //    //    case SortOrder.None:
        //    //    case SortOrder.Ascending:
        //    //        //sort descending
        //    //        order = SortOrder.Descending;
        //    //        break;
        //    //    case SortOrder.Descending:
        //    //        //sort ascending
        //    //        order = SortOrder.Ascending;
        //    //        break;
        //    //}
        //    foreach (SpectraContainerTabPage page in _parentProjectForm.documentTabs) {
        //        if (this._binding.ContainsContainer(page.SpectraContainer)) {
        //            foreach (TabPage grPage in page._groupsControl.TabPages) {
        //                if (grPage is GroupTabPage) {
        //                    DataGridView ggrid = ((GroupTabPage)grPage).grid;
        //                    ggrid.Sort(ggrid.Columns[clickedHeaderCell.ColumnIndex], (clickedHeaderCell.SortGlyphDirection == SortOrder.Ascending) ? ListSortDirection.Ascending : ListSortDirection.Descending);
        //                    groupGUI.Sort(grPage, clickedHeaderCell.ColumnIndex, clickedHeaderCell.SortGlyphDirection);
        //                    ((GroupTabPage)grPage).refreshReferences();
        //                }
        //            }
        //        }
        //    }
        //}

        protected override string GetExcelParametersGroupName() {
            StringBuilder builder = new StringBuilder();
            if (this._binding.HasName)
                builder.Append(this._binding.Name);
            builder.AppendFormat("({0}", this._binding.Containers[0].Name);
            for (int i = 1; i < this._binding.Containers.Length; i++)
                builder.AppendFormat(", {0}", this._binding.Containers[i].Name);
            builder.Append(")");
            return builder.ToString();
            //return Binding.;
        }

        protected override bool IncludeRule(object sender, ISpectrum spectrum) {
            return true;
        }

        protected override Comparison<ISpectrum> GetColumnComparison(int columnId) {
            if (columnId == 2) {
                return delegate(ISpectrum s1, ISpectrum s2) {
                    return s1.Container.Name.CompareTo(s2.Container.Name);
                };
            } else
               return base.GetColumnComparison(columnId);
        }

    }
}
