using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Evel.interfaces;
using EVEL.NeedTesting;
using System.IO;

namespace EVEL.ConsoleTesting {
    public class Test1 : Test {

        protected int docid;
        private int series;
        protected int sid;

        protected override void Init() {
            Name = "test1";
            testDescription = "This test calculates experimental spectra with current parameters" +
                    "for each spectrum in project's document in few series. Each time" +
                    "chosen spectrum is calculated array of differences is saved to file." +
                    "Test checks whether the method for determining the theoretical spectrum is stable or not.";
            templateflags = new Flag[] { 
                new Flag("d", typeof(int), "Document ID", true),
                new Flag("s", typeof(int), "Spectrum ID", false),
                new Flag("c", typeof(int), "Series count", false)
            };
        }

        public override string TestParametersInfo(Dictionary<string, Flag> flags, IProject project) {
            docid = (int)flags["d"].value;
            series = flags.ContainsKey("s") ? (int)flags["s"].value : 5;
            sid = flags.ContainsKey("c") ? (int)flags["c"].value : 0;
            return String.Format("\n - Test will be performed for document {0}\n - Series of spectra will be calulated {1} times\n - Differences for spectrum {2} will be saved\n",
                            project.Containers[docid].Name,
                            series, project.Containers[docid].Spectra[sid].Name);
        }

        protected override void RunTest(Dictionary<string, Flag> flags, IProject project) {
            double[] diffs = null;
            for (int seriesId = 1; seriesId <= series; seriesId++) {
                for (int specId = 0; specId < project.Containers[docid].Spectra.Count; specId++) {
                    ISpectrum spectrum = project.Containers[docid].Spectra[specId];
                    int start = (int)spectrum.Parameters[4].Components[0][1].Value;
                    int stop = (int)spectrum.Parameters[4].Components[0][2].Value;
                    if (diffs == null) {
                        diffs = new double[stop - start];
                    } else if (diffs.Length < stop - start)
                        diffs = new double[stop - start];
                    project.Containers[docid].getEvaluationArray(spectrum, diffs);
                    if (specId == sid) {
                        string diffsFileName = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, String.Format("diff{0}.txt", seriesId));
                        Console.WriteLine("Saving {0}", diffsFileName);
                        Utilities.SaveArray(diffs, diffsFileName);
                    }
                }
            }
        }

    }
}
