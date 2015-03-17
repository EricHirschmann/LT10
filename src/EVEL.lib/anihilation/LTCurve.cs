using System;
using Evel.share;
using Evel.interfaces;

namespace Evel.engine.anh {

    public class LTCurveParams : ICurveParameters {
        public Evel.interfaces.IComponent component;
        public Evel.interfaces.IComponent promptComponent;
        public int id;
        public double fraction;
        public double tau;
        public double dispersion;
        public double fwhm;
        public double tauleft;
        public double tauright;
        public double shift;          //actual shift
        public double zeroCorrection; //first shift
        public int nstart;
        public int nstop;
        public bool lf;
        public bool rt;
        public bool with_fi;
        public bool diff;
        public double cs;
        public double bs;
    }

    public class LTCurve {

        //public const double VARIABLE_ACCURACY = 1e-33;

        private double[] pp;

        public LTCurve() {
            this.pp = PP.getPPArray();
        }

        //private double tau;
        //private double dispersion;
        //private double fwhm;
        //private double p.tauleft;
        //private double p.tauright;
        //private double x0;
        //private int nstart;
        //private int nstop;
        //private bool lf;
        //private bool rt;
        //private bool with_fi;
        //private bool diff;
        //private double cs;
        //private double bs;

        private LTCurveParams p;

        private double[] prompt_left;
        private double[] prompt_right;


        //public void curve(ref double[] component, double tauC, double dispersionC,
        //    double fwhmC, double p.tauleftC, double p.taurightC, double x0C,
        //    int nstartC, int nstopC, bool lfC, bool rtC, bool with_fiC,
        //    bool diffC, double csC, double bsC) {
        public void curve(ref double[] component, LTCurveParams p) {
            this.p = p;
            if (p.dispersion > 0) {
                p.dispersion = Math.Log(1 + Math.Pow(p.dispersion / p.tau, 2));
                p.tau = p.tau * Math.Exp(-p.dispersion / 2);
                p.dispersion = Math.Sqrt(p.dispersion);
            }
            if (prompt_left == null)
                setArrays();
            else
                if (prompt_left.Length < p.nstop + 2)
                    setArrays();
            doCurve0(component);

            //FastLtCurve.Shape(component, 1, p.tau, p.fwhm, p.bs, p.nstart, p.nstop, -p.shift-p.cs);

        }

        private void setArrays() {
            prompt_left = new double[p.nstop + 2];
            prompt_right = new double[p.nstop + 2];
            Fi0 = new double[p.nstop + 2];
            YY = new double[p.nstop + 2];
        }

        //private class Curv0 {

        #region Curv0

        private static readonly double AL = 0.6366197;
        private static readonly double BT = 1.1283791;
        private double _t, _tt, SIG, MI, MI_half, sqr_MI_half, CS1, _X, X2, _H,
         ED, BN, DD, AN, CN, DC, XX, _P, XPP, HPP, lambda,
         x_0, disp2, psi_avg, ni_avg, mi_avg, cc;
        private /*static*/ int IX, NSMEM, NSTOPM, K1, K2, K_STEP;//, _K;
        private /*static*/ double[] Mod_Data, Fi0, YY;

        private double skala(int k) { //k-chanel's number, cs-zero channel, bs-scale coefficient
            return (k - p.cs) * p.bs - p.shift;
            //return (k - p.cs - p.x0) * p.bs;
        }

        private double exp_(double x) {
            return (Math.Abs(x) > 86) ? 0 : Math.Exp(x);
        }

        private int channel(double x) {
            //Convertion of the time scale into channel scale 
            //VAR K:INTEGER;xx:double;
            double xx = (x * SIG - CS1) / p.bs;
            if (Math.Abs(xx) > int.MaxValue - 1)
                xx = xx / xx * (int.MaxValue - 1);
            //K:=TRUNC(XX);
            int k = (int)Math.Floor(xx);
            //K- has to belong to the range from NSTART to NSTOP
            k = Math.Max(k, p.nstart - 1);// MAX0(K,NSTART-1);
            k = Math.Min(k, NSTOPM); // K:=MIN0(K,NSTOPM);
            if (k < 0)
                k = 0;
            return k;
        }

        private void init1(double _al, double _bt) {
            ED = exp_(-_H * (2.0 * _al * _X + _bt));
            BN = exp_(-_al * _H * _H);
            DD = BN * BN;
            X2 = _X * _X;
        }

        private void progress() {
            _X += _H;
            AN = AN * ED * BN;
            BN = BN * DD;
        }

        private double Y_1()/*Vresion*/ {
            /*Version 1
            Calculates function
             Y(tau,t)=exp(sqr(s/2/tau))*Fi(sigma/2/tau-t/tau)*exp(-t/tau)*/
            /*Version 2
             calculates function erfc*/
            progress();
            XPP -= HPP;
            XX = _X * 50.0 + 1.0000001;
            IX = (int)Math.Truncate(XX);
            if (IX < 200) {
                //_P = PP.getInstance()[IX];
                _P = this.pp[IX];
                return AN * (1 - _X * _X * _X * XPP - _P - (this.pp[IX + 1] - _P) * (XX - Math.Truncate(XX))); // Frac(xx));
            } else return 0;
        }

        private /*static*/ double Y_2() {/* for big arguments*/
            /*Version 1
            Calculates function
             Y(tau,t)=exp(sqr(s/2/tau))*Fi(sigma/2/tau-t/tau)*exp(-t/tau)*/
            /*Version 2
             calculates function erfc*/
            progress();
            _t = 1 / _X;
            _tt = _t * _t;
            return AN * 0.564189583547756 * _t * (1 - 0.5 * _tt * (1 - 1.5 * _tt * (1 - 2.5 * _tt)));
        }

        private void fun_Y(double lambda) {
            int n;
            p.nstart = channel(-4.2) + 1;
            p.nstop = channel(20.0 / Math.Abs(lambda) / SIG + 10);
            //range of non-zero result chosen arrbitrary
            if (p.nstart > p.nstop - 1) return;
            //function Y has to be determined in the range bigger by 1
            for (n = NSMEM; n <= NSTOPM + 1; n++)
                YY[n] = 0;
            //Determine Y function
            if (Math.Abs(1 / lambda) > 0.005 * SIG) {//for very short lifetimes Y is practically 0

                MI = SIG * Math.Abs(lambda);
                MI_half = 0.5 * MI;
                sqr_MI_half = MI_half * MI_half;

                //range of negative values of X
                K1 = channel(MI_half);    //last channel with X<=0
                K2 = channel(MI_half - 3.5 + _H); //first channel with X<=0
                _X = MI_half - (skala(K1 + 1) - p.zeroCorrection) / SIG; //sigma/2/tau-t/sigma
                if (_X + _H > 1e-8) {
                    if (_X < 4 - (K1 - K2 + 1) * _H) {
                        XPP = 0.1027726 - 0.019128447 * _X;

                        init1(AL, BT); //recurrential clculation of Y
                        AN = 0.5 * exp_(-AL * X2 - BT * _X - sqr_MI_half + MI * _X);
                        ED = ED * exp_(MI * _H);

                        for (n = K1; n >= K2; n--)
                            YY[n] = Y_1();//{Version 1};
                    } else
                        K2 = K1 + 1;
                    init1(1, 0);//recurrential clculation of Y for very big X
                    AN = 0.5 * exp_(-X2 - sqr_MI_half + MI * _X);
                    ED = ED * Math.Exp(MI * _H);
                    for (n = K2 - 1; n >= p.nstart; n--) {
                        cc = Y_2();
                        YY[n] = (cc < 0) ? 0 : cc;
                    }
                }
                //end of Y calculations in negative range of X

                //calculate Y for positive X
                K2 = channel(3.5 + MI_half); //Determine range for calculation of exact Y

                _X = (skala(K1) - p.zeroCorrection) / SIG - MI_half;
                XPP = 0.1027726 - 0.019128447 * _X;
                init1(AL, BT);
                AN = exp_(-AL * X2 - BT * _X);
                CN = exp_(-sqr_MI_half - MI * _X);
                DC = Math.Exp(-MI * _H);
                for (n = K1 + 1; n <= K2; n++) {
                    CN = CN * DC;
                    YY[n] = CN * (1 - 0.5 * Y_1());
                }
                K2 = K2 + 1;
                for (n = K2; n <= p.nstop; n++) { //rest of Y values in the range where Y_1(Version 2) is practically equal 0
                    CN = CN * DC;
                    YY[n] = CN;
                }
                //end of Y calculation
            }
        }

        private /*static*/ void calculate_fi() //1-erf
       {
            int n;
            //calculates Fi function
            for (n = NSMEM; n <= NSTOPM + 1; n++)
                Fi0[n] = 1;
            //first for negative values of time
            K1 = channel(0);
            K2 = channel(-3.5 + _H);
            if (K1 >= p.nstart) {
                _X = -(skala(K1 + 1) - p.zeroCorrection) / SIG;
                XPP = 0.1027726 - 0.019128447 * _X;
                init1(AL, BT);
                AN = exp_(-AL * X2 - BT * _X);
                for (n = K1; n >= K2; n--)
                    Fi0[n] = 1 - 0.5 * Y_1();
                //for very big argument
                if (p.nstart <= K2) {
                    init1(1, 0);
                    AN = exp_(-X2);
                    for (n = K2 - 1; n >= p.nstart; n--)
                        Fi0[n] = 1 - 0.5 * Y_2();
                }
            } //end of fi calculation for t<0
            //determine Fi dla t>0
            _X = (p.bs * K1 + CS1) / SIG;
            XPP = 0.1027726 - 0.019128447 * _X;
            init1(AL, BT);
            AN = exp_(-AL * X2 - BT * _X);
            K2 = channel(3.7);
            for (n = K1 + 1; n <= K2; n++)
                Fi0[n] = Y_1() * 0.5;

            //for big argument
            for (n = K2; n <= NSTOPM + 1; n++)
                Fi0[n] = 0;
            K_STEP = K1;
        }
        private /*static*/ void y_discrete(double lamb) {
            /*determines Y_functions when p.tauleft or p.tauright is present
            lf=true means p.tauleft>0
             rt=true p.tauright>0*/
            double taup;
            double del;
            double ni;
            double mi;
            double psi;
            int i;
            if (!p.lf && !p.rt) {
                fun_Y(lamb);
                for (i = p.nstart; i <= p.nstop; i++)
                    Mod_Data[i] = YY[i];
                //YY.CopyTo(Mod_Data, 0);
            } else {
                if (!p.lf && p.rt) {

                    taup = 1 / lamb;
                    del = taup - p.tauright;
                    ni = taup;
                    psi = p.tauright;
                    fun_Y(lamb);
                    if (del > 1e-3)
                        for (i = p.nstart; i <= p.nstop; i++)
                            YY[i] = (ni * YY[i] - psi * prompt_right[i]) / del;
                } else {
                    if (p.lf && !p.rt) {
                        taup = 1 / lamb;
                        ni = taup / (taup + p.tauleft);
                        mi = p.tauleft / (taup + p.tauleft);
                        fun_Y(lamb);
                        mi_avg = mi;
                        ni_avg = ni;
                        for (i = p.nstart; i <= p.nstop; i++)
                            YY[i] *= ni;
                    } else {
                        taup = 1 / lamb;
                        del = taup - p.tauright;

                        double alf = (taup + p.tauleft);
                        double bet = (p.tauright + p.tauleft);
                        ni = taup * taup / alf; // sqr(taup)/alf; 
                        double chi = p.tauright * p.tauright / bet;   // sqr(p.tauright)/bet;
                        psi = (p.tauright * taup + p.tauleft * (taup + p.tauright)) / alf / bet;
                        mi = p.tauleft * p.tauleft / alf / bet; //sqr(p.tauleft)/alf/bet;

                        fun_Y(lamb);
                        psi_avg = psi;
                        mi_avg = mi;
                        if (Math.Abs(del) > 1e-4)
                            for (i = p.nstart; i <= p.nstop; i++)
                                YY[i] = (ni * YY[i] - chi * prompt_right[i]) / del;
                        else {
                            del = taup * (taup + 2 * p.tauleft) / (taup + p.tauleft) / (taup + p.tauleft);
                            for (i = p.nstart; i <= p.nstop; i++)
                                YY[i] *= del;
                        }
                    }
                }
            }
        }


        /// <summary>
        /// x_0=ln(lambda)
        /// disp2=dispersion/sqrt(2)
        /// norm=1/sqrt(2*pi)/dispersion
        /// </summary>
        /// <param name="x">ln(actual rate of annihilation)</param>
        /// <param name="c"></param>
        /// <param name="norm">1/sqrt(2*pi)/dispersion</param>
        private /*static*/ void contribution(double x, double c, double norm) {
            double lamb;
            int i;
            if (Math.Abs(x) < 80)
                lamb = Math.Exp(x);
            else {
                lamb = Math.Exp((x < 0) ? -80 : 80);
            }
            double a_norm = Math.Pow((x - x_0) / disp2, 2);
            if (a_norm < 20) {
                a_norm = c * Math.Exp(-a_norm + norm);
                if (!p.lf && !p.rt) {

                    fun_Y(lamb);
                    for (i = p.nstart; i <= p.nstop; i++)
                        Mod_Data[i] += a_norm * YY[i];
                } else {
                    if (!p.lf && p.rt) {
                        double taup = 1 / lamb;
                        double del = taup - p.tauright;
                        if (Math.Abs(del) < 1e-5) {
                            del = 1e-5;
                            lamb = 1 / (p.tauright + del);
                        }

                        double ni = taup;
                        double psi = p.tauright;
                        fun_Y(lamb);
                        for (i = p.nstart; i <= p.nstop; i++)
                            Mod_Data[i] += a_norm * (ni * YY[i] - psi * prompt_right[i]) / del;
                    } else {
                        if (p.lf && !p.rt) {
                            double taup = 1 / lamb;
                            double ni = taup / (taup + p.tauleft);
                            double mi = p.tauleft / (taup + p.tauleft);
                            fun_Y(lamb);
                            mi_avg = mi_avg + a_norm * mi;
                            ni_avg = ni_avg + a_norm * ni;
                            for (i = p.nstart; i <= p.nstop; i++)
                                Mod_Data[i] += a_norm * ni * YY[i];
                        } else {
                            double taup = 1 / lamb;
                            double del = taup - p.tauright;
                            if (Math.Abs(del) < 1e-5) {
                                del = 1e-5;
                                lamb = 1 / (p.tauright + del);
                            }
                            double alf = (taup + p.tauleft);
                            double bet = (p.tauright + p.tauleft);
                            double ni = (taup / alf) * taup;
                            double chi = Math.Pow((p.tauright) / bet, 2);
                            double psi = (p.tauright * taup + p.tauleft * (taup + p.tauright)) / alf / bet;
                            double mi = Math.Pow((p.tauleft) / alf / bet, 2);

                            fun_Y(lamb);
                            psi_avg = psi_avg + a_norm * psi;
                            mi_avg = mi_avg + a_norm * mi;
                            for (i = p.nstart; i <= p.nstop; i++)
                                Mod_Data[i] += a_norm * (ni * YY[i] - chi * prompt_right[i]) / del;
                        }
                    }
                }
            }
        }

        //private void setLength(out double[] array, int dim) {
        //    //try {
        //    //    if (array.Length != dim)
        //    //        array = new double[dim];
        //    //} catch {
        //    array = new double[dim];
        //    //}
        //}

        private /*static*/ void doCurve0(double[] component) {

            Mod_Data = component;
            int n;
            if (p.lf && p.rt)
                if ((p.tauleft < 1e-6) && (p.tauright < 1e-6))
                    p.tauleft = 1e-6;
            SIG = p.fwhm * 0.6005612;//{1/(2*sqrt(ln(2)))};
            //CS1 = -p.cs * p.bs - p.x0;
            CS1 = -p.cs * p.bs - p.shift - p.zeroCorrection;

            //if (p.tau > 1e-8 * p.bs)
            //    lambda = 1 / Math.Abs(p.tau);
            //else
            //    lambda = 1e8 / p.bs;
            if (Math.Abs(p.tau) < 1e-5)
                lambda = 1e5;
            else
                lambda = 1 / Math.Abs(p.tau);


            NSMEM = p.nstart;
            NSTOPM = p.nstop;

            p.nstart = channel(-4.2) + 1;
            p.nstop = channel(20 / lambda / SIG + 10);
            //arbitrary chosen range of non-zero Mod_Data

            if (p.nstart > p.nstop - 1) return;// null;

            p.nstop++;
            //function Y has to be determined in the range bigger by 1
            _H = p.bs / SIG; //increment of X
            HPP = 0.019128447 * _H; //increment of XPP
            for (n = NSMEM; n <= NSTOPM + 1; n++)
                Mod_Data[n] = 0;
            if (p.with_fi) calculate_fi();
            if (p.dispersion < 0.0000049)
                y_discrete(lambda);
            else
                /*IntegrateFunY.*/
                doIntegrateFunY();
            if (p.with_fi) {
                if (!p.lf) {
                    for (n = NSMEM; n <= NSTOPM + 1; n++)
                        Mod_Data[n] = YY[n] + Fi0[n];
                } else {
                    if (p.lf && p.rt) {
                        for (n = NSMEM; n <= NSTOPM + 1; n++)
                            Mod_Data[n] = YY[n] + ni_avg * Fi0[n];
                    } else {
                        for (n = NSMEM; n <= NSTOPM + 1; n++)
                            Mod_Data[n] = YY[n] + psi_avg * Fi0[n];
                    }
                }
            }
            
            if (p.diff)
                for (n = NSMEM; n <= NSTOPM; n++) {
                    if (Math.Abs(Mod_Data[n + 1]) > share.Constants.VARIABLE_ACCURACY)
                        Mod_Data[n] = Mod_Data[n] - Mod_Data[n + 1];
                    else
                        Mod_Data[n] = 0;
                }
            if (p.lf)
                for (n = NSMEM; n <= NSTOPM; n++)
                    Mod_Data[n] += mi_avg * prompt_left[n];

            p.nstart = NSMEM;
            p.nstop = NSTOPM;
            //Mod_Data.CopyTo(component, 0);
            //return Mod_Data;
        }

        //private /*static*/ class IntegrateFunY {
        #region IntegrateFunY fields and methods
        private static readonly double t1 = -0.86113631;
        private static readonly double t2 = -0.33998104;
        private static readonly double a1 = 0.34785484;
        private static readonly double a2 = 0.65214516;
        private /*static*/ double sum;
        private /*static*/ double dif;
        private /*static*/ double[] a;// = new double[21];
        private /*static*/ double[] b;// = new double[21];//               array[-10..10] of double;
        private /*static*/ double norm;

        private double getx(double t) {
            return sum + dif * t;
        }

        private double getc(double getc_a) {
            return dif * getc_a;
        }

        /// <summary>
        /// determines 4 points for integration with the Gauss method
        /// and interrate the y function on lambda
        /// </summary>
        /// <param name="a"></param>
        /// <param name="b"></param>
        private /*static*/ void gauss_integral_4(double gi4_a, double gi4_b) {

            sum = (gi4_a + gi4_b) / 2;
            dif = (gi4_b - gi4_a) / 2;
            double c1 = dif * a1;
            double c2 = dif * a2;
            double xx = sum + dif * t1; contribution(xx, c1, norm);
            xx = sum + dif * t2; contribution(xx, c2, norm);
            xx = sum - dif * t2; contribution(xx, c2, norm);
            xx = sum - dif * t1; contribution(xx, c1, norm);


        }

        /*public*/
        private /*static*/ void doIntegrateFunY() {
            int i;
            int j;
            norm = Math.Log(1 / Math.Sqrt(2 * Math.PI) / p.dispersion);
            disp2 = p.dispersion * Math.Sqrt(2);
            x_0 = Math.Log(lambda);
            for (j = NSMEM; j <= NSTOPM + 1; j++)
                Mod_Data[j] = 0;
            psi_avg = 0;
            mi_avg = 0;
            ni_avg = 0;
            int n1;
            int n2;
            if (p.dispersion > 0) {
                /*a[-2] = -6 * dispersion; b[-2] = -3 * dispersion;
                a[-1] = b[-2]; b[-1] = -dispersion;
                a[0] = b[-1]; b[0] = dispersion;
                a[1] = b[0]; b[1] = 3 * dispersion;
                a[2] = b[1]; b[2] = 6 * dispersion;
                n1 = -2; n2 = 2;*/
                a = new double[5];
                b = new double[5];
                a[0] = -6 * p.dispersion; b[0] = -3 * p.dispersion;
                a[1] = b[0]; b[1] = -p.dispersion;
                a[2] = b[1]; b[2] = p.dispersion;
                a[3] = b[2]; b[3] = 3 * p.dispersion;
                a[4] = b[3]; b[4] = 6 * p.dispersion;
                n1 = 0; n2 = 4;
            } else {
                a = new double[3];
                b = new double[3];
                a[0] = -6 * p.dispersion; b[0] = -p.dispersion;
                a[1] = b[0]; b[1] = p.dispersion;
                a[2] = b[1]; b[2] = 6 * p.dispersion;
                n1 = 0; n2 = 2;
            }
            for (i = n1; i <= n2; i++)
                gauss_integral_4(a[i] + x_0, b[i] + x_0);
            for (i = 0; i <= NSTOPM; i++) // to High(Mod_Data) do
                YY[i] = Mod_Data[i];
            //Mod_Data.CopyTo(YY, 0);
        }
        #endregion IntegrateFunY fields and methods
        //}

        //}
        #endregion Curv0
    }


}
