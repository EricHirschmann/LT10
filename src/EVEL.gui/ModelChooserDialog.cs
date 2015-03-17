using System;
using System.Drawing;
using System.Windows.Forms;
using Evel.engine;
using Evel.interfaces;
using System.Collections;
using System.Text;

namespace Evel.gui.dialogs {
    public partial class ModelChooserDialog : Form {

        private Hashtable _models;

        public ModelChooserDialog(Type ProjectType) {
            InitializeComponent();

            _models = new Hashtable();

            //create groups

            Hashtable groups = new Hashtable();
            for (int i = 0; i < AvailableAssemblies.AvailableModels.Count; i++) {
                if (ProjectType == null || ProjectType == AvailableAssemblies.AvailableModels[i].projectType)
                    if (!groups.Contains(AvailableAssemblies.AvailableModels[i].projectType)) {
                        ListViewGroup group = new ListViewGroup(
                            String.Format("{0} models", 
                            AvailableAssemblies.GetProjectDesription(AvailableAssemblies.AvailableModels[i].projectType).experimentalMethodName));
                        groups.Add(AvailableAssemblies.AvailableModels[i].projectType, group);
                        listView1.Groups.Add(group);
                    }
            }

            for (int i = 0; i < AvailableAssemblies.AvailableModels.Count; i++) {
                if (groups.ContainsKey(AvailableAssemblies.AvailableModels[i].projectType)) {
                    ListViewItem item = new ListViewItem(
                        AvailableAssemblies.AvailableModels[i].name,
                        (ListViewGroup)groups[AvailableAssemblies.AvailableModels[i].projectType]);
                    _models.Add(item, AvailableAssemblies.AvailableModels[i]);
                    listView1.Items.Add(item);
                }
            }

        }

        public ModelChooserDialog(Type ProjectType, string modelName)
            : this(ProjectType) {
            for (int i = 0; i < listView1.Items.Count; i++)
                listView1.Items[i].Selected = listView1.Items[i].Focused = (((ModelDescription)_models[listView1.Items[i]]).name == modelName);
            listView1.Select();
        }

        private void listView1_SelectedIndexChanged(object sender, EventArgs e) {
            if (listView1.SelectedItems.Count == 1)
                SetDescrption((ModelDescription)_models[listView1.SelectedItems[0]], info);
            else
                info.Text = "";
            OkButton.Enabled = listView1.SelectedItems.Count == 1;
        }

        public static void AddHeader(string header, RichTextBox textBox) {
            textBox.Select(textBox.Text.Length, 1);
            textBox.SelectionFont = textBox.Font;
            textBox.SelectionIndent = 0;
            textBox.AppendText(String.Format("{0}\n", header));
            textBox.Select(textBox.Text.Length - header.Length-1, header.Length);
            textBox.SelectionFont = new Font(textBox.Font, FontStyle.Bold);
        }

        public static void AddParagraph(string text, bool format, RichTextBox textBox) {
            textBox.Select(textBox.Text.Length, 1);
            textBox.SelectionFont = textBox.Font;
            textBox.SelectionIndent = 0;
            int start = textBox.Text.Length;
            if (format) {
                //string formattedText = DefaultGroupGUI.BuildFormatedString(text, -1, DefaultGroupGUI.StringFormatTarget.ParameterDataGrid);
                //MainForm.WriteFormatedText(formattedText, textBox);
                DefaultGroupGUI.writeFormattedText(
                    DefaultGroupGUI.BuildFormatedString(text, DefaultGroupGUI.StringFormatTarget.ParameterDataGrid), 
                    textBox);
            } else {
                textBox.AppendText(String.Format("{0}\n", text));
            }
            textBox.Select(start, textBox.Text.Length-start);
            textBox.SelectionIndent = 20;
        }

        public static void SetDescrption(ModelDescription md, RichTextBox textBox) {
            textBox.Clear();
            AddHeader(md.name, textBox);
            AddParagraph(md.description, true, textBox);
            AddHeader("Prameters in groups", textBox);
            StringBuilder p = new StringBuilder();
            foreach (GroupDefinition gd in md.groupDefinitions) {
                if ((gd.Type & GroupType.Hidden) == GroupType.Hidden) continue;
                p.AppendFormat("[p text='{0}:\t']", gd.name);
                for (int i = 0; i < gd.parameters.Length; i++) {
                    p.AppendFormat("{0}[p text='{1}']", gd.parameters[i].Header, (i == gd.parameters.Length - 1) ? ".\n" : ", ");
                }
            }
            p.Append("[p text='\n']");
            AddParagraph(p.ToString(), true, textBox);
            AddHeader("Assembly", textBox);
            AddParagraph(md.plugin.assemblyPath, false, textBox);
        }

        private void panel1_Paint(object sender, PaintEventArgs e) {
            MainForm.DialogPanel_Paint(sender, e);
        }

        public ModelDescription Selection {
            get { return (ModelDescription)_models[listView1.SelectedItems[0]]; }
        }

        private void listView1_MouseDoubleClick(object sender, MouseEventArgs e) {
            if (listView1.SelectedItems.Count == 1)
                OkButton.PerformClick();
        }

        private void ModelChooserDialog_HelpRequested(object sender, HelpEventArgs hlpevent) {
            Help.ShowHelp(this.Parent, MainForm.helpfile, HelpNavigator.KeywordIndex, "Model");
        }

    }
}
