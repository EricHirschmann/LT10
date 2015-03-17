using System;
using System.IO;

namespace Evel.engine.anh.devel {
    public class DevTools {

        public static void saveSpectrumFile(AnhSpectraContainer container, AnhSpectrum spectrum, string fileName) {
            throw new NotSupportedException();
            //TextWriter writer = new StreamWriter(fileName);
            //container.setLongestRange();
            //container.preparePromptInts(container.Spectra[0]);
            //double[] s = container.getTheoreticalSpectrum(container.Spectra[0]);
            //int stop = (int)container.Spectra[0].Parameters[4].Components[0][2].Value;
            //for (int i = 0; i <= stop; i++)
            //    writer.WriteLine(s[i]);
            //writer.Close();
        }

        public static void saveSpectrum(double[] spectrum, int stop) {
            TextWriter writer = new StreamWriter("E:/SpectraComparison/Evel.txt");
            for (int i = 0; i <= Math.Min(stop, spectrum.Length-1); i++)
                writer.WriteLine(spectrum[i]);
            writer.Close();            
        }

        public static void saveSpectrumFile(AnhSpectraContainer container, AnhSpectrum spectrum) {
            saveSpectrumFile(container, spectrum, "E:/SpectraComparison/Evel.txt");
        }

    }
}
