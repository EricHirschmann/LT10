using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Evel.engine.anh;
using Evel.interfaces;
using EVEL.NeedTesting;
using System.IO;

namespace EVEL.ConsoleTesting {
    public class FSearchTest : Test {
        int docid;
        int sid;

        protected override void Init() {
            Name = "fsearch";
            testDescription = "Runs minimalization function for chosen spectrum of chosen document.";
            templateflags = new Flag[] { 
                new Flag("d", typeof(int), "Document ID", false),
                new Flag("s", typeof(int), "Spectrum ID", false)
            };
        }

        protected override void RunTest(Dictionary<string, Flag> flags, Evel.interfaces.IProject project) {
            ISpectrum spectrum = project.Containers[docid].Spectra[sid];
            //AnhProject anhproject = (AnhProject)project;
            AnhSpectraContainer container = (AnhSpectraContainer)spectrum.Container;
            project.FirstSpectraSearch(new ISpectrum[] { spectrum });
            Console.WriteLine("{0} chisq: {1}", spectrum.Name, spectrum.Fit);
        }

        public override string TestParametersInfo(Dictionary<string, Flag> flags, Evel.interfaces.IProject project) {
            docid = flags.ContainsKey("d") ? (int)flags["d"].value : 0;
            sid = flags.ContainsKey("s") ? (int)flags["s"].value : 0;
            return String.Format("\n - Test will be performed for document {0}\n - Spectrum {1} is about to be fit\n",
                            project.Containers[docid].Name,
                            project.Containers[docid].Spectra[sid].Name);
        }
    }
}
