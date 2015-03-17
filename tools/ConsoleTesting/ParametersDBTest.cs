using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Evel.interfaces;
using EVEL.NeedTesting;
using System.IO;
using Evel.engine.anh;
//using Evel.engine.ParametersManagement;

namespace EVEL.ConsoleTesting {
    class ParametersDBTest : Test {

        int docid;
        int sid;

        protected override void Init() {
            Name = "specdb";
            testDescription = "Test checks classes responsible for managing parameter values - loading, extracing and etc.";
            templateflags = new Flag[] { 
                new Flag("d", typeof(int), "Document ID", false),
                new Flag("s", typeof(int), "Spectrum ID", false),
            };
        }

        protected override void RunTest(Dictionary<string, Flag> flags, Evel.interfaces.IProject project) {
            //List<ISpectrum> spectra = new List<ISpectrum>();
            //spectra.Add(project.Containers[docid].Spectra[sid]);
            //Console.WriteLine("Spectrum parameters values before fitting");
            //Program.PrintSpectrum(spectra[0]);
            //Console.Write("Creating ParameterValuesRecord object...");
            //ParameterValuesRecord record = new ParameterValuesRecord(spectra);
            //Console.WriteLine("OK");
            //Console.Write("Fitting selected spectrum...");
            //project.FirstSpectraSearch(spectra);
            //Console.WriteLine("OK");
            //Console.WriteLine("Spectrum parameters values after fitting");
            //Program.PrintSpectrum(spectra[0]);

            
            //Console.Write("Filling spectrum parameters with previous values...");
            //record.FillSpectrum(spectra[0]);
            //Console.WriteLine("OK");
            //Program.PrintSpectrum(spectra[0]);
            Console.WriteLine("To test ParameterValuesRecord change access modifiers");
        }

        public override string TestParametersInfo(Dictionary<string, Flag> flags, Evel.interfaces.IProject project) {
            docid = flags.ContainsKey("d") ? (int)flags["d"].value : 0;
            sid = flags.ContainsKey("s") ? (int)flags["s"].value : 0;
            return String.Format("\n - Test will be performed for document {0}\n - Spectrum {1} is about to be analysed\n",
                            project.Containers[docid].Name,
                            project.Containers[docid].Spectra[sid].Name);
        }
    }
}
