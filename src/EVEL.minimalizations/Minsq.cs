using System;
using Evel.interfaces;
using System.Collections.Generic;

namespace Evel.engine.algorythms {



    public class Minsq : IFitter {

        //public event IterationEvent OnIterationEvent;
        //public event FinishEvent OnFinish;
        //public event MinsqProgressChangedEventHandler MinsqProgressChanged;
        //public event MinsqProgressChangedEventHandler MinsqCompleted;
        //public MinsqProgressChangedEventArgs progress;

        public virtual event FitChangeEventHandler Changed;
        //public virtual event FitChangeEventHandler Finished;
        public virtual event IndependencyFoundEventHandler IndependencyFound;

        //public delegate void FitChangeEventHandler(object sender, FitProgressChangedEventArgs args);
        //public delegate void GetArrayHandler(object sender, double[] f);

        #region fields
        private bool parametersSet = false; // switched to true when SetParameters(...) executed. Field prevents executing SetParameters() before SetParameters(...)

        private int _is_v;
        private int _iinc_v;
        private int _mc_v;

        private double _xinc_v;
        private double _db_v;
        private double _fb_v;
        private double _fc_v;
        private double _dc_v;
        private double _da_v;
        private double _fa_v;
        private double _d_v;


        private int MAXFUN;
        //private int MPLUSN;
        //private int KST;
        //private int NPLUS;
        //private int KINV;
        //private int KSTORE;
        //private int NN;
        //private int IINV;
        //private int KK;
        //private int ILESS;
        //private int IGAMAX;
        //private int INCINV;
        //private int INCINP;
        //private int IIP;
        //private int JL;
        //private int JJP;
        //private int KL;
        //private int ICONT;
        //private int ISS;
        private int ITC;
        //private int IPS;
        //private int IT;

        private int IPC;
        private int IPP;
        private int IPRINT = 0;
        private int MC;

        //private double SUM;
        //private double B;
        //private double BB;
        //private double FF;
        //private double CHANGE;
        //private double DM;
        //private double FC;
        //private double ACC;
        //private double XC;
        //private double XL;
        //private double FMIN;
        //private double FSEC;
        //private double xxx;
        //private double doklad;

        private double[] w_n;
        //private void putToWN(int index, double value) {
        //if (double.IsNaN(value))
        //    System.Diagnostics.Debug.Write("putting NaN!");
        //    w_n[index] = value;
        //}

        //----
        //private int M;
        //private int N, NNN;
        private int _M, _N, _NNN;
        private GetArrayHandler function;
        private IParameter[] parameters;
        private double[] x, xx, memx;
        //private double[] e;
        private double[] errors;
        //private bool[] enabled;
        //private ulong enabled;
        //private double[] delta;
        private double XSTEP;
        //private int NFUN;
        private double[] f;
        private int startChannel;
        private FitProgressChangedEventArgs fitChangeArgs;
        private int iteration;

        //private int[] aa, bbb;

        private double FFNORM;
        private int icov, jcov;
        private int IS_;

        #endregion fields

        //public Minsq() {
        //    //progress = new FitProgressChangedEventArgs();
        //}

        public int DataLength {
            get { return this._M; }
            set {
                if (this._M > value) {
                    this._M = value;
                    Parameters = this.x;
                } else
                    this._M = value;
            }
        }

        public double[] DiffsArray {
            get { return this.f; }
            set {
                this.f = value;
            }
        }

        public int StartChannel {
            get { return this.startChannel; }
            set { this.startChannel = value; }
        }

        public GetArrayHandler Function {
            get { return this.function; }
            set { this.function = value; }
        }

        public double[] Parameters {
            get { return x; }
            set {
                x = value;
                xx = new double[x.Length];
                memx = new double[x.Length];

                //aa = new int[x.Length+2];
                //bbb = new int[x.Length+2];

                //enabled = new bool[x.Length];
                //enabled = 0;
                //for (int i = 0; i < x.Length; i++)
                //    enabled[i] = true;

                x.CopyTo(xx, 0);
                errors = new double[x.Length];
                _N = x.Length;
                _NNN = _N;
                setW_Narray(_M, _N);
            }
        }

        //public double[] Delta {
        //    get { return e; }
        //    set {
        //        this.e = value;
        //    }
        //}

        public double[] Error {
            get { return this.errors; }
        }

        public int MaxIterationCount {
            get {
                return this.MAXFUN;
            }
            set {
                this.MAXFUN = value;
            }
        }

        public int Iteration {
            get { return this.ITC; }
            set { this.ITC = value; }
        }

        public void SetParameters() {
            if (!parametersSet)
                throw new InvalidOperationException("Parameters not set!");
            for (int i = 0; i < this.x.Length; i++)
                this.x[i] = this.parameters[i].Value;
        }

        public void SetParameters(IParameter[] parameters, double[] diffs, int dataLength, GetArrayHandler function) {
            this.DataLength = dataLength;
            this.DiffsArray = diffs;
            this.Function = function;
            this.parameters = parameters;
            double[] p = new double[parameters.Length];
            //double[] d = new double[parameters.Length];
            for (int i = 0; i < parameters.Length; i++) {
                p[i] = parameters[i].SearchValue;
                //d[i] = parameters[i].SearchDelta;
            }
            //this.Delta = d;
            this.Parameters = p;
            this.fitChangeArgs.FunctionCallCount = 0;
            this.parametersSet = true;
        }

        private void UpdateParameters(int parameterCount) {
            for (int i = 0; i < parameterCount; i++)
                //if ((enabled & (2u << i)) == 0)
                    this.parameters[i].SearchValue = xx[i];
        }

        private void UpdateErrors(int parameterCount) {
            for (int i = 0; i < parameterCount; i++)
                //if ((enabled & (2u << i)) == 0) {
                    if (errors[i] < 0)
                        this.parameters[i].SearchError = Math.Sqrt(Math.Abs(errors[i]));
                    else
                        this.parameters[i].SearchError = Math.Sqrt(errors[i]);
                //}
        }

        //private void setErrors(object sender, GetArrayHandler diffsFunction) {
        //    for (int i = 0; i < errors.Length; i++)
        //        errors[i - 1] = Math.Sqrt(errors[i - 1]);
        //    //ErrorsCalculator.setErrors(sender, diffsFunction, Parameters, DiffsArray);
        //}

        public Minsq(int NFUN, double XSTEP) {
            this.MAXFUN = NFUN;
            this.XSTEP = XSTEP;
            this.fitChangeArgs = new FitProgressChangedEventArgs(double.PositiveInfinity, 0, null, null);
        }

        //public Minsq(double[] parameters, double[] errors, GetArrayHandler func, int M)
        //    : this(parameters, errors, func, M, 10 * parameters.Length, 0.1) {
        //}

        //public Minsq(double[] parameters, double[] errors, GetArrayHandler func,
        //    int M, int NFUN, double XSTEP) {
        //    this.M = M;
        //    //this.f = new double[M];
        //    this.NFUN = NFUN;
        //    this.MAXFUN = NFUN;
        //    this.XSTEP = XSTEP;
        //    Function = func;
        //    Parameters = parameters;
        //    fitChangeArgs = new FitProgressChangedEventArgs(0, 0, null, null);
        //    iteration = 0;
        //}

        private void setW_Narray(int maxM, int maxN) {
            int w_nSize = (int)Math.Floor((double)(maxN + maxM * (maxN + 1) + 3 * maxN * (maxN + 1) / 2 + 1));
            if (w_n == null)
                this.w_n = new double[w_nSize];
            else {
                if (w_nSize > w_n.Length)
                    this.w_n = new double[w_nSize];
                else {
                    //for (int i = 0; i < w_nSize; i++)
                    //    w_n[i] = 0;
                }
            }
        }

        //public virtual double fit(object target) {
        //    return this.fit(target, false);
        //}

        private bool callTargetFunction(object target, double[] diffs, int parameterCount) {
            //for (int id = 0; id < diffs.Length; id++)
            //    if (double.IsNaN(w_n[id]))
            //        System.Diagnostics.Debug.Write("Nan has been evaluated!");

            UpdateParameters(parameterCount);

            //if (target is IProject) { //tylko globalny
            //if (target is System.Collections.Generic.IList<ISpectrum>) {
            //    //System.Diagnostics.Debug.Indent();
            //    //if (((IProject)target).SearchMode == SearchMode.Preliminary) {

            //        for (int i = 0; i < parameters.Length; i++)
            //            System.Diagnostics.Debug.Write(String.Format("{0}\t", parameters[i].Value));
            //        System.Diagnostics.Debug.WriteLine("");
            //        //System.Diagnostics.Debug.Unindent();
            //    //}
            //}
            fitChangeArgs.FunctionCallCount++;

            return function(target, diffs);



            //for (int id = 0; id < diffs.Length; id++)
            //    if (double.IsNaN(diffs[id]))
            //        System.Diagnostics.Debug.Write("Nan has been evaluated!");

            //<test>
            //if ((target is IProject)) {
            //    double localFF = 0;
            //    for (int i = 1; i <= M; i++)
            //        localFF += diffs[i - 1] * diffs[i - 1];

            //    System.Diagnostics.Debug.WriteLine(String.Format("{0}", localFF / (M - N)));
            //    EVEL.NeedTesting.Utilities.SaveArray(diffs, String.Format(@"d:\devel\ltvsneed\diffs_need_{0}.txt", Iteration));
            //}
            //<\test>

        }

        private void resetFields() {
            int i;
            for (i = 0; i < w_n.Length; i++)
                w_n[i] = 0.0;
            for (i = 0; i < f.Length; i++)
                f[i] = 0;

            _is_v = _iinc_v = _mc_v = 0;
            _xinc_v = _db_v = _fb_v = _fc_v = _dc_v = _da_v = _d_v = 0;

            //MPLUSN = KST = NPLUS = KINV = KSTORE = NN = IINV = KK = 0;
            //ILESS = IGAMAX = INCINV = INCINP = IIP = JL = JJP = KL = ICONT = ISS = 0;
            IS_ = /*IPS = IT =*/ IPC = IPP = IPRINT = MC = 0;
            ///*SUM = */B = BB = FF = CHANGE = DM = FC = ACC = XC = XL = FMIN = FSEC = xxx = 0;
            //enabled = 0u;
            icov = jcov = 0;
        }

        private void moveElement<T>(T[] array, int id) {
            T tmp = array[id];
            for (int i = id; i < array.Length-1; i++)
                array[i] = array[i + 1];
            array[array.Length - 1] = tmp;
        }

        private void eliminateParameter(int id, ref int currentParameterCount) {
            moveElement<double>(x, id);
            moveElement<IParameter>(parameters, id);
            moveElement<double>(memx, id);
            moveElement<double>(errors, id);
            moveElement<double>(xx, id);
            currentParameterCount--;
        }

        //public void findMinimum(TargetFunction fcn, object target,
        //    int M, int N, double[] f, List<IParameter> parameters,
        //    out double FFNorm, int NFUN, double XSTEP, out int independentParamId, bool useEvent) {
        //f = null;
        public virtual double fit(object target, bool emptyRun) {
            int ii, i, jj, j, k, Ifree, mr;
            int MPLUSN, KST, NPLUS, KINV;
            int KSTORE, NN, IINV, KK, ILESS, IGAMAX, INCINV, INCINP, IIP, JL, JJP, KL, ICONT, ISS, IPS, IT;
            int M, N, NNN;
            double SUM, B, BB, FF, CHANGE, DM, FC, ACC, XC, XL, FMIN, FSEC, xxx;
        labelAfterElimination:
            resetFields();
            ICONT = IGAMAX = ILESS = INCINP = INCINV = IPS = ISS = JL = KL = 0;
            CHANGE = FF = FMIN = FSEC = 0;
            M = _M;
            NNN = _NNN;
            double chisq;
            bool _cancel = false;
            N = NNN;
            double eps = 1e-12;
        
            MPLUSN = M + N;
            KST = N + MPLUSN;
            NPLUS = N + 1;
            KINV = NPLUS * (MPLUSN + 1);
            KSTORE = KINV - MPLUSN - 1;

            //xx=x;
            x.CopyTo(xx, 0);
            x.CopyTo(memx, 0);
            //for (iii = NNN - N; iii >= 1; iii--)
            //    exch_a(ref xx[aa[iii] - 1], ref xx[bbb[iii] - 1]); //delphi--> for iii=NNN-N downto 1 do exch(xx[aa[iii]],xx[bbb[iii]]);
            //indpar = 0;

            _cancel = !callTargetFunction(target, f, N);
            //EVEL.NeedTesting.Utilities.SaveArray(f, @"d:\devel\ltvsneed\diffs_lt10.txt");
            //if ((target is ISpectrum)) {
            //    //System.Collections.Generic.ICollection<ISpectrum> spectra =
            //    //    (System.Collections.Generic.ICollection<ISpectrum>)target;
            //    //System.Collections.Generic.IEnumerator<ISpectrum> enumerator = spectra.GetEnumerator();
            //    //enumerator.MoveNext();
            //    //enumerator.Current.Container.ParentProject.SaveObjectState(@"d:\devel\ltvsneed\state_begin_.txt");
            //    ((ISpectrum)target).Container.ParentProject.SaveObjectState(@"d:\devel\ltvsneed\state_begin_.txt");
            //    EVEL.NeedTesting.Utilities.SaveArray(f, @"d:\devel\ltvsneed\diffs_need_begin_.txt");
            //}


            //saveArray('d:\ltvsneed\lt.txt', f);
            NN = N + N;
            k = NN;
            for (i = 1; i <= M; i++) { //delphi--> FOR i=1 TO M DO
                k = k + 1;
                w_n[k] = f[i - 1];
            }

            IINV = 2;
            k = KST;
            Ifree = 1;
            if (parameters.Length == 0) {
                goto label15; //calculate FF and finish
            }
        label2:
            x[Ifree - 1] = x[Ifree - 1] + parameters[Ifree - 1].SearchDelta; // e[Ifree - 1];
            x.CopyTo(xx, 0);
            //for (iii = NNN - N; iii >= 1; iii--)
            //    exch_a(ref xx[aa[iii] - 1], ref xx[bbb[iii] - 1]); //delphi--> for iii=NNN-N downto 1 do exch(xx[aa[iii]],xx[bbb[iii]]);
            _cancel = !callTargetFunction(target, f, N);
            //x[Ifree - 1] = (x[Ifree - 1] - e[Ifree - 1]);
            x[Ifree - 1] = memx[Ifree - 1];
            for (j = 1; j <= N; j++) {
                k++;
                w_n[k] = 0.0;
                w_n[j] = 0.0;
            }
            SUM = 0.0;
            KK = NN;
            for (j = 1; j <= M; j++) { //delphi--> FOR j=1 TO M DO
                KK++;
                f[j - 1] = f[j - 1] - w_n[KK];
                SUM = SUM + f[j - 1] * f[j - 1];
            }

            if (SUM <= 1e-30) { // 1e-200) {//delphi--> IF(SUM<=1E-200)
                //try {

                //    aa[NNN - N + 1] = Ifree;
                //} catch (Exception) {
                //    System.Diagnostics.Debug.WriteLine("Index out of range!");
                //}
                //bbb[NNN - N + 1] = N;
                //exch(ref x[Ifree - 1], ref x[N - 1]); //--> exch(x[Ifree],x[N]);
                //exch(ref e[Ifree - 1], ref e[N - 1]); //--> exch(e[Ifree],e[N]);
                //exch(Ifree - 1, N - 1);
                //N--;
                //enabled[Ifree - 1] = false;
                if (this.IndependencyFound != null)
                    this.IndependencyFound(target, parameters[Ifree - 1]);
                //if (independiences != null)
                //    independiences.Add(parameters[Ifree - 1]);
                eliminateParameter(Ifree - 1, ref _NNN);
                //enabled |= 2u << Ifree - 1;
                //if (++Ifree <= N && !_cancel)
                if (N>0 && !_cancel)
                    goto labelAfterElimination;
                else {
                    chisq = 0;
                    goto label33;
                }
                
                //goto labelE1;
                //sum=1e-30;
            }
            SUM = 1.0 / Math.Sqrt(SUM);
            j = k - N + Ifree;
            //w_n[j] = e[Ifree - 1] * SUM;
            w_n[j] = parameters[Ifree - 1].SearchDelta * SUM;
            for (j = 1; j <= M; j++) { //delphi--> FOR j=1 TO M DO
                k++;
                w_n[k] = f[j - 1] * SUM;
                KK = NN + j;
                for (ii = 1; ii <= Ifree; ii++) { //delphi--> FOR ii=1 TO Ifree DO
                    KK = KK + MPLUSN;
                    w_n[ii] = w_n[ii] + w_n[KK] * w_n[k];
                }
            }
            ILESS = Ifree - 1;
            IGAMAX = N + Ifree - 1;
            INCINV = N - ILESS;
            INCINP = INCINV + 1;
            if (ILESS <= 0) { //delphi--> IF(ILESS<=0) THEN
                w_n[KINV] = 1;
                goto label15;
            }
        label14:
            B = 1.0;
            for (j = NPLUS; j <= IGAMAX; j++)
                w_n[j] = 0.0; //delphi--> FOR j=NPLUS TO IGAMAX DO w_n[j]=0.0;
            KK = KINV;
            for (ii = 1; ii <= ILESS; ii++) { //delphi--> FOR ii=1 TO ILESS DO
                IIP = ii + N;
                w_n[IIP] = w_n[IIP] + w_n[KK] * w_n[ii];
                JL = ii + 1;
                if (JL - ILESS <= 0)  //delphi--> IF(JL-ILESS<=0) THEN
                    for (jj = JL; jj <= ILESS; jj++) { //delphi--> FOR jj=JL TO ILESS DO
                        KK++;
                        JJP = jj + N;
                        w_n[IIP] = w_n[IIP] + w_n[KK] * w_n[jj];
                        w_n[JJP] = w_n[JJP] + w_n[KK] * w_n[ii];
                    }
                B = B - w_n[ii] * w_n[IIP];
                KK = KK + INCINP;
            }
            if (Math.Abs(B) > eps)
                B = 1 / B;
            else
                B = 1E10; //delphi--> if B!=0  B=1.0/B else B=1E10;
            //B = 1 / (B + 1e-30);
            KK = KINV;
            for (ii = NPLUS; ii <= IGAMAX; ii++) { //delphi--> FOR ii=NPLUS TO IGAMAX DO
                BB = -B * w_n[ii];
                for (jj = ii; jj <= IGAMAX; jj++) { //delphi--> FOR jj=ii TO IGAMAX DO
                    w_n[KK] = w_n[KK] - BB * w_n[jj];
                    KK++;
                }
                w_n[KK] = BB;
                KK = KK + INCINV;
            }
            w_n[KK] = B;
        label15:
            if (IINV != 1) { //delphi--> IF IINV!=1 THEN
                //do {
                    Ifree = Ifree + 1;
                //    //} while (Ifree <= N && !enabled[Ifree-1]);
                //} while (Ifree <= N && ((enabled & (2u << Ifree - 1)) == 1));
                if (Ifree - N <= 0) goto label2; //delphi--> IF(Ifree-N<=0) THEN GOTO 2;
                IINV = 1;
                FF = 0.0;
                KL = NN;
                for (i = 1; i <= M; i++) { //delphi--> FOR i=1 TO M DO
                    KL = KL + 1;
                    f[i - 1] = w_n[KL];
                    FF = FF + f[i - 1] * f[i - 1];
                }
                ICONT = 1;
                ISS = 1;
                MC = N + 1;
                IPP = IPRINT * (IPRINT - 1);
                //ITC = 0;
                IPS = 1;
                IPC = 0;
            }
            IPC = IPC - IPRINT;
        //if (IPC >= 0) goto label29; //delphi--> IF(IPC>=0) THEN GOTO 29;
        label28:
            FFNORM = FF / (M - N);
            chisq = FFNORM;
            if (this.Changed != null && IPS != 2) {
                UpdateParameters(N);
                fitChangeArgs.Target = target;
                fitChangeArgs.Chisq = FFNORM;
                fitChangeArgs.Iteration = ++iteration;
                Changed(this, fitChangeArgs);

            }
            //if (!(target is IProject))
            //System.Diagnostics.Debug.WriteLine(chisq);
            IPC = IPP;
            if (IPS == 2 || emptyRun || x.Length == 0 || _cancel) goto label33; //delphi--> IF IPS=2 THEN GOTO 33;

            //label29: 
            if (ICONT == 1) goto label34; //delphi--> IF ICONT=1 THEN GOTO 34;
            if (CHANGE - 1 > 0) goto label36; //delphi--> IF(CHANGE-1.0>0) THEN GOTO 36;
        label10:
            IPS = 2;
            goto label28;
        //goto label33;
        label33:   //IFLAG=3;
            //xx = x;
            x.CopyTo(xx, 0);
            //for (iii = NNN - N; iii >= 1; iii--)  //delphi--> for iii=NNN-N downto 1 do exch(xx[aa[iii]],xx[bbb[iii]]);
            //    exch_a(ref xx[aa[iii] - 1], ref xx[bbb[iii] - 1]); //--> exch(xx[aa[iii]],xx[bbb[iii]]); 
            //exch(aa[iii] - 1, bbb[iii] - 1);

            //if (!(target is IProject)) {
            //    System.Diagnostics.Debug.WriteLine(chisq);
            //    EVEL.NeedTesting.Utilities.SaveArray(f, @"d:\devel\ltvsneed\diffs_need_1.txt");
            //}
            _cancel = !callTargetFunction(target, f, N);
            //if ((target is ISpectrum)) {
            //    //System.Diagnostics.Debug.WriteLine(chisq);
            //    EVEL.NeedTesting.Utilities.SaveArray(f, @"d:\devel\ltvsneed\diffs_need_end_.txt");

            //    //System.Collections.Generic.ICollection<ISpectrum> spectra =
            //    //    (System.Collections.Generic.ICollection<ISpectrum>)target;
            //    //System.Collections.Generic.IEnumerator<ISpectrum> enumerator = spectra.GetEnumerator();
            //    //enumerator.MoveNext();
            //    //enumerator.Current.Container.ParentProject.SaveObjectState(@"d:\devel\ltvsneed\state_end_.txt");
            //    ((ISpectrum)target).Container.ParentProject.SaveObjectState(@"d:\devel\ltvsneed\state_end_.txt");
            //}

            for (i = 1; i <= NNN; i++)
                errors[i - 1] = 0.0; //delphi--> for i=1 to 15 do errors[i]=0.0;
            // IWC1=2*N+N*(N+M);
            for (i = 1; i <= N; i++)  //delphi--> FOR i=1 TO N DO
                for (j = 1; j <= i; j++) { //delphi--> FOR j=1 TO i DO
                    //if (i == j && ((enabled & (2u << i - 1)) == 0))  //delphi--> if i=j 
                        for (mr = 1; mr <= N; mr++) { //delphi--> FOR mr=1 TO N DO
                            jcov = N + j + mr * (N + M);
                            icov = N + i + mr * (N + M);
                            errors[i - 1] = errors[i - 1] + w_n[icov] * w_n[jcov];
                        }
                }

            // for i=NNN-N+1 to NNN do errors[i]=0;
            //for (i = NNN - N; i >= 1; i--)  //delphi--> for i=NNN-N downto 1 do exch(x[aa[i]],x[bbb[i]]);
            //    exch(aa[i] - 1, bbb[i] - 1); //--> exch(x[aa[i]],x[bbb[i]]); 
            //for (i = NNN - N; i >= 1; i--)  //delphi--> for i=NNN-N downto 1 do exch(errors[aa[i]],errors[bbb[i]]);
            //    exch(ref errors[aa[i] - 1], ref errors[bbb[i] - 1]); //--> exch(errors[aa[i]],errors[bbb[i]]); 
            //xx = x;
            UpdateErrors(N);
            x.CopyTo(xx, 0);
            //indpar = enabled;
            return chisq;

        label36: ICONT = 1;
        label34: ITC = ITC + 1;
            k = N;
            KK = KST;
            for (i = 1; i <= N; i++) { //delphi--> FOR i=1 TO N DO
                //if ((enabled & (2u << i - 1)) != 0) continue; //test 12 maj 2011
                k = k + 1;
                w_n[k] = 0.0;
                KK = KK + N;
                w_n[i] = 0.0;
                for (j = 1; j <= M; j++) { //delphi--> FOR j=1 TO M DO
                    KK = KK + 1;
                    w_n[i] = w_n[i] + w_n[KK] * f[j - 1];
                }
            }
            DM = 0.0;
            k = KINV;
            for (ii = 1; ii <= N; ii++) { //delphi--> FOR ii=1 TO N DO
                //if ((enabled & (2u << ii - 1)) != 0) continue; //test 12 maj 2011
                IIP = ii + N;
                w_n[IIP] = w_n[IIP] + w_n[k] * w_n[ii];
                JL = ii + 1;
                if (JL - N <= 0) { //delphi--> IF(JL-N<=0) THEN
                    for (jj = JL; jj <= N; jj++) { //delphi--> FOR jj=JL TO N DO
                        JJP = jj + N;
                        k++;
                        w_n[IIP] = w_n[IIP] + w_n[k] * w_n[jj];
                        w_n[JJP] = w_n[JJP] + w_n[k] * w_n[ii];
                    }
                    k++;
                }
                if (DM - Math.Abs(w_n[ii] * w_n[IIP]) < 0) { //delphi--> IF(DM-Math.Abs(w_n[ii]*w_n[IIP])<0) THEN
                    DM = Math.Abs(w_n[ii] * w_n[IIP]);
                    KL = ii;
                }
            }
            ii = N + MPLUSN * KL;
            CHANGE = 0;
            for (i = 1; i <= N; i++) { //delphi--> FOR i=1 TO N DO
                //if ((enabled & (2u << i - 1)) != 0) //test 12 maj 2011
                //    continue;
                JL = N + i;
                w_n[i] = 0.0;
                for (j = NPLUS; j <= NN; j++) { //delphi--> FOR j=NPLUS TO NN DO
                    JL = JL + MPLUSN;
                    w_n[i] = w_n[i] + w_n[j] * w_n[JL];   // PIERWSZE PODSTAWIENIE INFINITY!!! PO TYM MIEJSCU JESLI KTORS Z PARAMETROW JEST JUZ BLISKO GRANICY ZAKRESU ZMIENNEJ, NASTEPUJE ARYTMETYKA Z NIESKONCZONOSCIA -> NaN!!!
                }
                ii = ii + 1;
                w_n[ii] = w_n[JL];
                w_n[JL] = x[i - 1];
                if (Math.Abs(parameters[i - 1].SearchDelta * CHANGE) - Math.Abs(w_n[i]) <= 0)    //delphi--> IF(Math.Abs(e[i]*CHANGE)-Math.Abs(w_n[i])<=0) THEN  
                    CHANGE = Math.Abs(w_n[i] / parameters[i - 1].SearchDelta);
            }
            for (i = 1; i <= M; i++) { //delphi--> FOR i=1 TO M DO
                ii = ii + 1;
                JL = JL + 1;
                w_n[ii] = w_n[JL];
                w_n[JL] = f[i - 1];
            }
            FC = FF;
            if (Math.Abs(CHANGE) < eps)  //delphi--> if (CHANGE = 0) 
                CHANGE = 1;
            ACC = 0.1 / CHANGE;
            IT = 3;
            XC = 0.0;
            XL = 0.0;
            IS_ = 3;
            if (CHANGE - 1 <= 0) ICONT = 2; //delphi--> IF(CHANGE-1.0<=0) THEN ICONT=2;
        label51:
            _VD01A(ref IT, ref XC, ref FC, 5, ref ACC, 0.1, ref XSTEP);
            if (IT >= 2 && IT <= 4)
                goto label53; //delphi--> IF (IT>=2) AND (IT<=4) THEN GOTO 53;
            MC++;
            if (MC - MAXFUN <= 0)  //delphi--> IF (MC-MAXFUN<=0) THEN
                goto label54;
            ISS = 2;
            goto label53;
        label54:
            XL = XC - XL;
            for (j = 1; j <= N; j++) {//delphi--> FOR j=1 TO N DO
                //if ((enabled & (2u << j - 1)) == 0) {
                x[j - 1] = x[j - 1] + XL * w_n[j];
                //if (double.IsInfinity(x[j - 1]) || double.IsNaN(x[j - 1])) {
                //    x[j - 1] = 1e100;
                //    w_n[j] = 1e100;
                //}
                //}
            }
            XL = XC;
            //xx = x;
            x.CopyTo(xx, 0);
            //for (iii = NNN - N; iii >= 1; iii--)  //delphi--> for iii=NNN-N downto 1 do exch(xx[aa[iii]],xx[bbb[iii]]);
            //    exch_a(ref xx[aa[iii] - 1], ref xx[bbb[iii] - 1]); //--> exch(xx[aa[iii]],xx[bbb[iii]]); 
            //exch(aa[iii] - 1, bbb[iii] - 1);
            //FCN1(M,NNN,f,xx,IFLAG,NW,KEND,KEND2N);
            _cancel = !callTargetFunction(target, f, N);
            FC = 0.0;
            for (j = 1; j <= M; j++) {
                xxx = Math.Abs(f[j - 1]);
                if (xxx > 1e16)   //delphi--> if xxx>1e16  
                    xxx = 1e16; //delphi--> FOR j=1 TO M DO begin  xxx=Math.Abs(f[j]); if xxx>1e16  xxx=1e16;
                FC = FC + xxx * xxx;
            }

            if (IS_ == 1 || IS_ == 2)
                goto label59; //delphi--> IF (IS_=1) OR (IS_=2) THEN GOTO 59;
            k = N;
            xxx = FC - FF;
            if (xxx > 0)
                goto label62; //delphi--> IF(xxx>0) THEN GOTO 62;
            if (Math.Abs(xxx) <= eps)
                goto label51; //delphi--> IF(xxx=0) THEN GOTO 51;
            IS_ = 2;
            FMIN = FC;
            FSEC = FF;
            goto label63;
        label62:
            IS_ = 1;
            FMIN = FF;
            FSEC = FC;
            goto label63;
        label59:
            if (FC - FSEC >= 0)
                goto label51; //delphi--> IF(FC-FSEC>=0) THEN GOTO 51;
            k = KSTORE;
            if (IS_ != 2)
                k = N; //delphi--> IF IS_!=2 THEN k=N;
            xxx = FC - FMIN;
            if (Math.Abs(xxx) < eps)
                goto label51; //delphi--> if(Math.Abs(xxx))<doklad  goto 51;
            if (xxx < 0)
                goto label65; //delphi--> IF(xxx<0) THEN GOTO 65;
            FSEC = FC;
            goto label63;
        label65: IS_ = 3 - IS_;
            FSEC = FMIN;
            FMIN = FC;
        label63:
            for (j = 1; j <= N; j++) { //delphi--> FOR j=1 TO N DO
                //if ((enabled & (2u << j - 2)) == 1) continue;
                k++;
                w_n[k] = x[j - 1];
            }
            for (j = 1; j <= M; j++) { //delphi--> FOR j=1 TO M DO
                k++;
                w_n[k] = f[j - 1];
            }
            goto label51;
        label53: k = KSTORE;
            KK = N;
            if (IS_ != 1 && IS_ != 3) { //delphi--> IF (IS_!=1) AND (IS_!=3) THEN
                k = N;
                KK = KSTORE;
            }
            SUM = 0.0;
            DM = 0.0;
            jj = KSTORE;
            for (j = 1; j <= N; j++) { //delphi--> FOR j=1 TO N DO
                //if (!enabled[j - 1]) continue;
                //if ((enabled & (2u << j - 2)) == 1) continue;
                k = k + 1;
                KK = KK + 1;
                jj = jj + 1;
                x[j - 1] = w_n[k];
                w_n[jj] = w_n[k] - w_n[KK];
            }
            for (j = 1; j <= M; j++) { //delphi--> FOR j=1 TO M DO
                k = k + 1;
                KK = KK + 1;
                jj = jj + 1;
                f[j - 1] = w_n[k];
                w_n[jj] = w_n[k] - w_n[KK];
                SUM = SUM + w_n[jj] * w_n[jj];
                DM = DM + f[j - 1] * w_n[jj];
            }
            if (ISS == 2) goto label10; //delphi--> IF ISS=2 THEN GOTO 10;
            j = KINV;
            KK = NPLUS - KL;
            for (i = 1; i <= KL; i++) { //delphi--> FOR i=1 TO KL DO
                k = j + KL - i;
                j = k + KK;
                w_n[i] = w_n[k];
                w_n[k] = w_n[j - 1];
            }
            if (KL - N < 0) { //delphi--> IF(KL-N<0) THEN
                KL = KL + 1;
                jj = k;
                for (i = KL; i <= N; i++) { //delphi--> FOR i=KL TO N DO
                    //if (!enabled[i - 1]) continue;
                    //if ((enabled & (2u << j - 2)) == 1) continue;
                    k = k + 1;
                    j = j + NPLUS - i;
                    w_n[i] = w_n[k];
                    w_n[k] = w_n[j - 1];
                }
                w_n[jj] = w_n[k];
                B = 1 / w_n[KL - 1];
                w_n[KL - 1] = w_n[N];
            } else
                B = 1 / w_n[N];
            k = KINV;
            for (i = 1; i <= ILESS; i++) { //delphi--> FOR i=1 TO ILESS DO
                BB = B * w_n[i];
                for (j = i; j <= ILESS; j++) { //delphi--> FOR j=i TO ILESS DO
                    w_n[k] = w_n[k] - BB * w_n[j];
                    k++;
                }
                k++;
            }
            if (FMIN - FF >= 0)
                CHANGE = 0; //delphi--> IF(FMIN-FF>=0) THEN CHANGE=0.0
            else {
                FF = FMIN;
                CHANGE = Math.Abs(XC) * CHANGE;
            }
            XL = -DM / (FMIN + 1e-30);
            SUM = 1 / Math.Sqrt(Math.Abs(SUM + DM * XL + 1e-30));
            k = KSTORE;
            for (i = 1; i <= N; i++) { //delphi--> FOR i=1 TO N DO
                k = k + 1;
                w_n[k] = SUM * w_n[k];
                w_n[i] = 0;
            }
            for (i = 1; i <= M; i++) { //delphi--> FOR i=1 TO M DO
                k++;
                w_n[k] = SUM * (w_n[k] + XL * f[i - 1]);
                KK = NN + i;
                for (j = 1; j <= N; j++) { //delphi--> FOR j=1 TO N DO
                    KK = KK + MPLUSN;
                    w_n[j] = w_n[j] + w_n[KK] * w_n[k];
                }
            }
            goto label14;
        }//{fit}






        //private void exch_a(ref double x1, ref double x2) {
        //    return;
        //    double tmp = x1;
        //    x1 = x2;
        //    x2 = tmp;
        //}

        //private void exch(int i1, int i2) {
        //    return;
        //    IParameter tmpa = parameters[i1];
        //    parameters[i1] = parameters[i2];
        //    parameters[i2] = tmpa;
        //    exch_a(ref x[i1], ref x[i2]);
        //    exch_a(ref e[i1], ref e[i2]);
        //    exch_a(ref errors[i1], ref errors[i2]);
        //    exch_a(ref xx[i1], ref xx[i2]);
        //}


        private void _VD01A(ref int itest_v, ref double x_v,
            ref double f_v, int maxfun_v, ref double absacc_v,
            double relacc_v, ref double xstep_v) {


            //int _is_v, _iinc_v, _mc_v;

            //double _xinc_v, _db_v, _fb_v, _fc_v, _dc_v, _da_v, _fa_v, _d_v;

            //_xinc_v = _db_v = _fb_v = _fc_v = _dc_v = _da_v = _fa_v = _d_v = 0;
            //_is_v = 1;
            //_iinc_v = _mc_v = 0;

            double XXX;
            if (itest_v == 1) goto Label1;
            _is_v = 6 - itest_v;
            itest_v = 1;
            _iinc_v = 1;
            _xinc_v = xstep_v + xstep_v;
            _mc_v = _is_v - 3;
            if (_mc_v <= 0)
                goto Label4;
            else
                goto Label15;

            //Label3----------------------------------------------------------
        Label3:
            _mc_v++;
            if (maxfun_v - _mc_v >= 0)
                goto Label15;
            itest_v = 4;

            //Label43----------------------------------------------------------
        Label43:
            x_v = _db_v;
            f_v = _db_v;
            if (_fb_v - _fc_v <= 0)
                goto Label15;
            x_v = _dc_v;
            f_v = _fc_v;

            //Label15----------------------------------------------------------
        Label15:
            return;

            //Label1----------------------------------------------------------
        Label1:
            switch (_is_v) {
                case 1: goto Label5;
                case 2: goto Label6;
                case 3: goto Label7;
            }
            _is_v = 3;

            //Label4----------------------------------------------------------
        Label4:
            _dc_v = x_v;
            _fc_v = f_v;
            x_v = x_v + xstep_v;
            goto Label3;

            //Label7----------------------------------------------------------
        Label7:
            XXX = _fc_v - f_v;
            if (XXX < 0)
                goto Label9;
            if (XXX > 0)
                goto Label11;

            //Label10----------------------------------------------------------
        Label10:
            x_v = x_v + _xinc_v;
            _xinc_v = _xinc_v + _xinc_v;
            goto Label3;

            //Label9----------------------------------------------------------
        Label9:
            _db_v = x_v;
            _fb_v = f_v;
            _xinc_v = - _xinc_v;
            goto Label13;

            //Label11----------------------------------------------------------
        Label11:
            _db_v = _dc_v;
            _fb_v = _fc_v;
            _dc_v = x_v;
            _fc_v = f_v;

            //Label13----------------------------------------------------------
        Label13: x_v = _dc_v + _dc_v - _db_v;
            _is_v = 2;
            goto Label3;

            //Label6----------------------------------------------------------
        Label6: _da_v = _db_v;
            _db_v = _dc_v;
            _fa_v = _fb_v;
            _fb_v = _fc_v;
        Label32: _dc_v = x_v;
            _fc_v = f_v;
            goto Label14;

            //Label5----------------------------------------------------------
        Label5: if (_fb_v - _fc_v < 0) goto Label16;
            if (f_v - _fb_v >= 0) goto Label32;
            _fa_v = _fb_v;
            _da_v = _db_v;
        Label19: _fb_v = f_v;
            _db_v = x_v;
            goto Label14;

            //Label16----------------------------------------------------------
        Label16: if (_fa_v - _fc_v <= 0) goto Label21;
            _xinc_v = _fa_v;
            _fa_v = _fc_v;
            _fc_v = _xinc_v;
            _xinc_v = _da_v;
            _da_v = _dc_v;
            _dc_v = _xinc_v;

            //Label21----------------------------------------------------------
        Label21: _xinc_v = _dc_v;
            if ((_d_v - _db_v) * (_d_v - _dc_v) < 0) goto Label32;
            if (f_v - _fa_v >= 0) goto Label24;
            _fc_v = _fb_v;
            _dc_v = _db_v;
            goto Label19;

            //Label24----------------------------------------------------------
        Label24: _fa_v = f_v;
            _da_v = x_v;
        Label14: if (_fb_v - _fc_v > 0) goto Label29;
            _iinc_v = 2;
            _xinc_v = _dc_v;
            if (Math.Abs(_fb_v - _fc_v) <= 1e-30) goto Label45;
            //Label29----------------------------------------------------------
        Label29: _d_v = (_fa_v - _fb_v) / (_da_v - _db_v) - (_fa_v - _fc_v) / (_da_v - _dc_v);
            if (_d_v * (_db_v - _dc_v) <= 0) goto Label33;
            _d_v = 0.5 * (_db_v + _dc_v - (_fb_v - _fc_v) / _d_v);

            if (Math.Abs(_d_v - x_v) - Math.Abs(absacc_v) <= 0) goto Label34;
            if (Math.Abs(_d_v - x_v) - Math.Abs(_d_v * relacc_v) > 0) goto Label36;

            //Label34----------------------------------------------------------
        Label34: itest_v = 2;
            goto Label43;

            //Label36----------------------------------------------------------
        Label36: _is_v = 1;
            x_v = _d_v;
            XXX = (_da_v - _dc_v) * (_dc_v - _d_v);
            if (XXX < 0) goto Label3;
            if (Math.Abs(XXX) < 1e-30) goto Label26;
            _is_v = 2;
            if (_iinc_v == 2) goto Label40;
            XXX = Math.Abs(_xinc_v) - Math.Abs(_dc_v - _d_v);
            if (XXX < 0)
                goto Label41;
            else
                goto Label3;

            //Label33----------------------------------------------------------
        Label33: _is_v = 2;
            if (_iinc_v == 2) goto Label42;

            //Label41----------------------------------------------------------
        Label41: x_v = _dc_v;
            goto Label10;

            //Label40----------------------------------------------------------
        Label40: if (Math.Abs(_xinc_v - x_v) - Math.Abs(x_v - _dc_v) > 0) goto Label3;

            //Label42----------------------------------------------------------
        Label42: x_v = 0.5 * (_xinc_v + _dc_v);
            if ((_xinc_v - x_v) * (x_v - _dc_v) < 0)
                goto Label26;
            else
                goto Label3;

            //Label45----------------------------------------------------------
        Label45: x_v = 0.5 * (_db_v + _dc_v);
            if ((_db_v - x_v) * (x_v - _dc_v) > 0) goto Label3;

            //Label26----------------------------------------------------------
        Label26: itest_v = 3;
            goto Label43;
        }



    }
}
