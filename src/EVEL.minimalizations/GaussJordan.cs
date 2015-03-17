using System;

namespace Evel.engine.algorythms {
    public class GaussJordan {

        /// <summary>
        /// Linear equation solution by Gauss-Jordan elimination.
        /// Produces solution of system of equations and inverses matrix a
        /// </summary>
        /// <param name="a">Coefficients matrix</param>
        /// <param name="b">Right hand side matrix</param>
        public static void gaussj(double[,] a, double[,] b) {
            int icol = 0;
            int irow = 0;
            int n = a.GetLength(0);       //row count
            int m = b.GetLength(1);    //column count
            if (a.GetLength(0) != b.GetLength(0))
                throw new ArgumentException("Cannot solve equations: B has invalid size");
            double big, dum, pivinv;
            int[] indxc = new int[n];
            int[] indxr = new int[n];
            int[] ipiv = new int[n];
            int i, j, k, l, ll;
            for (j = 0; j < n; j++) ipiv[j] = 0;
            for (i = 0; i < n; i++) {
                big = 0.0;
                for (j = 0; j < n; j++) {
                    if (ipiv[j] != 1) {
                        for (k = 0; k < n; k++) {
                            if (ipiv[k] == 0) {
                                if (Math.Abs(a[j,k]) >= big) {
                                    big = Math.Abs(a[j,k]);
                                    irow = j;
                                    icol = k;
                                }
                            }
                        }
                    }
                }
                ipiv[icol]++;
                if (irow != icol) {
                    for (l = 0; l < n; l++) swap(ref a[irow,l], ref a[icol,l]);
                    for (l = 0; l < m; l++) swap(ref b[irow,l], ref b[icol,l]);
                }
                indxr[i] = irow;
                indxc[i] = icol;
                if (a[icol,icol] == 0.0)
                    throw new ArgumentException("Cannot solve equations: A is Singular Matrix");
                pivinv = 1.0 / a[icol,icol];
                a[icol,icol] = 1.0;
                for (l = 0; l < n; l++) a[icol,l] *= pivinv;
                for (l = 0; l < m; l++) b[icol,l] *= pivinv;
                for (ll = 0; ll < n; ll++)
                    if (ll != icol) {
                        dum = a[ll,icol];
                        a[ll,icol] = 0;
                        for (l = 0; l < n; l++) a[ll,l] -= a[icol,l] * dum;
                        for (l = 0; l < m; l++) b[ll,l] -= b[icol,l] * dum;
                    }
            }
            for (l = n - 1; l >= 0; l--) {
                if (indxr[l] != indxc[l])
                    for (k = 0; k < n; k++)
                        swap(ref a[k,indxr[l]], ref a[k,indxc[l]]);
            }
        }

        public static void swap(ref double a, ref double b) {
            double c = a;
            a = b;
            b = c;
        }

    }
}
