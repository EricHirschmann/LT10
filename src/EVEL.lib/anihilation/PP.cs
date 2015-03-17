using System;

namespace Evel.engine.anh {
    class PP {  //singleton design pattern

        private static PP instance = null;

        private double[] pp = new double[202];

        protected PP() {
            double xxx = 0;
            for (int i = 1; i < pp.Length; i++) {
                pp[i] = (erfkan(xxx) - erfcc(xxx)) / Math.Exp(-(xxx * 0.6366197 + 1.1283791) * xxx);
                xxx += 0.02;
            }       
        }

        public static double[] getPPArray() {
            return getPP().pp;
        }

        private static PP getPP() {
            if (instance == null) {
                instance = new PP();
            } 
            return instance;
        }

        private double erfcc(double x) {
            double z = Math.Abs(x);
            double t = 1 / (1 + 0.5 * z);
            double tt = t * (-1.13520398 + t * (1.48851587 + t * (-0.82215223 + t * 0.17087277)));
            tt = t * (-0.18628806 + t * (0.27886807 + tt));
            double ans = t * Math.Exp(-z * z - 1.26551223 + t * (1.00002368 +
               t * (0.37409196 + t * (0.09678418 + tt))));
            return (x >= 0) ? ans : 2 - ans;
        }

        private double erfkan(double x) {
            if (x >= 3.9)
                return 0;
            else {
                double xx = Math.Abs(x);
                double w = Math.Exp(-(xx * 0.6366197 + 1.1283791) * xx) * (1 - xx * xx * xx * (0.1027726 - 0.019128447 * xx));
                return w;
            }
        }

    }
}
