using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Evel.interfaces;
using EVEL.NeedTesting;
using System.IO;
using Evel.engine.anh;

namespace EVEL.ConsoleTesting {
    class IntsChangeTest : Test {

        int docid;
        int sid;

        protected override void Init() {
            Name = "IntsChange";
            testDescription = "This test calculates experimental spectra with current parameters" +
                    "then changes intensities to percentage values and calculates spectrum again";
            templateflags = new Flag[] { 
                new Flag("d", typeof(int), "Document ID", true),
                new Flag("s", typeof(int), "Spectrum ID", false),
            };
        }

        protected override void RunTest(Dictionary<string, Flag> flags, Evel.interfaces.IProject project) {

            //Console.WriteLine(project.Containers[docid].GetType().Assembly.Location);
            //Console.WriteLine(typeof(AnhSpectraContainer).Assembly.Location);
            //Console.ReadLine();
            ISpectrum spectrum = project.Containers[docid].Spectra[sid];
            SaveSpectrum(project.Containers[docid].getTheoreticalSpectrum(spectrum), "spec1_woInts");
            ((AnhSpectraContainer)project.Containers[docid]).reduceIntsFromCounts(spectrum);
            SaveSpectrum(project.Containers[docid].getTheoreticalSpectrum(spectrum), "spec1_wInts");
        }

        private void SaveSpectrum(double[] s, string fileName) {
            string diffsFileName = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, Path.ChangeExtension(fileName, "txt"));
            Console.WriteLine("Saving {0}", diffsFileName);
            Utilities.SaveArray(s, diffsFileName);
        }

        public override string TestParametersInfo(Dictionary<string, Flag> flags, Evel.interfaces.IProject project) {
            docid = (int)flags["d"].value;
            sid = flags.ContainsKey("c") ? (int)flags["c"].value : 0;
            return String.Format("\n - Test will be performed for document {0}\n - Spectrum {1} is about to be analysed\n",
                            project.Containers[docid].Name,
                            project.Containers[docid].Spectra[sid].Name); 
        }
    }
}