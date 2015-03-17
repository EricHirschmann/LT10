using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Evel.engine.anh;
using Evel.interfaces;
using EVEL.NeedTesting;
using System.IO;

namespace EVEL.ConsoleTesting {
    public class SSearchTest : Test {
        bool stype;
        System.ComponentModel.ProgressChangedEventHandler progressHandler;

        protected override void Init() {
            Name = "run";
            testDescription = "Runs minimalization function for first spectrum of chosen document.";
            templateflags = new Flag[] { 
                new Flag("series", typeof(bool), "run type:\n\t\tfalse - first spectra search (default)\n\t\ttrue - series search", false),
            };
            progressHandler = new System.ComponentModel.ProgressChangedEventHandler(project_SearchProgressChanged);
        }

        protected override void RunTest(Dictionary<string, Flag> flags, Evel.interfaces.IProject project) {
            if (!stype) {
                List<ISpectrum> spectra = new List<ISpectrum>();
                foreach (ISpectraContainer container in project.Containers)
                    spectra.Add(container.Spectra[0]);
                Console.Write("Fitting first spectra... ");
                project.FirstSpectraSearch(spectra);
                Console.WriteLine("Done!");
                foreach (ISpectrum spectrum in spectra)
                    Console.WriteLine("\t{0} chisq: {1}", spectrum.Name, spectrum.Fit);
            } else {
                Console.WriteLine("Fitting series of spectra... ");
                project.SearchProgressChanged += progressHandler;
                project.SeriesSearch(null, project.Containers);
                Console.WriteLine("Done! Global chisq: {0}", project.Fit);
                project.SearchProgressChanged -= progressHandler;
            }
        }

        void project_SearchProgressChanged(object sender, System.ComponentModel.ProgressChangedEventArgs e) {
            Evel.engine.algorythms.FitProgressChangedEventArgs args = (Evel.engine.algorythms.FitProgressChangedEventArgs)e;
            Console.WriteLine("Iteration {0}, function called {1} times, chisq: {2}", args.Iteration, args.FunctionCallCount, args.Chisq);
        }

        public override string TestParametersInfo(Dictionary<string, Flag> flags, Evel.interfaces.IProject project) {
            stype = flags.ContainsKey("series") ? (bool)flags["series"].value : false;
            return String.Format("{0}?\n", (!stype) ?
                "Fit first spectra in each project's document" :
                "Start fitting series of spectra?");
        }
    }
}
