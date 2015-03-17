using System;

namespace Evel.engine.anh {
    public class FastLtCurve {

        private const double sqrln = 0.600561204393225; // 1 / (2 * Math.Sqrt(Math.Log(2)) )


        unsafe private static double F(double intensity, double tau, double fwhm, int channel, double scale, double x0) {
            double s = fwhm * sqrln;
            double t = (channel + x0) * scale;
            return intensity / 2 * (Y(tau, s, t) - Y(tau, s, t + scale) -
                Erf.erfc((t + scale) / s) + Erf.erfc(t / s));
        }

        unsafe private static double Y(double tau, double s, double t) {
            return Math.Exp(s * s / 4 / tau / tau) * Erf.erfc(s / 2 / tau - t / s) * Math.Exp(-t / tau);
        }

        unsafe public static void Shape(double[] a, double intensity, double tau, double fwhm, double scale, int start, int stop, double x0) {
            int i;
            for (i = start; i < stop; i++)
                a[i] = F(intensity, tau, fwhm, i, scale, x0);
        }

    }
}
