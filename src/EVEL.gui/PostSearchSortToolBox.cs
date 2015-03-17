using System;
using System.Collections.Generic;
using System.Text;
using System.Windows.Forms;
using System.ComponentModel;
using Evel.interfaces;
using System.Drawing;
using System.Threading;

namespace Evel.gui {
    public class PostSearchSortToolBox : PostSearchToolBox {

        private int parameterId = 0;
        private ComboBox parameterSelector;

        public PostSearchSortToolBox(TabPage groupTabPage) : base(groupTabPage) {
            parameterSelector = new ComboBox();
            GroupTabPage gtb = (GroupTabPage)groupTabPage;
            parameterSelector.Items.Add("[p text='(none)']");
            for (int i=0; i<gtb.GroupDefinition.parameters.Length; i++)
                if ((gtb.GroupDefinition.parameters[i].Properties & (ParameterProperties.GroupUnique | ParameterProperties.Hidden | ParameterProperties.KeyValue | ParameterProperties.Readonly | ParameterProperties.Unsearchable)) == 0)
                    parameterSelector.Items.Add(gtb.GroupDefinition.parameters[i]);
            //parameterSelector.Items.Add
            parameterSelector.DrawMode = DrawMode.OwnerDrawFixed;
            parameterSelector.DropDownStyle = ComboBoxStyle.DropDownList;
            parameterSelector.DrawItem += new DrawItemEventHandler(parameterSelector_DrawItem);
            parameterSelector.Dock = DockStyle.Fill;
            parameterSelector.ItemHeight = 20;
            parameterSelector.SelectedIndexChanged += new EventHandler(parameterSelector_SelectedIndexChanged);
            parameterSelector.SelectedIndex = gtb.GroupDefinition.defaultSortedParameter + 1;
            this.Controls.Add(parameterSelector);
        }

        void parameterSelector_SelectedIndexChanged(object sender, EventArgs e) {
            parameterId = parameterSelector.SelectedIndex - 1;
            //if (parameterId >= 0)
            //    MessageBox.Show(String.Format("sorting by {0}", ((GroupTabPage)groupTabPage).GroupDefinition.parameters[parameterId].Name));
            //else
            //    MessageBox.Show("Sorting disabled");
        }

        void parameterSelector_DrawItem(object sender, DrawItemEventArgs e) {
            if (e.Index == -1) return;
            e.DrawBackground();
            ComboBox comboBox = (ComboBox)sender;
            e.Graphics.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.HighQuality;
            string item;
            if (comboBox.Items[e.Index] is ParameterDefinition)
                item = ((ParameterDefinition)comboBox.Items[e.Index]).Header;
            else
                item = comboBox.Items[e.Index].ToString();
            if ((e.State & DrawItemState.Selected) == DrawItemState.Selected)
                DefaultGroupGUI.DrawHeaderContent(

                    item,
                    //comboBox.GetItemText(comboBox.Items[e.Index]),
                    e.Graphics,
                    e.Font,
                    e.Bounds,
                    false, SystemBrushes.HighlightText);
            else
                DefaultGroupGUI.DrawHeaderContent(
                    item,
                    //comboBox.GetItemText(comboBox.Items[e.Index]),
                    e.Graphics,
                    e.Font,
                    e.Bounds,
                    false, SystemBrushes.WindowText);
        }

        public override void RunPostSearchEvent(object sender, AsyncCompletedEventArgs args) {
            if (parameterId != -1) {
                int i;
                GroupTabPage gtb = (GroupTabPage)this.groupTabPage;
                bool sort = true;
                for (i = 2; i < gtb.grid.ColumnCount && sort; i++)
                    if (gtb.grid[i, 0] is DataGridViewComboBoxCell)
                        sort = !"mixed".Equals(gtb.grid[i, 0].FormattedValue.ToString());
                if (sort && parameterId >= 0) {
                    //MessageBox.Show(String.Format("Sorting {0} group", groupTabPage.Text));
                    for (i = 0; i < gtb.Spectra.Count; i++)
                        gtb.Spectra[i].Parameters[gtb.GroupDefinition.name].Components.Sort(parameterId);
                }
                gtb.Invoke(new ThreadStart(delegate() {
                    gtb.SetDataGrid(true);
                }));
            }
        }

    }
}
