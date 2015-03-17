using System;
using System.Collections.Generic;
using System.Text;

namespace Evel.engine.algorythms {

    /// <summary>
    /// delegate of function being fitted
    /// </summary>
    /// <param name="parameters">parameter values</param>
    /// <param name="y">function values</param>
    /// <param name="dyda">function first derivatives for all parameters in all channels, first index is channel</param>
    public delegate void Funks(object target, double[] a, bool[] ia, out double[] y, out double[][] dyda);

    public class Fitmrq {
        public Funks f;
        const int NDONE = 4, ITMAX = 1000;
        int ma, mfit;//, datstart;
        //int ndat;
        int[][] datpos;
        double[,] y;
        double tol = 1e-3;
        bool[] ia;
        double[] a;
        public double[,] covar;
        double[,] alpha;
        public double chisq;

        #region fit arrays
        double[] atry = null;
        double[] beta = null;
        double[] da = null;
        #endregion

        public Fitmrq(Funks f) {
            this.f = f;
        }

        /// <summary>
        /// initializes arrays and sets fields
        /// </summary>
        /// <param name="datpos">indexes where data starts and ends in data buffer</param>
        /// <param name="y">data [y, sigma] matrix</param>
        /// <param name="aa">parameters</param>
        public void init(int[][] datpos, double[,] y, double[] aa, bool[] ia) {
            //this.datstart = datstart;
            //this.ndat = ndat;
            this.datpos = datpos;
            this.ma = aa.Length;
            this.y = y;
            this.ia = ia;
            this.alpha = new double[ma, ma];
            this.a = aa;
            this.covar = new double[ma, ma];
            atry = new double[ma];
            beta = new double[ma];
            da = new double[ma];
        }

        public void hold(int i, double val) { ia[i] = false; a[i] = val; }
        public void free(int i) { ia[i] = true; }

        public double fit(object target) {
            int j, k, l, iter, done = 0;
            double alamda = 0.001, ochisq;
            mfit = 0;
            for (j = 0; j < ma; j++) if (ia[j]) mfit++;
            double[,] oneda = new double[mfit, 1];
            double[,] temp = new double[mfit, mfit];
            mrqcof(target, a, alpha, beta);
            for (j = 0; j < ma; j++) atry[j] = a[j];
            ochisq = chisq;
            for (iter = 0; iter < ITMAX; iter++) {
                if (done == NDONE) alamda = 0;
                for (j = 0; j < mfit; j++) {
                    for (k = 0; k < mfit; k++) covar[j, k] = alpha[j, k];
                    covar[j, j] = alpha[j, j] * (1.0 + alamda);
                    for (k = 0; k < mfit; k++) temp[j, k] = covar[j, k];
                    oneda[j, 0] = beta[j];
                }
                GaussJordan.gaussj(temp, oneda);
                for (j = 0; j < mfit; j++) {
                    for (k = 0; k < mfit; k++) covar[j, k] = temp[j, k];
                    da[j] = oneda[j, 0];
                }
                if (done == NDONE) {
                    covsrt(ref covar);
                    covsrt(ref alpha);
                    int ndat = 0;
                    for (j = 0; j < datpos.Length; j++)
                        ndat += datpos[j][1] - datpos[j][0];
                    chisq /= ndat - mfit;
                    return chisq;
                }
                for (j = 0, l = 0; l < ma; l++)
                    if (ia[l]) atry[l] = a[l] + da[j++];
                mrqcof(target, atry, covar, da);
                if (Math.Abs(chisq - ochisq) < Math.Max(tol, tol * chisq)) done++;
                if (chisq < ochisq) {
                    alamda *= 0.1;
                    ochisq = chisq;
                    for (j = 0; j < mfit; j++) {
                        for (k = 0; k < mfit; k++) alpha[j, k] = covar[j, k];
                        beta[j] = da[j];
                    }
                    for (l = 0; l < ma; l++) a[l] = atry[l];
                } else {
                    alamda *= 10.0;
                    chisq = ochisq;
                }
            }
            throw new InvalidOperationException("Fitmrq too many iterations");
        }


        unsafe void mrqcof(object target, double[] a, double[,] alpha, double[] beta) {
            int s, i, j, k, l, m, n;
            double wt, sig2i, dy;
            double[] ymod;
            double[][] dyda; // = new double[ma];
            for (j = 0; j < mfit; j++) {
                for (k = 0; k <= j; k++) alpha[j, k] = 0.0;
                beta[j] = 0.0;
            }
            chisq = 0.0;
            f(target, a, ia, out ymod, out dyda);

            for (s = 0; s < datpos.Length; s++) {
                n = 1; //TODO : nie 1! tutaj ma byc numer kanalu "start"
                for (i = datpos[s][0]; i <= datpos[s][1]; i++) {
                    sig2i = 1.0 / y[i, 1] / y[i, 1];
                    dy = y[i, 0] - ymod[i];
                    for (j = 0, l = 0; l < ma; l++) {
                        if (ia[l]) {
                            wt = dyda[l][n] * sig2i;
                            for (k = 0, m = 0; m < l + 1; m++)
                                if (ia[m]) alpha[j, k++] += wt * dyda[m][n];
                            beta[j++] += dy * wt;
                        }
                    }
                    chisq += dy * dy * sig2i;
                    n++;
                }
            }
            for (j = 1; j < mfit; j++)
                for (k = 0; k < j; k++) alpha[k, j] = alpha[j, k];
        }

        void covsrt(ref double[,] covar) {
            int i, j, k;
            for (i = mfit; i < ma; i++)
                for (j = 0; j < i + 1; j++) covar[i, j] = covar[j, i] = 0.0;
            k = mfit - 1;
            for (j = ma - 1; j >= 0; j--) {
                if (ia[j]) {
                    for (i = 0; i < ma; i++) GaussJordan.swap(ref covar[i, k], ref covar[i, j]);
                    for (i = 0; i < ma; i++) GaussJordan.swap(ref covar[k, i], ref covar[j, i]);
                    k--;
                }
            }
        }

    }
}