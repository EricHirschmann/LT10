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
    public class SearchTest : Test {
        //int docid;
        //int sid;

        protected override void Init() {
            Name = "fsearch";
            testDescription = "This test calculates experimental spectra with current parameters" +
                    "then changes intensities to percentage values and calculates spectrum again";
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
            project.FirstSpectraSearchProgressChanged += new System.ComponentModel.ProgressChangedEventHandler(project_FirstSpectraSearchProgressChanged);
            project.FirstSpectraSearchCompleted += new AsyncFirstSpectraSearchCompletedEventHandler(project_FirstSpectraSearchCompleted);
            List<ISpectrum> spectra = new List<ISpectrum>();
            foreach (ISpectraContainer doc in project.Containers)
                if (doc.Enabled)
                    spectra.Add(doc.Spectra[0]);
            project.FirstSpectraSearch(spectra);
        }

        void project_FirstSpectraSearchCompleted(object sender, AsyncFirstSpectraSearchCompletedEventArgs args) {
            foreach (ISpectrum spectrum in args.Spectra)
                Console.WriteLine("{0}: {1}", spectrum.Name, spectrum.Fit);
        }

        void project_FirstSpectraSearchProgressChanged(object sender, System.ComponentModel.ProgressChangedEventArgs e) {
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
            StringBuilder builder = new StringBuilder("Fitting following spectra in 'FirstSpectraSearch' manner:\n");
            foreach (ISpectraContainer doc in project.Containers)
                if (doc.Enabled)
                    builder.AppendFormat("\t-> {0}\n", doc.Spectra[0].Name);
            return builder.ToString();
        }
    }
}
