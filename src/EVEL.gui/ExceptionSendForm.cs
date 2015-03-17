using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.Net;
using System.Security.Permissions;
using System.Collections.Specialized;
using System.IO;
using System.IO.Compression;
using System.Text.RegularExpressions;
using System.Security;
using System.Reflection;

namespace Evel.gui {
    public partial class ExceptionSendForm : Form {

        private Exception _exception;
        private MemoryStream _reportStream;

        public ExceptionSendForm(Exception exception) {
            InitializeComponent();
            this.Height = 249;
            this._exception = exception;
            prepareReport(String.Empty);
            this.pictureBox1.Image = SystemIcons.Error.ToBitmap();
        }

        private void appendReportLine(string line, TextWriter writer) {
            writer.WriteLine(line);
            textBox1.Text += String.Format("{0}\r\n", line);
        }

        private void prepareReport(string userComments) {
            _reportStream = new MemoryStream();
            textBox1.Clear();
            GZipStream zipStream = new GZipStream(_reportStream, CompressionMode.Compress);
            TextWriter writer = new StreamWriter(zipStream);
            appendReportLine("----------OS-----------", writer);
            appendReportLine(Environment.OSVersion.ToString(), writer);
            appendReportLine("--------LT Version--------", writer);
            appendReportLine(Assembly.GetExecutingAssembly().GetName().Version.ToString(), writer);
            appendReportLine("------Environment------", writer);
            appendReportLine(String.Format("decimal separator = '{0}'", Application.CurrentCulture.NumberFormat.NumberDecimalSeparator), writer);
            appendReportLine(String.Format("current culture = '{0}'", Application.CurrentCulture.ToString()), writer);
            appendReportLine("-------Exception-------", writer);
            appendReportLine(_exception.Message, writer);
            appendReportLine(_exception.StackTrace, writer);
            appendReportLine("-------Loaded assemblies----------", writer);
            foreach (Assembly ass in AppDomain.CurrentDomain.GetAssemblies()) {
                appendReportLine(String.Format("{0}\r\n", ass.FullName), writer);
            }
            if (userComments != String.Empty && userComments != "") {
                appendReportLine("-----------user comments-----------", writer);
                appendReportLine(userComments, writer);
            }
            writer.Close();
            zipStream.Close();
            _reportStream.Close();
        }

        private void pictureBox1_Paint(object sender, PaintEventArgs e) {
            //e.Graphics.DrawIcon(SystemIcons.Error, e.ClipRectangle);
        }

        private void linkLabel1_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e) {
            splitContainer1.Panel2Collapsed = !splitContainer1.Panel2Collapsed;
            this.Height = (splitContainer1.Panel2Collapsed) ? 249 : 438;
        }

        private void sendReport() {
            try {
                string url = "http://prac.us.edu.pl/~kansy/feedback.php";
                WebClient client = new WebClient();
                byte[] response;

                NameValueCollection queryCollection = new NameValueCollection();
                queryCollection.Add("version", Assembly.GetExecutingAssembly().GetName().Version.ToString());
                queryCollection.Add("pass", "qikol95");
                queryCollection.Add("program", "need");
                client.QueryString = queryCollection;
                prepareReport(txtComments.Text);
                response = client.UploadData(url, _reportStream.ToArray());
                string responseStr = new string(Encoding.ASCII.GetChars(response));
                Regex regex = new Regex(@"\[returncode=(?<code>\d+)\]", RegexOptions.Compiled);
                if (regex.IsMatch(responseStr)) {
                    Match match = regex.Match(responseStr);
                    int code = Int32.Parse(match.Groups["code"].Value);
                    switch (code) {
                        case 200: MessageBox.Show("Report sucessfuly sent", Application.ProductName, MessageBoxButtons.OK, MessageBoxIcon.Information); break;
                        default: MessageBox.Show("Couldn't send raport! There have been errors while sending report.", Application.ProductName, MessageBoxButtons.OK, MessageBoxIcon.Warning); break;
                    }
                }
            } catch (Exception) {
                MessageBox.Show("Couldn't send report! Either there is no internet connection available or feedback site is not responding.", Application.ProductName, MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }

        private void button1_Click(object sender, EventArgs e) {
            sendReport();
        }
    }
}
