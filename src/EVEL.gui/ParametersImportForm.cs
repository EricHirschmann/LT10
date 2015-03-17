using System;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Text;
using System.Windows.Forms;
using Evel.interfaces;
using System.IO;
using Evel.engine;
using System.Xml;
using Evel.engine.exceptions;
using System.Collections.Generic;

namespace Evel.gui {
    public partial class ParametersImportForm : Form {

        //private Hashtable _models;
        //private object[][] _models;
        private List<ImportedModel> _importedModels;
        private List<string> _modelNames;
        private IProject _project;
        private string _fileName;

        public ParametersImportForm(string filePath, IProject project) {
            InitializeComponent();
            this._project = project;
            //this._models = new object[15][];// new Hashtable();
            this._importedModels = new List<ImportedModel>();
            this._modelNames = new List<string>();
            this._fileName = System.IO.Path.GetFileName(filePath);
            getModels(filePath);
            setImportGrid();
        }

        private void getModels(string filePath) {
            Stream stream = null;
            ImportedModel currentModel = null;
            IParameterSet currentSet = null;
            IGroup group = null;
            bool parametersXmlArea = true;
            //int i = 0;
            try {
                using (XmlReader reader = ProjectBase.getXmlReader(filePath, out stream)) {
                    string modelClass;
                    string name;
                    //reader.ReadToFollowing("models");
                    while (reader.Read() && parametersXmlArea) {
                        switch (reader.Name) {
                            case "spectra":
                                if (reader.MoveToFirstAttribute()) {
                                    modelClass = String.Empty;
                                    name = String.Empty;
                                    do {
                                        switch (reader.Name) {
                                            case "name": name = reader.Value; break;
                                            case "class": modelClass = reader.Value; break;// AvailableAssemblies.getModel(reader.Value); break;
                                        }
                                    } while (reader.MoveToNextAttribute());
                                    if (name != String.Empty && modelClass != String.Empty) {
                                        _importedModels.Add(currentModel = new ImportedModel(modelClass, name));
                                        _modelNames.Add(name);
                                    }

                                }
                                reader.MoveToElement();
                                break;
                            case "spectrum":
                                //only start elemement has attributes. end elements are not analysed
                                if (reader.IsStartElement()) {
                                    if (currentModel == null) throw new Exception();
                                    //if (currentSet != null)
                                    //    currentModel.parameters.Add(currentSet);
                                    if (reader.ReadToFollowing("ps"))
                                        currentSet = new ParameterSet();
                                } else {
                                    currentModel.parameters.Add(currentSet);
                                    reader.MoveToElement();
                                    currentSet = null;
                                }
                                break;
                            case "group":
                                if (reader.HasAttributes) {
                                    if (currentSet == null) throw new Exception();
                                    //while (reader.Read()) { //groups
                                    if (reader.Name == "group") {
                                        XmlReader groupReader = reader.ReadSubtree();
                                        group = SpectrumBase.getGroup(groupReader, null, currentModel.model);
                                        currentSet.addGroup(group);
                                        groupReader.Close();
                                    }

                                }
                                break;
                            case "models": if (!reader.HasAttributes) parametersXmlArea = false; break;

                        }
                    }
                    if (_modelNames.Count > 0)
                        _modelNames.Insert(0, "(none)");
                }                    
            } catch (Exception) {
                throw new ImportException(String.Format("Failed to import parameters from {0}", filePath));
            } finally {
                if (stream != null)
                    stream.Close();
            }
            if (_modelNames.Count == 0)
                throw new ImportException(String.Format("{0}\ndoesn't contain any importable parameter values.", filePath));
        }

        public List<ImportedModel> ImportedModels {
            get { return this._importedModels; }
        }

        public List<string> ImportedModelNames {
            get { return this._modelNames; }
        }

        #region CellConstructors

        private DataGridViewCell GetComboBoxCell() {
            if (_modelNames.Count == 2 && _project.Containers.Count == 1)
                return new DataGridViewTextBoxCell();
            else {
            //if (_modelNames.Count > 1) {
                DataGridViewComboBoxCell cell = new DataGridViewComboBoxCell();
                cell.DataSource = this._modelNames;
                return cell;
            }
        }

        private DataGridViewTextBoxCell GetContainerCell(ISpectraContainer container) {
            DataGridViewTextBoxCell cell = new DataGridViewTextBoxCell();
            cell.Value = container;
            return cell;
        }

        private DataGridViewCheckBoxGroupCell GetGroupCell(GroupDefinition definition) { //, GroupDefinition importedDefinition) {
            DataGridViewCheckBoxGroupCell cell = new DataGridViewCheckBoxGroupCell(definition);
            return cell;
        }

        #endregion CellConstructors

        #region GridLookAndFeel

        private void setImportGrid() {
            DataGridViewRow row;
            int i, j;
            //columns
            importGridView.ColumnCount = 2;
            importGridView.Columns[0].HeaderText = this._fileName;
            //importGridView.Columns[0].Width = 100;
            importGridView.Columns[1].HeaderText = this._project.Caption;
            for (i = 0; i < _project.Containers[0].Model.GroupsDefinition.Length; i++)
                if ((_project.Containers[0].Model.GroupsDefinition[i].Type & GroupType.Hidden) == 0) {
                    importGridView.ColumnCount++;
                    importGridView.Columns[importGridView.ColumnCount - 1].HeaderText = _project.Containers[0].Model.GroupsDefinition[i].name;
                }
            for (i = 0; i < importGridView.ColumnCount; i++) {
                importGridView.Columns[i].SortMode = DataGridViewColumnSortMode.NotSortable;
                if (i<2)
                    importGridView.Columns[i].AutoSizeMode = DataGridViewAutoSizeColumnMode.DisplayedCells;
            }
            //rows
            for (i = 0; i < _project.Containers.Count; i++) {
                row = new DataGridViewRow();
                //import docs
                row.Cells.Add(GetComboBoxCell());
                //project docs
                row.Cells.Add(GetContainerCell(_project.Containers[i]));
                //group cells
                for (j = 0; j < _project.Containers[i].Model.GroupsDefinition.Length; j++) {
                    if ((_project.Containers[i].Model.GroupsDefinition[j].Type & GroupType.Hidden) == 0) {
                        row.Cells.Add(GetGroupCell(_project.Containers[i].Model.GroupsDefinition[j]));
                    }
                }
                importGridView.Rows.Add(row);
            }
            j=1;
            for (i = 0; i < importGridView.Rows.Count; i++) {
                if (j >= _modelNames.Count) j = 1;
                importGridView[0, i].Value = _modelNames[j++];
                //importGridView[0, i].ReadOnly = _modelNames.Count < 3;
            }
            importGridView.Columns[1].ReadOnly = true;
        }

        private void importGridView_CellValueChanged(object sender, DataGridViewCellEventArgs e) {
            //if import doc has been changed - modify group checkboxes 
            if (e.ColumnIndex == 0 && e.RowIndex >= 0) {
                int i, j;
                int id = _modelNames.IndexOf(importGridView[e.ColumnIndex, e.RowIndex].Value.ToString()) - 1;
                IModel model;
                for (i = 2; i < importGridView.Columns.Count; i++) {
                    importGridView[i, e.RowIndex].ReadOnly = true;
                    importGridView[i, e.RowIndex].Value = false;
                    if (id >= 0) {
                        model = _importedModels[id].model; // (IModel)_models[id][1];
                        for (j = 0; j < model.GroupsDefinition.Length; j++)
                            if (model.GroupsDefinition[j] == ((DataGridViewCheckBoxGroupCell)importGridView[i, e.RowIndex]).groupDefinition) {
                                importGridView[i, e.RowIndex].ReadOnly = false;
                                importGridView[i, e.RowIndex].Value = true;
                            }
                    }
                }
            }
        }        

        private void importGridView_CurrentCellDirtyStateChanged(object sender, EventArgs e) {
            if (importGridView.IsCurrentCellDirty)
                importGridView.CommitEdit(DataGridViewDataErrorContexts.Commit);
        }

        private void importGridView_CellFormatting(object sender, DataGridViewCellFormattingEventArgs e) {
            if (e.ColumnIndex == 1 && e.RowIndex > -1) {
                e.Value = ((ISpectraContainer)importGridView[e.ColumnIndex, e.RowIndex].Value).Name;
            }
        }

        #endregion GridLookAndFeel

        private void panel3_Paint(object sender, PaintEventArgs e) {
            MainForm.DialogPanel_Paint(sender, e);
        }

        #region HelperClasses

        public class DataGridViewCheckBoxGroupCell : DataGridViewCheckBoxCell {

            public GroupDefinition groupDefinition;

            public DataGridViewCheckBoxGroupCell(GroupDefinition definition)
                : base(false) {
                this.groupDefinition = definition;
            }

        }

        public class ImportedModel {
            public IModel model;
            public string name;
            public List<IParameterSet> parameters; //single parameterSet holds parameters for all interesting groups
            public ImportedModel(string modelClass, string name) {
                this.model = AvailableAssemblies.getModel(modelClass);
                this.name = name;
                this.parameters = new List<IParameterSet>();
            }
        }

        #endregion HelperClasses

    }
}
