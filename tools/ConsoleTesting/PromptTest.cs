using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Evel.engine.anh;
using Evel.interfaces;
using EVEL.NeedTesting;
using System.IO;

namespace EVEL.ConsoleTesting {
    public class PromptTest : Test {
        int docid;
        int sid;
        int sercount;

        protected override void Init() {
            Name = "prompt";
            testDescription = "This test calculates experimental spectra with current parameters in loop " +
                    "preparing prompt intensities to search and then normalizing them. NEED is thought to " + 
                    "be unstable when norming prompt.";
            templateflags = new Flag[] { 
                new Flag("d", typeof(int), "Document ID", false),
                new Flag("s", typeof(int), "Spectrum ID", false),
                new Flag("c", typeof(int), "Series count", false)
            };
        }

        protected override void RunTest(Dictionary<string, Flag> flags, Evel.interfaces.IProject project) {
            //int M;
            //int s, i, chiChannelCount;
            //ISpectrum spectrum = project.Containers[docid].Spectra[sid];
            //AnhSpectraContainer container = (AnhSpectraContainer)spectrum.Container;
            //M = (int)spectrum.Parameters["ranges"].Components[0]["stop"].Value - (int)spectrum.Parameters["ranges"].Components[0]["start"].Value;
            //double[] diffs = new double[M];
            throw new NotSupportedException("No prompt preparing method exists");
            //container.preparePromptInts(spectrum);

            //for (s = 0; s < sercount; s++) {

            //    project.Flags &= ~SearchFlags.IncludeInts;

                

            //    container.getEvaluationArray(spectrum, diffs);
            //    SaveSpectrum(diffs, String.Format("diffs_woints_{0}", s));


            //    container.reduceIntsFromCounts(spectrum);
            //    project.Flags |= SearchFlags.IncludeInts;

            //    container.getEvaluationArray(spectrum, diffs);
            //    SaveSpectrum(diffs, String.Format("diffs_wints_{0}", s));

            //    project.Flags &= ~SearchFlags.IncludeInts;
            //    //container.normalizeInts(spectrum, true);

            //    chiChannelCount = 0;
            //    spectrum.Fit = 0;
            //    for (i = 0; i < M; i++) {
            //        //if (i < spectrum.EffectEndChannel) {
            //            spectrum.Fit += diffs[i] * diffs[i];
            //            chiChannelCount++;
            //        //}
            //    }
            //    spectrum.Fit /= chiChannelCount - 4; //4 to strzał

            //    Console.WriteLine("chisq{0}: {1}", s, spectrum.Fit);

            //}
        }

        private void SaveSpectrum(double[] s, string fileName) {
            string diffsFileName = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, Path.ChangeExtension(fileName, "txt"));
            Console.WriteLine("Saving {0}", diffsFileName);
            Utilities.SaveArray(s, diffsFileName);
        }

        public override string TestParametersInfo(Dictionary<string, Flag> flags, Evel.interfaces.IProject project) {
            docid = flags.ContainsKey("d") ? (int)flags["d"].value : 0;
            sid = flags.ContainsKey("s") ? (int)flags["s"].value : 0;
            sercount = flags.ContainsKey("c") ? (int)flags["c"].value : 5;
            return String.Format("\n - Test will be performed for document {0}\n - Spectrum {1} is about to be analysed {2} times\n",
                            project.Containers[docid].Name,
                            project.Containers[docid].Spectra[sid].Name,
                            sercount);
        }
    }
}
