using System.Windows.Forms;

namespace Evel.gui.dialogs {
    public partial class DocumentNameDialog : Form {
        public DocumentNameDialog() {
            InitializeComponent();
        }

        private void panel1_Paint(object sender, PaintEventArgs e) {
            MainForm.DialogPanel_Paint(sender, e);
        }

    }
}
