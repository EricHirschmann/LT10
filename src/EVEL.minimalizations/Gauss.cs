using System;

namespace Evel.engine.algorythms {

    public class EquationRow {
        public double[] coeff;
        public double b;
        public EquationRow(double[] coeff, double b) {
            this.coeff = coeff;
            this.b = b;
        }
        public override string ToString() {
            System.Text.StringBuilder builder = new System.Text.StringBuilder();
            foreach (double d in coeff)
                builder.AppendFormat("{0}\t", d.ToString("F05"));
            builder.AppendFormat("\t{0}", b.ToString("F05"));
            return builder.ToString();
        }
    }

    /// <summary>
    /// Solving system of linear equations
    /// </summary>
    public class Gauss {

        private static void subtractRow(EquationRow u, EquationRow v, double m, int k, int n) {
            for (int i = k; i <= n; i++)
                u.coeff[i] -= m * v.coeff[i];
            u.b -= m * v.b;
        }

        private static void swapPivRow(EquationRow[] a, int col, int n) {
            double max = Math.Abs(a[col].coeff[col]);
            int pivi = col;
            for (int i = col + 1; i <= n; i++)
                if (Math.Abs(a[i].coeff[col]) > max) {
                    max = Math.Abs(a[i].coeff[col]);
                    pivi = i;
                }
            if (pivi != col) {
                EquationRow rowp = a[col];
                a[col] = a[pivi];
                a[pivi] = rowp;
            }
        }

        public static bool solve(EquationRow[] equations, double[] x, int n) {
            const double assumedzero = 1e-30;   
            //int n = equations.Length-1;
            //x = null;       
            for (int j = 0; j < n; j++) {

                swapPivRow(equations, j, n);
                EquationRow pivotalrow = equations[j];
                double pivot = pivotalrow.coeff[j];
                if (Math.Abs(pivot) <= assumedzero) {
                    
                    return false;
                }
                for (int i = j + 1; i <= n; i++) {
                    double mult = equations[i].coeff[j] / pivot;
                    if (Math.Abs(mult) > assumedzero) {
                        equations[i].coeff[j] = mult;
                        subtractRow(equations[i], pivotalrow, mult, j + 1, n);
                    } else
                        equations[i].coeff[j] = 0;
                }
            }
            bool result = Math.Abs(equations[n].coeff[n]) > assumedzero;
            if (result) {
                //x = new double[equations.Length];
                x[n] = equations[n].b / equations[n].coeff[n];
                for (int i = n - 1; i >= 0; i--) {
                    double top = equations[i].b;
                    for (int k = i + 1; k <= n; k++)
                        top -= equations[i].coeff[k] * x[k];
                    x[i] = top / equations[i].coeff[i];
                }
            }
            return result;
        }

    }
}
