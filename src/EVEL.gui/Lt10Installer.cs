using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Configuration.Install;
using Microsoft.Win32;
using System.Drawing;
using Evel.interfaces;
using System.ComponentModel;
using System.IO;

namespace Evel.gui {
    [RunInstaller(true)]
    public class Lt10Installer : Installer {
        public Lt10Installer()
            : base() {
        }
        public override void Install(System.Collections.IDictionary savedState) {
            base.Install(savedState);
            Registry.CurrentUser.CreateSubKey(@"Software\LT10");
            RegistryKey rk = Registry.CurrentUser.CreateSubKey(@"Software\LT10\gui");
            //saving before calculations
            if (rk.GetValue(@"SaveBeforeFitting") == null)
                rk.SetValue(@"SaveBeforeFitting", 1, RegistryValueKind.DWord);
            //switching to search after calculations run
            if (rk.GetValue(@"SwitchToSearchTab") == null)
                rk.SetValue(@"SwitchToSearchTab", "True", RegistryValueKind.String);
            //paths
            if (rk.GetValue(@"projectsPath") == null)
                rk.SetValue(@"projectsPath", Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "My LT10 Projects"), RegistryValueKind.String);
            if (rk.GetValue(@"commonSpectraPath") == null)
                rk.SetValue(@"commonSpectraPath", "", RegistryValueKind.String);
            //recent projects
            if (rk.GetValue(@"RecentProjects") == null)
                rk.SetValue(@"RecentProjects", new string[] { }, RegistryValueKind.MultiString);
            if (rk.GetValue(@"MaxRecentProjects") == null)
                rk.SetValue(@"MaxRecentProjects", 5, RegistryValueKind.DWord);
            //status colors

            if (rk.GetValue("LocalFreeColor") == null)
                rk.SetValue(@"LocalFreeColor", Color.FromArgb(0xe1, 0xf5, 0xde).ToArgb(), RegistryValueKind.DWord);
            if (rk.GetValue("LocalFixedColor") == null)
                rk.SetValue(@"LocalFixedColor", Color.FromArgb(0xef, 0x0e, 0x13).ToArgb(), RegistryValueKind.DWord);
            if (rk.GetValue("CommonFreeColor") == null)
                rk.SetValue(@"CommonFreeColor", Color.FromArgb(0x6d, 0xe8, 0x60).ToArgb(), RegistryValueKind.DWord);
            if (rk.GetValue("CommonFixedColor") == null)
                rk.SetValue(@"CommonFixedColor", Color.FromArgb(0x82, 0x13, 0x13).ToArgb(), RegistryValueKind.DWord);
            if (rk.GetValue("BindedFixedColor") == null)
                rk.SetValue(@"BindedFixedColor", Color.FromArgb(0xfd, 0xc8, 0x4a).ToArgb(), RegistryValueKind.DWord);
            if (rk.GetValue("BindedFreeColor") == null)
                rk.SetValue(@"BindedFreeColor", Color.FromArgb(0x4a, 0xd0, 0xfd).ToArgb(), RegistryValueKind.DWord);
            if (rk.GetValue("RestoreCount") == null)
                rk.SetValue(@"RestoreCount", 23, RegistryValueKind.DWord);

            Registry.ClassesRoot.CreateSubKey("LT10").SetValue("", "LT10 Project File");
            Registry.ClassesRoot.CreateSubKey(@"LT10\shell\Open\command").SetValue("", String.Format("\"{0}\" -p \"%1\"", this.Context.Parameters["assemblypath"]));
            Registry.ClassesRoot.CreateSubKey(@"LT10\DefaultIcon").SetValue("", String.Format("\"{0}\",1", this.Context.Parameters["assemblypath"]));

            Registry.ClassesRoot.CreateSubKey(".ltp").SetValue("", "LT10");
            Registry.ClassesRoot.CreateSubKey(".ltpi").SetValue("", "LT10");
            Registry.ClassesRoot.CreateSubKey(".ltpe").SetValue("", "LT10");
            Registry.ClassesRoot.CreateSubKey(".ltpp").SetValue("", "LT10");

        }

        public override void Uninstall(System.Collections.IDictionary savedState) {
            base.Uninstall(savedState);
            try {
                try {
                    Registry.CurrentUser.DeleteSubKeyTree(@"Software\LT10");
                } catch { }
                try {
                    Registry.ClassesRoot.DeleteSubKeyTree(@"LT10");
                } catch { }
                try {
                    Registry.ClassesRoot.DeleteSubKey(".ltp");
                } catch { }
                try {
                    Registry.ClassesRoot.DeleteSubKey(".ltpi");
                } catch { }
                try {
                    Registry.ClassesRoot.DeleteSubKey(".ltpe");
                } catch { }
                try {
                    Registry.ClassesRoot.DeleteSubKey(".ltpp");
                } catch { }
                //delete any of files left after using LT10 (pr.bak)
                //try {
                //    string prbak = System.IO.Path.Combine(this.Context.Parameters["InstDir"], "pr.bak");
                //    if (System.IO.File.Exists(prbak))
                //        System.IO.File.Delete(prbak);
                //} catch (Exception ei) {
                //    System.Windows.Forms.MessageBox.Show(ei.Message + ei.StackTrace);
                //}
            } catch (Exception ei) {
                System.Windows.Forms.MessageBox.Show(ei.Message + ei.StackTrace);
            }
        }
        public override void Rollback(System.Collections.IDictionary savedState) {
            base.Rollback(savedState);
            if (Registry.CurrentUser.GetValue(@"Software\LT10") != null)
                Registry.CurrentUser.DeleteSubKeyTree(@"Software\LT10");
        }
        public override void Commit(System.Collections.IDictionary savedState) {
            base.Commit(savedState);
        }
    }
}
