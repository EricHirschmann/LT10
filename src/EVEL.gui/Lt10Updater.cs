using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Text;
using System.Windows.Forms;
using System.Net;
using System.IO;
using System.Reflection;
using System.Threading;

namespace Evel.gui {

    public partial class Lt10Updater : Form {

        public event EventHandler NewVersionAppeared;
        public event EventHandler NoUpdatesFound;

        private Version _siteVersion;
        //private Version _thisOrSkippedVersion = null;
        private static string _downloadedExecutablePath = string.Empty;
        private bool _autoUpdate = false;

        public Version SiteVersion {
            get { return this._siteVersion; }
        }

        public static string DownloadedExecutablePath {
            get { return _downloadedExecutablePath; }
        }

        internal Version ThisOrSkippedVersion {
            get {
                Version currVersion = Assembly.GetExecutingAssembly().GetName().Version;
                if (!"".Equals(MainForm.uversion)) {
                    Version regVersion = new Version(MainForm.uversion);

                    if (regVersion < currVersion) {
                        MainForm.uversion = currVersion.ToString();
                        MainForm.WriteRegistry();
                        return currVersion;
                    } else
                        return regVersion;
                } else if (!_autoUpdate)
                    return currVersion;
                //return this._thisOrSkippedVersion;
                else
                    return null;
            }
        }

        public Lt10Updater(bool programStart, EventHandler newVersionCallback, EventHandler noUpdatesCallback) : this(programStart, newVersionCallback) {
            this.NoUpdatesFound = noUpdatesCallback;
        }

        public Lt10Updater(bool programStart, EventHandler newVersionCallback) {
            InitializeComponent();
            this._autoUpdate = programStart;
            this.Text = Application.ProductName;
            this.NewVersionAppeared += newVersionCallback;
            LookForUpdates(programStart);
        }

        internal void LookForUpdates(bool auto) {
            if (!"".Equals(MainForm.uversion) || !auto) {
                //_thisOrSkippedVersion = new Version(MainForm.uversion);
                Thread updateThread = new Thread(LookForUpdatesThread);
                updateThread.Start(auto);
            }
        }

        private void LookForUpdatesThread(object auto) {
            if (ThisOrSkippedVersion != null || !(bool)auto) {
                try {
                    VersionRequest request = new VersionRequest((HttpWebRequest)WebRequest.Create("http://prac.us.edu.pl/~kansy/lt10v.htm"), (bool)auto);
                    request.AutoUpdate = (bool)auto;
                    request.request.BeginGetResponse(RespCallback, request);
                } catch (Exception) {
                    //MessageBox.Show(e.Message);
                }
            }
        }

        private void RespCallback(IAsyncResult asyncResult) {
            try {
                VersionRequest request = (VersionRequest)asyncResult.AsyncState;
                HttpWebResponse response = (HttpWebResponse)request.request.EndGetResponse(asyncResult);
                using (StreamReader reader = new StreamReader(response.GetResponseStream())) {
                    _siteVersion = new Version(reader.ReadToEnd());
                    if (_siteVersion > ThisOrSkippedVersion && this.NewVersionAppeared != null) {
                        //if (request.AutoUpdate)
                        if (NewVersionAppeared != null)
                            NewVersionAppeared(this, null);
                    } else {
                        if (NoUpdatesFound != null)
                            NoUpdatesFound(this, null);
                    }
                }
            } catch (Exception) {
                //MessageBox.Show(e.Message);
            }
        }

        private void Download() {
            try {
                WebClient client = new WebClient();
                client.DownloadProgressChanged += new DownloadProgressChangedEventHandler(client_DownloadProgressChanged);
                client.DownloadDataCompleted += new DownloadDataCompletedEventHandler(client_DownloadDataCompleted);
                //string localFile = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), ;
                Uri uri = new Uri("http://prac.us.edu.pl/~kansy/downloads/lt10.zip");
               
                client.DownloadDataAsync(uri);
            } catch (Exception ex) {
                MessageBox.Show(ex.Message);
            }
        }

        void client_DownloadDataCompleted(object sender, DownloadDataCompletedEventArgs e) {
            if (e.Cancelled)
                MessageBox.Show("Downloading has been corrupted", Application.ProductName, MessageBoxButtons.OK, MessageBoxIcon.Error);
            else {
                string updateDir = Path.Combine(Path.GetDirectoryName(Program.UpdatesFile), String.Format("lt10update_{0}", this.SiteVersion.ToString()));
                int i = 2;
                while (Directory.Exists(updateDir))
                    if (updateDir.IndexOf("(") == -1)
                        updateDir += "(2)";
                    else {
                        updateDir = updateDir.Replace(String.Format("({0})", i), String.Format("({0})", i + 1));
                        i++;
                    }
                try
                {
                    Directory.CreateDirectory(updateDir);
                    using (FileStream fstream = new FileStream(Path.Combine(updateDir, "lt10.zip"), FileMode.Create))
                    {
                        fstream.Write(e.Result, 0, e.Result.Length);
                    }
                    MessageBox.Show("Update has been successfuly downloaded and will be installed after program exit.");
                    using (var zipFile = Ionic.Zip.ZipFile.Read(Path.Combine(updateDir, "lt10.zip")))
                    {
                        zipFile.ExtractAll(updateDir);
                    }
                    _downloadedExecutablePath = Path.Combine(updateDir, "setup.exe");
                    this.DialogResult = DialogResult.OK;
                }
#if DEBUG
                catch (Exception ex)
                {
                    this.DialogResult = DialogResult.Cancel;
                    MessageBox.Show(String.Format("Error occured when unzipping downloaded files: {0}.", ex.Message), Application.ProductName, MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
#else
                catch (Exception)
                {
                    this.DialogResult = DialogResult.Cancel;
                    MessageBox.Show("Error occured when unzipping downloaded files.", Application.ProductName, MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
#endif 
                finally 
                {
                    Close();
                }
            }
        }

        void client_DownloadProgressChanged(object sender, DownloadProgressChangedEventArgs e) {
            downloadProgress.Value = e.ProgressPercentage;
        }

        private class VersionRequest {
            internal HttpWebRequest request;
            internal bool AutoUpdate = true;
            public VersionRequest(HttpWebRequest request, bool autoUpdate) {
                this.request = request;
                this.AutoUpdate = autoUpdate;
            }
        }

        private void Lt10Updater_Load(object sender, EventArgs e) {
            label1.Text = String.Format(label1.Text, SiteVersion.ToString());
        }

        private void button2_Click(object sender, EventArgs e) {
            Download();
        }

        private void button1_Click(object sender, EventArgs e) {
            if (this._autoUpdate && !"".Equals(MainForm.uversion)) {
                MainForm.uversion = this._siteVersion.ToString();
                MainForm.WriteRegistry();
            }
        }

    }
}
