using System.Windows.Forms;
using System.Threading;
using System.Reflection;

namespace Evel.gui {
    public partial class Splashscreen : Form {
        public Splashscreen() {
            InitializeComponent();
            versionLabel.Text = Assembly.GetExecutingAssembly().GetName().Version.ToString();
        }

        private void Splashscreen_Load(object sender, System.EventArgs e) {
            new Thread(delegate() {
                Thread.Sleep(1000);
                BeginInvoke(new ThreadStart(Close));
            }).Start();
        }
    }
}
