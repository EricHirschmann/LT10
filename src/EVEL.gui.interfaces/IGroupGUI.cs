using System;
using System.Windows.Forms;
using Evel.interfaces;
using System.Collections.Generic;
using Evel.gui;

namespace Evel.gui.interfaces {
    public interface IGroupGUI {

        int FixedColCount { get; }
        DataGridView Grid { get; }
        Type ProjectType { get; }
        bool HasGraphicAdjustment { get; }
        GroupDefinition GroupDefinition { get; }
        
        int GetColumnCount();
        void SetHeaders();

        List<ToolBox> GetToolBoxes(ISpectrum spectrum, EventHandler changeHandler);
        void GridCellValueChange(object sender, DataGridViewCellEventArgs e);
        bool IsCellReadOnly(DataGridViewCell cell);
        //void Sort(TabPage tabPage, int columnId, SortOrder order);
        Form CreateValuesAdjuster(List<ISpectrum> spectra, ISpectrum adjustingSpectrum);
        
        double getDefaultParameterValue(ISpectrum spectrum, IParameter parameter);
        void CellFormatting(Object sender, DataGridViewCellFormattingEventArgs e);

        DataGridViewParameterCell CreateParameterCell(IParameter parameter);
        DataGridViewRow CreateControlersRow();
        DataGridViewSpectrumRow CreateSpectrumRow(ISpectrum spectrum);

        void ResetTools(Control.ControlCollection controls, EventHandler additionalChangeHandler);

    }
}
