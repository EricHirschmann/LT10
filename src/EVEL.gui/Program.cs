using System;
using System.Collections.Generic;
using System.Windows.Forms;
using System.Globalization;
using System.Threading;
using Evel.engine;
using Microsoft.Win32;
using System.IO;
using System.Diagnostics;

namespace Evel.gui {
    public static class Program {

        private static MainForm _mainWindow;
        private static string[] _args;

        public static MainForm MainWindow {
            get {
                if (_mainWindow == null)
                    _mainWindow = new MainForm(_args);
                return _mainWindow;
            }
        }

        public static string RegistryRootKeyName {
            get { return "HKEY_CURRENT_USER\\Software\\lt10"; }
        }

        internal static string UpdatesFile {
            get {
                //return Path.Combine(Application.StartupPath, "updates\\updates.pth");
                return Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), @"LT10\updates\updates.pth");
            }
        }

        public static void saveException(Exception e) {
            TextWriter writer = File.CreateText(Path.Combine(Application.StartupPath, "exception.log"));
            writer.Write(e.StackTrace);
            writer.Close();
        }

        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        [STAThread]
        static void Main(string[] args) {
            //clean updates folder if any (proper entry in registry
            
            try {
                if (File.Exists(UpdatesFile)) {
                    using (StreamReader reader = new StreamReader(UpdatesFile)) {
                        string updatesSetup = reader.ReadToEnd().Trim();
                        string updatesPath = Path.GetDirectoryName(updatesSetup);
                        Directory.Delete(updatesPath, true);

                    }
                    File.Delete(UpdatesFile);
                }
            } catch (Exception) {
                MessageBox.Show("Couldn't perform a clean up after updating LT10.", Application.ProductName, MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }

            bool restart = false;
            System.Diagnostics.Debug.Listeners.Clear();
            System.Diagnostics.Debug.Listeners.Add(
                new System.Diagnostics.TextWriterTraceListener(@"d:\devel\ltvsneed\lt10search.log"));
            do {
                try {
                    if (restart) {
                        _mainWindow.CloseForm();
                        _mainWindow = null;
                        //Application.Exit();
                    }
                    //if (!restart) {
                    CultureInfo culture = new CultureInfo(CultureInfo.CurrentCulture.LCID);
                    culture.NumberFormat.NumberDecimalSeparator = ".";
                    AvailableAssemblies.LibraryDir = Application.StartupPath;
                    Application.CurrentCulture = culture;
                    Application.EnableVisualStyles();
                    _args = args;
                    Application.Run(MainWindow);
                    //} else {
                    //    _mainWindow = null;
                    //    Application.Exit();
                    //    Application.Run(MainWindow);

                    //}
                    restart = false;
                } catch (Exception e) {
                    Evel.gui.ExceptionSendForm form = new ExceptionSendForm(Evel.share.Utilities.findException(e));
                    form.ShowDialog();
                    restart = true;
                }
            } while (restart);
            if (Lt10Updater.DownloadedExecutablePath != string.Empty) {
                using (StreamWriter writer = new StreamWriter(UpdatesFile, false)) {
                    writer.WriteLine(Lt10Updater.DownloadedExecutablePath);
                }
                try {
                    System.Diagnostics.Process.Start(Lt10Updater.DownloadedExecutablePath, null);
                } catch (Exception e) {
                    MessageBox.Show(String.Format("Installation aborted. {0}", e.Message), Application.ProductName, MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
        }
    }
}
