using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Evel.engine.anh;
using Evel.interfaces;
using EVEL.NeedTesting;
using System.IO;
using Evel.engine.algorythms;

namespace EVEL.ConsoleTesting {
    public class SeriesSearchTest : Test {
        //int docid;
        //int sid;
        private IProject project;

        protected override void Init() {
            Name = "ssearch";
            testDescription = "Series search test. Calculating currently loaded project.";
            templateflags = new Flag[] { 
                //new Flag("d", typeof(int), "Document ID", true),
                //new Flag("s", typeof(int), "Spectrum ID", false),
            };
        }

        protected override void RunTest(Dictionary<string, Flag> flags, Evel.interfaces.IProject project) {

            //Console.WriteLine(project.Containers[docid].GetType().Assembly.Location);
            //Console.WriteLine(typeof(AnhSpectraContainer).Assembly.Location);
            //Console.ReadLine();
            //ISpectrum spectrum = project.Containers[docid].Spectra[sid];
            project.SearchProgressChanged += new System.ComponentModel.ProgressChangedEventHandler(project_SearchProgressChanged);
            project.SearchCompleted += new System.ComponentModel.AsyncCompletedEventHandler(project_SearchCompleted);
            this.project = project;
            //project.SeriesSearch(null, project.Containers);
        }

        void project_SearchCompleted(object sender, System.ComponentModel.AsyncCompletedEventArgs e) {
            Console.WriteLine("Project fit: {0}", project.Fit);
        }

        void project_SearchProgressChanged(object sender, System.ComponentModel.ProgressChangedEventArgs e) {
            FitProgressChangedEventArgs me = (FitProgressChangedEventArgs)e;
            Console.WriteLine(me.Chisq);
        }

        //private void SaveSpectrum(double[] s, string fileName) {
        //    string diffsFileName = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, Path.ChangeExtension(fileName, "txt"));
        //    Console.WriteLine("Saving {0}", diffsFileName);
        //    Utilities.SaveArray(s, diffsFileName);
        //}

        public override string TestParametersInfo(Dictionary<string, Flag> flags, Evel.interfaces.IProject project) {
            //docid = (int)flags["d"].value;
            //sid = flags.ContainsKey("c") ? (int)flags["c"].value : 0;
            //return String.Format("\n - Test will be performed for document {0}\n - Spectrum {1} is about to be analysed\n",
            //                project.Containers[docid].Name,
            //                project.Containers[docid].Spectra[sid].Name);
            return String.Format("Fitting all spectra in project {0}", project.Caption);
        }
    }
}
