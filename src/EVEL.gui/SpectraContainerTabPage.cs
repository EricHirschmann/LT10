using System;
using System.Collections.Generic;
using System.Text;
using Evel.interfaces;
using GemBox.Spreadsheet;
using System.Windows.Forms;


namespace Evel.gui {
    public class SpectraContainerTabPage : System.Windows.Forms.TabPage {

        private ISpectraContainer _container;
        internal TabControl _groupsControl;
        private ProjectForm _projectForm;

        public SpectraContainerTabPage(ISpectraContainer container, ProjectForm projectForm)
            : base(container.Name) {
            this._container = container;
            this.Name = container.Name;
            this._projectForm = projectForm;
            _groupsControl = new TabControl();
            _groupsControl.Name = "groupsControl";
            _groupsControl.Parent = this;
            _groupsControl.Dock = DockStyle.Fill;
            int id = 0;
            foreach (IGroup group in container.Spectra[0].Parameters) {
                if (!projectForm.project.BindingsManager.Contains(container, group.Definition.name)) {
                    GroupDefinition gd = group.Definition;
                    if ((gd.Type & GroupType.Hidden) != GroupType.Hidden)
                        //_groupsControl.TabPages.Add(new GroupTabPage(container.Spectra, gd, projectForm.statusStrip1, _groupsControl));
                        InsertGroupTabPage(gd.name, id++);
                }
            }
            //if (projectForm.project.BindingsManager.Contains(container))
            //    SortableGroupGrids = false;
        }

        public GroupTabPage InsertGroupTabPage(string groupName) {
            int id = 0;
            foreach (IGroup group in _container.Spectra[0].Parameters) {
                if (!_projectForm.project.BindingsManager.Contains(_container, group.Definition.name)) {
                    GroupDefinition gd = group.Definition;
                    if ((gd.Type & GroupType.Hidden) != GroupType.Hidden) {
                        if (gd.name != groupName) id++;
                        else
                            return InsertGroupTabPage(gd.name, id);
                    }
                }
            }
            return null;
        }

        protected GroupTabPage InsertGroupTabPage(string groupName, int id) {
            GroupTabPage page = new GroupTabPage(
                this._container.Spectra,
                this._container.Spectra[0].Parameters[groupName].Definition,
                this._projectForm.statusStrip1,
                _groupsControl, _projectForm);
            if (id >= _groupsControl.TabPages.Count)
                _groupsControl.TabPages.Add(page);
            else
                _groupsControl.TabPages.Insert(id, page);
            return page;
        }

        public void RemoveGroupTabPage(string groupName) {
            foreach (TabPage page in _groupsControl.TabPages)
                if (page is GroupTabPage) {
                    if (((GroupTabPage)page).groupGUI.GroupDefinition.name == groupName) {
                        _groupsControl.TabPages.Remove(page);
                        break;
                    }
                }
        }

        //public bool SortableGroupGrids {
        //    set {
        //        int i, j;
        //        DataGridViewColumnSortMode sortMode = (value) ? DataGridViewColumnSortMode.Programmatic : DataGridViewColumnSortMode.NotSortable;
        //        for (i = 0; i < this._groupsControl.TabCount; i++) {
        //            for (j=0; j<2; j++)
        //                ((GroupTabPage)this._groupsControl.TabPages[i]).grid.Columns[j].SortMode = sortMode;
        //        }                    
        //    }
        //    get {
        //        return ((GroupTabPage)this._groupsControl.TabPages[0]).grid.Columns[0].SortMode == DataGridViewColumnSortMode.Programmatic;
        //    }
        //}

        public ProjectForm ProjectForm {
            get { return this._projectForm; }
        }

        public TabControl GroupsControl {
            get { return this._groupsControl; }
        }

        public ISpectraContainer SpectraContainer {
            get { return this._container; }
        }

        public void fillExcelWorksheets(ExcelWorksheetCollection worksheets, int[] startColumns) {
            int tabId = 0;
            foreach (GroupTabPage tab in _groupsControl.TabPages) {
                while (tabId < worksheets.Count && worksheets[tabId].Name != tab.Text) tabId++;
                //worksheets[tabId].Cells[0, startColumns[tabId]].Value = this._container.Name;
                tab.fillExcelWorksheet(worksheets[tabId], ref startColumns[tabId]);
                
                tabId++;
            }
        }

    }
}
