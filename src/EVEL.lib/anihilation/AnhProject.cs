using System;
using System.Collections.Generic;
using System.Text;
using Evel.interfaces;
using Evel.engine.algorythms;
using System.Xml;
using System.Net;
using System.IO;
using System.ComponentModel;
using System.Reflection;
using System.Collections;
using Evel.engine.parametersImport;

namespace Evel.engine.anh {

    public class AnhProject : ProjectBase, IProject {

        private enum FitterUsed { fminsq, fmrq };

        private const FitterUsed FITTER_KIND = FitterUsed.fminsq;

        protected double _fit = double.PositiveInfinity;
        protected bool firstLocalSearchCall;
        //public bool includeInts;
        //protected Minsq localMinsq = null; //TODO : minim-remove
        //private int iteration;
        //private int functionCallCount;
        protected string[] _currentEvelListeners;
        protected double[] localDiffs;
        //protected double[] localSigma;
        protected IFitter localFitter = null;
        private ParameterValuesRecord backupRecord = null;

        public override event AsyncCompletedEventHandler SearchCompleted;
        public override event ProgressChangedEventHandler SearchProgressChanged;
        public override event ProgressChangedEventHandler FirstSpectraSearchProgressChanged;
        public override event IndependencyFoundEventHandler IndependencyFound;
        public override event AsyncFirstSpectraSearchCompletedEventHandler FirstSpectraSearchCompleted;
        public override event IndefiniteMatrixEventHandler IndefiniteMatrixGot;


        #region IProject Members

        public override string ExperimentalMethodName {
            get { return "Anihilation"; }
        }

        public override Type GetSpectraContainerType() {
            return typeof(AnhSpectraContainer);
        }

        public override double Fit {
            get {
                setGlobalChi();
                return this._fit;
            }
            set { this._fit = value; }
        }

        public override string Name {
            get { return "Anihilation project"; }
        }

        public override string Description {
            get { return "Anihilation project. Performs Search for parameters of anihilation spectra"; }
        }

        public override ISpectraContainer CreateContainer(IModel model) {
            return new AnhSpectraContainer(this, model);
        }

        public override ISpectraContainer CreateContainer(string name, IModel model, ICollection<string> spectraPaths, ICollection<GroupDefinition> groupsDefinition) {
            return new AnhSpectraContainer(this, name, model, spectraPaths, groupsDefinition);
        }

        #endregion

        //protected bool IncludeInts {
        //    get {
        //        return this._flags["includeInts"];
        //    }
        //    set {
        //        this._flags["includeInts"] = value;
        //    }
        //}

        protected void init() {
            //int L, i, l; //data buffer length
            //L = 0;
            //l = 0;
            //foreach (ISpectraContainer container in Containers)
            //    foreach (SpectrumBase spectrum in container.Spectra) {
            //        spectrum.dataBufferStart = L;
            //        L += spectrum.ExperimentalSpectrum.Length;
            //        if (l < spectrum.ExperimentalSpectrum.Length)
            //            l = spectrum.ExperimentalSpectrum.Length;
            //        spectrum.dataBufferStop = L - 1;
            //    }
            //_mrqdata = new double[L, 2];
            //_mrqy = new double[L];
            //_mrqtmpfx0 = new double[l];
            //foreach (ISpectraContainer container in Containers)
            //    foreach (SpectrumBase spectrum in container.Spectra) {
            //        for (i = spectrum.BufferStartPos; i <= spectrum.BufferEndPos; i++) {
            //            _mrqdata[i, 0] = spectrum.ExperimentalSpectrum[i - spectrum.BufferStartPos];
            //            _mrqdata[i, 1] = Math.Sqrt(_mrqdata[i, 0]);
            //        }
            //    }
            ////mrq fitters
            //if (_mrqfitG == null)
            //    _mrqfitG = new Fitmrq(function);
            //if (_mrqfitL == null)
            //    _mrqfitL = new Fitmrq(prefunction);
        }

        public AnhProject()
            : base() {
            init();

        }

        public AnhProject(string fileName)
            : base(fileName) {
            init();
        }

        public AnhProject(string name, SpectraContainerDescription[] descriptions)
            : base(name, descriptions) {
            init();
        }

        //private void SetParameters(List<IParameter> parameters, ICollection<ISpectrum> spectra, out int M, out int N,
        //    ParameterStatus status, bool[] includeFlags, CheckOptions checkOptions) {
        //    M = 0;
        //    N = 0;
        //    parameters.Clear();
        //    parameters.Add(null);

        //    foreach (ISpectrum spectrum in spectra) {
        //        //spectrum.Container.Model.checkParameters(spectrum.Parameters, CheckOptions.RefreshDelta);
        //        List<IParameter> sParameters = spectrum.Container.Model.getParameters(status, spectrum.Parameters, includeFlags);
        //        foreach (IParameter p in parameters)
        //            sParameters.Remove(p);
        //        parameters.AddRange(sParameters);
        //        M += (int)spectrum.Parameters[4].Components[0][2].Value;
        //        M++;
        //        M -= (int)spectrum.Parameters[4].Components[0][1].Value;
        //    }
        //    N = parameters.Count - 1;
        //}

        private bool[] GetFlags(SearchFlags flags) {
            return new bool[] { 
                (flags & SearchFlags.IncludeInts) == SearchFlags.IncludeInts,
                (flags & SearchFlags.IncludeSourceContribution) == SearchFlags.IncludeSourceContribution,
                (flags & SearchFlags.PromptOnly) == SearchFlags.PromptOnly };
        }

        private void SetFlags(ref bool[] bflags, SearchFlags flags) {
            bflags[0] = (flags & SearchFlags.IncludeInts) == SearchFlags.IncludeInts;
            bflags[1] = (flags & SearchFlags.IncludeSourceContribution) == SearchFlags.IncludeSourceContribution;
            bflags[2] = (flags & SearchFlags.PromptOnly) == SearchFlags.PromptOnly;
        }

        private void SetParameters(ICollection<ISpectrum> spectra,
            out Parameter[] parameters, out int M, ParameterStatus status, //ref double[] sigma,
            SearchFlags includeFlags, CheckOptions checkOptions) {
            M = 0;
            HashSet<IParameter> pset = new HashSet<IParameter>();
            int start, stop;
            bool[] bflags = GetFlags(includeFlags);
            foreach (ISpectrum spectrum in spectra) {
                foreach (IParameter p in spectrum.Container.Model.getParameters(status, spectrum, bflags, checkOptions))
                    pset.Add(p);
                stop = (int)spectrum.Parameters[4].Components[0][2].Value;
                start = (int)spectrum.Parameters[4].Components[0][1].Value;
                M += stop - start;// -spectrum.ThresholdsCompression;
                
            }

            parameters = new Parameter[pset.Count];
            int pId = 0;
            foreach (IParameter p in pset) {
                p.SaveBackup();
                parameters[pId++] = (Parameter)p;
            }
        }

        public void SaveBackupParameters(List<ISpectrum> spectra) {
            //if (this.backupRecord == null)
            //    backupRecord = new ParameterValuesRecord(spectra);
            //else
            //    backupRecord.SetData(spectra);
            SaveBackupParameters(spectra, ref backupRecord);
        }

        public void SaveBackupParameters(List<ISpectrum> spectra, ref ParameterValuesRecord values) {
            if (values == null)
                values = new ParameterValuesRecord(spectra);
            else
                values.SetData(spectra);
        }

        public override void RestoreSpectrumStartingValues(ISpectrum spectrum, ParameterStatus status) {
            backupRecord.FillSpectrum(spectrum, status);
        }

        public override void RestoreParameter(ISpectrum spectrum, ParameterLocation location) {
            backupRecord.RestoreParameter(spectrum, location);
        }

        /// <summary>
        /// Search values for first spectrum in each container version with marquardtfit minimizer
        /// </summary>
        public override void FirstSpectraSearch(List<ISpectrum> spectra) {
            //presearch(spectra);
            //return;
            SaveBackupParameters(spectra);
            Exception error = null;
            bool searchAgain = false;
            int specid;
            //ulong indpar;
            int i = 0;
            int j;
            //double[] backupValues;
            try {
                do {
                    try {
                        this._searchMode = SearchMode.Preliminary;
                        this._isBusy = true;
                        Flags &= ~SearchFlags.IncludeInts;
                        if (spectra.Count == 0)
                            throw new Exception("No spectra to fit");
                        firstLocalSearchCall = false;
                        Parameter[] parameters;
                        double oldchi;
                        double[] f = null;
                        int M = 0;
                        for (specid = 0; specid < spectra.Count; specid++) {
                            AnhSpectraContainer container = (AnhSpectraContainer)spectra[specid].Container;
                            container.ResetArrays();
                            //container.preparePromptInts(spectra[specid]);
                            spectra[specid].Fit = double.PositiveInfinity;
                            container.setLongestRange(); //finds longest spectrum i remembers its right range. There is one array for each spectrum so it must have the longest dimension
                            spectra[specid].prepareToSearch(SearchLevel.Preliminary, PrepareOptions.GlobalArea | PrepareOptions.PromptIntensities);
                        }

                        SetParameters(
                            spectra,
                            out parameters,
                            out M, ParameterStatus.Free,
                            //ref sigma, 
                            SearchFlags.Standard,
                            CheckOptions.RefreshDelta);
                        f = new double[M];
                        //backupValues = new double[parameters.Length];
                        for (i = 0; i < parameters.Length; i++)
                            //backupValues[i] = parameters[i].Value;
                            parameters[i].SaveBackup();
                        

                        IFitter fitter = new Minsq(parameters.Length * 8, 0.1);
                        if (this.IndependencyFound != null)
                            fitter.IndependencyFound += this.IndependencyFound;
                        FitProgressChangedEventArgs args = new FitProgressChangedEventArgs(0, 0, null, fitter);
                        fitter.SetParameters(parameters, f, M, this.firstSpectraTargetFunction);
                        args.Chisq = 1e+50;
                        i = 0;
                        do {
                            oldchi = args.Chisq;
                            try {
                                for (j = 0; j < parameters.Length; j++)
                                    parameters[j].SearchDelta += Math.Abs(parameters[j].SearchValue) * 0.0005;
                                args.Chisq = fitter.fit(spectra, false);
                                args.Iteration = fitter.Iteration;
                                firstSpectra_fit_Changed(fitter, args);
                            } catch (IndefiniteMatrixException mxexception) {
                                if (IndefiniteMatrixGot != null)
                                    IndefiniteMatrixGot(this, mxexception.Spectrum, ParameterStatus.Free);
                            }
                        } while (Math.Abs(oldchi - args.Chisq) > 1e-2 && fitter.Iteration < fitter.MaxIterationCount && i++ < 10);

                        //search with all parameters (intensities not included)
                        //includeFlags[2] = false;
                        //SetParameters(spectra, out parameters, out M, ParameterStatus.Free, ref sigma, includeFlags, CheckOptions.RefreshDelta);
                        //fitter.Parameters = parameters;
                        //chi = fitter.fit(spectra);
                        //-------------------------CALCULATIONS WITH INTENSITIES--------------------------------
                        //do calculations one more time with intensities included
                        this._searchMode = SearchMode.PreliminaryInts;
                        for (specid = 0; specid < spectra.Count; specid++)
                            spectra[specid].prepareToSearch(SearchLevel.Preliminary, PrepareOptions.ComponentIntensities);
                        
                        Flags |= SearchFlags.IncludeInts;
                        SetParameters(
                            spectra,
                            out parameters,
                            out M,
                            ParameterStatus.Free, 
                            SearchFlags.IncludeInts | SearchFlags.IncludeSourceContribution,
                            CheckOptions.RefreshDelta);
                        fitter.SetParameters(parameters, f, M, this.firstSpectraTargetFunction);
                        //SaveObjectState(@"d:\devel\ltvsneed\state_begin_.txt");
                        for (i = 0; i < parameters.Length; i++)
                            parameters[i].SaveBackup();
                            //backupValues[i] = parameters[i].Value;
                        i = 0;
                        fitter.MaxIterationCount = parameters.Length * 20;
                        fitter.Iteration = 0;
                        do {
                            try {
                                oldchi = args.Chisq;
                                for (j = 0; j < parameters.Length; j++) parameters[j].SearchDelta += Math.Abs(parameters[j].SearchValue) * 0.0005;
                                args.Chisq = fitter.fit(spectra, false);
                                args.Iteration = fitter.Iteration;
                                firstSpectra_fit_Changed(fitter, args);
                            } catch (IndefiniteMatrixException mxexception) {
                                if (IndefiniteMatrixGot != null)
                                    IndefiniteMatrixGot(this, mxexception.Spectrum, ParameterStatus.Free);
                            }
                        } while (Math.Abs(oldchi - args.Chisq) > 1e-2 && fitter.Iteration < fitter.MaxIterationCount && i++ < 10);

                        for (specid = 0; specid < spectra.Count; specid++)
                            spectra[specid].normalizeAfterSearch(SearchLevel.Preliminary, PrepareOptions.All, false);
                    } catch (Exception ex) {
                        error = ex;
                    }
                    this._searchMode = SearchMode.Inactive;
                    _isBusy = false;
                    if (FirstSpectraSearchCompleted != null) {
                        AsyncFirstSpectraSearchCompletedEventArgs args = new AsyncFirstSpectraSearchCompletedEventArgs(error, Canceled, null);
                        args.Spectra = spectra;
                        FirstSpectraSearchCompleted(null, args);
                        searchAgain = args.SearchAgain;
                    }
                } while (searchAgain);
            } finally {
                _isBusy = false;
                this._searchMode = SearchMode.Inactive;
            }
        }

        private void setGlobalChi() {
            this._fit = 0;
            int spectraCount = 0;
            foreach (ISpectraContainer container in Containers)
                if (container.Enabled)
                    foreach (ISpectrum spectrum in container.Spectra) {
                        this._fit += spectrum.Fit;
                        spectraCount++;
                    }
            this._fit /= spectraCount;
        }

        private void setGlobalChi(ICollection<ISpectrum> spectra) {
            this._fit = 0;
            foreach (ISpectrum spectrum in spectra)
                this._fit += spectrum.Fit;
            this._fit /= spectra.Count;
        }

        /// <summary>
        /// Search values for first spectrum in each container version with marquardtfit minimizer
        /// </summary>
        public override void SeriesSearch(ICollection<string> evelListeners, List<ISpectrum> spectra) {
            //search(containers);
            //return;
            SaveBackupParameters(spectra);
            int i;
            this._searchMode = SearchMode.Main;
            Exception error = null;
            try {
                GetArrayHandler __localSearch;
                //bool[] includeFlags = GetFlags(SearchFlags.IncludeSourceContribution); // new bool[] { IncludeInts, true, false };
                if (this.isListenersValid(evelListeners)) {
                    __localSearch = netLocalSearch;
                    this._currentEvelListeners = new string[evelListeners.Count];
                    evelListeners.CopyTo(this._currentEvelListeners, 0);
                } else {
                    __localSearch = localSearch;
                }
                firstLocalSearchCall = true;
                Flags &= ~SearchFlags.IncludeInts;
                //IncludeInts = false;

                ParameterStatus globalFreeStatus = ParameterStatus.Common | ParameterStatus.Free;
                Parameter[] parameters;
                //double[] sigma = null;
                HashSet<IParameter> parameterSet = new HashSet<IParameter>();
                List<AnhSpectraContainer> containers = new List<AnhSpectraContainer>();

                for (i = 0; i < spectra.Count; i++) {
                    if (!containers.Contains((AnhSpectraContainer)spectra[i].Container))
                        containers.Add((AnhSpectraContainer)spectra[i].Container);
                    ((AnhSpectrum)spectra[i]).prepareToSearch(SearchLevel.Global, PrepareOptions.All);
                    spectra[i].Fit = double.PositiveInfinity;
                }

                for (i = 0; i < containers.Count; i++) {
                    containers[i].ResetArrays();
                    containers[i].setLongestRange();
                }

                int M = 0;
                SetParameters(
                    spectra,
                    out parameters,
                    out M,
                    globalFreeStatus,
                    SearchFlags.IncludeSourceContribution | SearchFlags.IncludeInts,
                    CheckOptions.RefreshDelta | CheckOptions.NoReferencedDelta);

                IFitter fitter = new Minsq(1000, 0.1);
                if (this.IndependencyFound != null)
                    fitter.IndependencyFound += this.IndependencyFound;
                fitter.Changed += fit_Changed; // changeEventHandler;

                double[] f = new double[M];
                fitter.SetParameters(parameters, f, M, __localSearch);
                this._fit = fitter.fit(spectra, false);
                //setGlobalChi(spectra);
            } catch (Exception ex) {
                error = ex;
            } finally {
                //normalization
                for (i = 0; i < spectra.Count; i++) {
                    spectra[i].normalizeAfterSearch(SearchLevel.Global, PrepareOptions.All, false);
                    spectra[i].normalizeAfterSearch(SearchLevel.Local, PrepareOptions.PromptIntensities, false);
                }
                _isBusy = false;
                this._searchMode = SearchMode.Inactive;
            }
            if (SearchCompleted != null) {
                SearchCompleted(this, new AsyncCompletedEventArgs(error, Canceled, null));
            }
        }

        void minsq_MinsqProgressChanged(object sender, FitProgressChangedEventArgs e) {
            if (this.SearchProgressChanged != null) {
                this.SearchProgressChanged(sender, e);
            }
        }

        void fit_Changed(object sender, ProgressChangedEventArgs args) {
            if (this.SearchProgressChanged != null) {
                this.SearchProgressChanged(sender, args);
            }
        }

        void firstSpectra_fit_Changed(object sender, ProgressChangedEventArgs args) {
            if (this.FirstSpectraSearchProgressChanged != null)
                this.FirstSpectraSearchProgressChanged(sender, args);
        }


        protected bool firstSpectraTargetFunction(object target, double[] diffs) {
            List<ISpectrum> spectra = (List<ISpectrum>)target;
            int diffsPosition = 0;
            int chiChannelCount;
            int start, stop, M;
            //foreach (ISpectrum spectrum in spectra) {
            for (int s = 0; s < spectra.Count; s++) {
                chiChannelCount = 0;
                start = (int)spectra[s].Parameters[4].Components[0][1].Value;
                stop = (int)spectra[s].Parameters[4].Components[0][2].Value;
                M = stop - start;// -spectra[s].ThresholdsCompression;
                if (localDiffs == null)
                    localDiffs = new double[M];
                else {
                    if (localDiffs.Length < M)
                        localDiffs = new double[M];
                }
                spectra[s].Container.getEvaluationArray(spectra[s], localDiffs);
                spectra[s].Fit = 0;
                for (int i = 0; i < M; i++) {
                    diffs[diffsPosition++] = localDiffs[i];
                //    if (i + start < spectra[s].EffectEndChannel) {
                        spectra[s].Fit += localDiffs[i] * localDiffs[i];
                        chiChannelCount++;
                //    }
                }
                spectra[s].Fit /= chiChannelCount;
                if (Canceled) break;
            }
            return !Canceled;
        }

        protected bool localSearch(object target, double[] diffs) {
            //if (Canceled) return;
            if (localFitter == null) {
                localFitter = new Minsq(0, 0.1);
                if (this.IndependencyFound != null)
                    localFitter.IndependencyFound += this.IndependencyFound;
            }
            int diffsPosition = 0;
            int i, j, s; //, sc, s;
            double chi, deltachi = 0;
            //ulong indpar;
            //int chiChannelCount;
            ParameterStatus localFreeStatus = ParameterStatus.Local | ParameterStatus.Free;
            AnhSpectrum previousSpectrum = null;
            ISpectrum[] spectraCollection = new ISpectrum[1];
            Parameter[] parameters;
            List<ISpectrum> spectra = (List<ISpectrum>)target;
            //for (sc = 0; sc < this._containers.Count; sc++) {
                //if (!this._containers[sc].Enabled) continue;
                previousSpectrum = null;
                //for (s = 0; s < this._containers[sc].Spectra.Count; s++) {
            for (s=0; s<spectra.Count; s++) {
                    if (firstLocalSearchCall) {
                        spectra[s].prepareToSearch(SearchLevel.Local, PrepareOptions.PromptIntensities);
                        if (previousSpectrum != null)
                            spectra[s].copy(previousSpectrum.Parameters, localFreeStatus, CalculatedValues);
                    }
                    spectraCollection[0] = spectra[s];// this._containers[sc].Spectra[s];
                    Flags &= ~SearchFlags.IncludeInts;
                    int M;
                    SetParameters(
                        spectraCollection,
                        out parameters,
                        out M,
                        localFreeStatus,
                        SearchFlags.Standard,
                        CheckOptions.RefreshDelta | CheckOptions.SetDefaultValues);

                    if (localDiffs == null)
                        localDiffs = new double[M];
                    else {
                        if (localDiffs.Length < M)
                            localDiffs = new double[M];
                    }

                    localFitter.MaxIterationCount = parameters.Length * 16;
                    localFitter.SetParameters(parameters, localDiffs, M, spectra[s].Container.getEvaluationArray);// this._containers[sc].getEvaluationArray);
                    localFitter.StartChannel = (int)spectra[s].Parameters[4].Components[0][1].Value;
                    localFitter.Iteration = 0;
                    chi = spectra[s].Fit;
                    i = 0;
                    try {
                        do {

                            for (j = 0; j < parameters.Length; j++)
                                parameters[j].Delta += Math.Abs(parameters[j].Value) * 0.0005;
                            deltachi = chi;
                            chi = localFitter.fit(spectra[s], false);

                        } while (i++ < 10 && localFitter.Iteration < localFitter.MaxIterationCount && (Math.Abs(deltachi - chi) > 1e-3));
                    } catch (IndefiniteMatrixException mxexception) {
                        if (IndefiniteMatrixGot != null)
                            IndefiniteMatrixGot(this, mxexception.Spectrum, ParameterStatus.Local | ParameterStatus.Free);
                    }

                    spectra[s].prepareToSearch(SearchLevel.Local, PrepareOptions.ComponentIntensities);
                    Flags |= SearchFlags.IncludeInts;
                    SetParameters(
                        spectraCollection,
                        out parameters,
                        out M,
                        localFreeStatus,
                        SearchFlags.IncludeInts | SearchFlags.IncludeSourceContribution,
                        CheckOptions.RefreshDelta | CheckOptions.SetDefaultValues);
                    localFitter.SetParameters(parameters, localDiffs, M, spectra[s].Container.getEvaluationArray);
                    localFitter.MaxIterationCount = parameters.Length * 100;
                    i = 0;
                    localFitter.Iteration = 0;
                    try {
                        do {

                            for (j = 0; j < parameters.Length; j++)
                                parameters[j].Delta += Math.Abs(parameters[j].Value) * 0.0005;
                            deltachi = chi;
                            chi = localFitter.fit(spectra[s], false);

                        } while (i++ < 10 && localFitter.Iteration < localFitter.MaxIterationCount && (Math.Abs(deltachi - chi) > 1e-3));
                    } catch (IndefiniteMatrixException mxexception) {
                        if (IndefiniteMatrixGot != null)
                            IndefiniteMatrixGot(this, mxexception.Spectrum, ParameterStatus.Local | ParameterStatus.Free);
                    } finally {
                        Flags &= ~SearchFlags.IncludeInts;
                        spectra[s].normalizeAfterSearch(SearchLevel.Local, PrepareOptions.ComponentIntensities, false);
                    }
                    //container.normalizeInts(spectrum, true);
                    spectra[s].Fit = chi;
                    //chiChannelCount = 0;
                    for (i = 0; i < M; i++) {
                        diffs[diffsPosition++] = localDiffs[i];
                        //if (i + localFitter.StartChannel < spectrum.EffectEndChannel) {
                        //    spectrum.Fit += localDiffs[i] * localDiffs[i];
                        //    chiChannelCount++;
                        //}
                    }
                    //spectrum.Fit /= chiChannelCount - localFitter.Parameters.Length;
                    if (firstLocalSearchCall)
                        previousSpectrum = (AnhSpectrum)spectra[s];
                    if (Canceled) break;
                }
            //}
            firstLocalSearchCall = false;
            return !Canceled;
        }

        #region marquardt fit minimalization

        //Fitmrq _mrqfitG = null;
        //Fitmrq _mrqfitL = null;
        //double[][] _mrqdyda = null;
        //double[] _mrqy;
        //double[] _mrqtmpfx0 = null;
        //double[] _mrqa;
        //bool[] _mrqiaG, _mrqiaL;
        //IParameter[] _mrqp = null; //fitting parameters
        //double[,] _mrqdata;

        /// <summary>
        /// initializes buffers a, dyda and ai(L and G). if buffers are nulls or their sizes are different than real parameter set, they are (re)initialized
        /// </summary>
        private void setbuffers() {
            throw new NotSupportedException();
            //int size = 0;
            //int pos = 0;
            //int maxn = 0;
            ////get sizes and set buffer positions
            //foreach (ISpectraContainer container in Containers)
            //    foreach (ISpectrum spectrum in container.Spectra) {
            //        if (spectrum.DataLength > maxn)
            //            maxn = spectrum.DataLength;
            //        spectrum.Parameters.BufferStart = pos;
            //        size += container.Model.setparams(spectrum.Parameters, null, null, null, ParameterStatus.None, spectrum);
            //        pos = size;
            //        spectrum.Parameters.BufferStop = pos - 1;
            //    }
            ////initialize buffers
            //if (_mrqdyda == null || _mrqdyda.Length < size) {
            //    _mrqa = new double[size];
            //    _mrqp = new IParameter[size];
            //    _mrqiaG = new bool[size];
            //    _mrqiaL = new bool[size];
            //    _mrqdyda = new double[size][];
            //    for (int i = 0; i < _mrqdyda.Length; i++)
            //        _mrqdyda[i] = new double[maxn];
            //}
            ////fill buffers
            //foreach (ISpectraContainer container in Containers)
            //    foreach (ISpectrum spectrum in container.Spectra)
            //        container.Model.setparams(spectrum.Parameters, _mrqp, null, null, ParameterStatus.None, spectrum);
        }

        private void adjustprefitter(Fitmrq fitter, object spectra, bool[] ia, SearchFlags includeFlags, ParameterStatus status) {
            throw new NotSupportedException("No prompt preparing method exists");
            //int i;
            //ISpectrum s;
            //IEnumerator<ISpectrum> sen = null;
            //int count = 1;
            //if (spectra is ISpectrum)
            //    s = (ISpectrum)spectra;
            //else {
            //    sen = ((ICollection<ISpectrum>)spectra).GetEnumerator();
            //    count = ((ICollection<ISpectrum>)spectra).Count;
            //    sen.MoveNext();
            //    s = sen.Current;
            //}

            //int[][] datpos = new int[count][];

            //bool[] bflags = GetFlags(includeFlags);
            //for (i = 0; i < ia.Length; i++) {
            //    _mrqa[i] = _mrqp[i].Value;
            //    ia[i] = false;
            //}
            //i = 0;
            //do {
            //    datpos[i] = new int[2];
            //    datpos[i][0] = s.BufferStartPos + (int)s.Parameters[4].Components[0][1].Value;
            //    datpos[i][1] = s.BufferStartPos + (int)s.Parameters[4].Components[0][2].Value;
            //    s.Container.Model.setparams(s.Parameters, null, ia, bflags, status, s);
            //    i++;
            //    if (sen != null)
            //        s = (sen.MoveNext()) ? sen.Current : null;
            //    else
            //        s = null;
            //} while (s != null);
            //fitter.init(datpos, _mrqdata, _mrqa, ia);
        }

        /// <summary>
        /// Search values for first spectrum in each container version with marquardtfit minimizer
        /// </summary>
        public void presearch(ICollection<ISpectrum> spectra) {
            throw new NotSupportedException("No prompt preparing method exists");
            //this._searchMode = SearchMode.Preliminary;
            //Exception error = null;
            //bool searchAgain = false;
            //try {
            //    do {
            //        Flags &= ~SearchFlags.IncludeInts;
            //        if (spectra.Count == 0)
            //            throw new Exception("No spectra to fit");
            //        firstLocalSearchCall = false;

            //        double chi;
            //        foreach (ISpectrum spectrum in spectra) {
            //            AnhSpectraContainer container = (AnhSpectraContainer)spectrum.Container;
            //            container.ResetArrays();
            //            //container.preparePromptInts(spectrum);
            //            spectrum.Fit = double.PositiveInfinity;
            //            container.setLongestRange(); //finds longest spectrum i remembers its right range. There is one array for each spectrum so it must have the longest dimension
            //        }
            //        setbuffers();
            //        adjustprefitter(_mrqfitL, spectra, _mrqiaG, Flags, ParameterStatus.Free);

            //        chi = _mrqfitL.fit(spectra);

            //        foreach (ISpectrum spectrum in spectra) {
            //            AnhSpectraContainer container = (AnhSpectraContainer)spectrum.Container;
            //            container.reduceIntsFromCounts(spectrum);
            //        }
            //        adjustprefitter(_mrqfitL, spectra, _mrqiaG, Flags |= SearchFlags.IncludeInts, ParameterStatus.Free);

            //        chi = _mrqfitL.fit(spectra);

            //        foreach (ISpectrum spectrum in spectra) {
            //            AnhSpectraContainer container = (AnhSpectraContainer)spectrum.Container;
            //            container.normalizeInts(spectrum, true);
            //        }
            //        if (FirstSpectraSearchCompleted != null) {
            //            AsyncFirstSpectraSearchCompletedEventArgs args = new AsyncFirstSpectraSearchCompletedEventArgs(error, Canceled, null);
            //            args.Spectra = spectra;
            //            FirstSpectraSearchCompleted(null, args);
            //            searchAgain = args.SearchAgain;
            //        }
            //    } while (searchAgain);
            //} finally {
            //    _isBusy = false;
            //    this._searchMode = SearchMode.Inactive;
            //}
        }

        public void search(ICollection<ISpectraContainer> containers) {
            throw new NotSupportedException("No prompt preparing method exists");
            //this._searchMode = SearchMode.Main;
            //Exception error = null;
            //FitChangeEventHandler changeEventHandler = new FitChangeEventHandler(fit_Changed);
            //ParameterStatus commonf = ParameterStatus.Common | ParameterStatus.Free;
            //List<ISpectrum> spectra = new List<ISpectrum>();
            //try {
            //    firstLocalSearchCall = true;
            //    Flags &= ~SearchFlags.IncludeInts;

            //    foreach (AnhSpectraContainer container in containers) {
            //        container.ResetArrays();
            //        AnhSpectrum spectrum = (AnhSpectrum)container.Spectra[0];
            //        //container.preparePromptInts(spectrum);
            //        spectrum.Fit = double.PositiveInfinity;
            //        container.setLongestRange(); //finds longest spectrum i remembers its right range. There is one array for each spectrum so it must have the longest dimension
            //        spectra.Add(spectrum);
            //    }

            //    adjustprefitter(_mrqfitG, spectra, _mrqiaG, Flags, commonf);

            //    _mrqfitG.fit(containers);

            //    //normalization
            //    foreach (AnhSpectraContainer container in containers)
            //        foreach (AnhSpectrum spectrum in container.Spectra)
            //            container.normalizeInts(spectrum, false);

            //    setGlobalChi(spectra);
            //} catch (Exception ex) {
            //    error = ex;
            //} finally {
            //    _isBusy = false;
            //    this._searchMode = SearchMode.Inactive;
            //}
            //if (SearchCompleted != null) {
            //    SearchCompleted(this, new AsyncCompletedEventArgs(error, Canceled, null));
            //}
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="target">ISpectraContainer collection </param>
        /// <param name="a">parameter values passed by fitter</param>
        /// <param name="ia">array of freedom flags</param>
        /// <param name="y">function values</param>
        /// <param name="dyda">function first derivatives. first index is parameter</param>
        public void function(object target, double[] a, bool[] ia, out double[] y, out double[][] dyda) {
            throw new NotSupportedException("Marquardt method is not supported");
            //int i, j, start, stop;
            //ParameterStatus localf = ParameterStatus.Local | ParameterStatus.Free;
            //AnhSpectrum previousSpectrum = null;
            //foreach (AnhSpectraContainer container in (ICollection<ISpectraContainer>)target) {
            //    previousSpectrum = null;
            //    foreach (AnhSpectrum spectrum in container.Spectra) {
            //        start = (int)spectrum.Parameters[4].Components[0][1].Value;
            //        stop = (int)spectrum.Parameters[4].Components[0][2].Value;
            //        for (j = spectrum.Parameters.BufferStart; j <= spectrum.Parameters.BufferStop; j++)
            //            if (ia[j])
            //                _mrqp[j].Value = a[j];
            //        //if it is first function call local parameters might have undefined value
            //        if (firstLocalSearchCall && (previousSpectrum != null))
            //            spectrum.copy(previousSpectrum, localf, CalculatedValues);
            //        Flags &= ~SearchFlags.IncludeInts;
            //        adjustprefitter(_mrqfitL, spectrum, _mrqiaL, Flags, localf);

            //        _mrqfitL.fit(spectrum);
            //        container.reduceIntsFromCounts(spectrum);
            //        Flags |= SearchFlags.IncludeInts;

            //        adjustprefitter(_mrqfitL, spectrum, _mrqiaL, Flags, localf);

            //        _mrqfitL.fit(spectrum);
            //        //backup fx0
            //        for (i = spectrum.BufferStartPos; i <= spectrum.BufferEndPos; i++)
            //            _mrqtmpfx0[i - spectrum.BufferStartPos] = _mrqy[i];
            //        //calculate derivatives
            //        for (j = spectrum.Parameters.BufferStart; j <= spectrum.Parameters.BufferStop; j++) {
            //            if (ia[j]) {
            //                if (_mrqp[j] == spectrum.Parameters[4].Components[0][3]) {
            //                    for (i = spectrum.BufferStartPos; i <= spectrum.BufferEndPos; i++)
            //                        _mrqdyda[i][j] = 1;
            //                } else {
            //                    _mrqp[j].Value += _mrqp[j].Delta;
            //                    _mrqfitL.fit(spectrum);
            //                    _mrqp[j].Value -= _mrqp[j].Delta;
            //                    for (i = spectrum.BufferStartPos; i <= spectrum.BufferEndPos; i++)
            //                        _mrqdyda[j][i] = (_mrqy[i] - _mrqtmpfx0[i - spectrum.BufferStartPos]) / _mrqp[j].Delta;
            //                }
            //            }
            //        }
            //        //copy real fx0 to ymod
            //        for (i = spectrum.BufferStartPos; i <= spectrum.BufferEndPos; i++)
            //            _mrqy[i] = _mrqtmpfx0[i - spectrum.BufferStartPos];

            //        Flags &= ~SearchFlags.IncludeInts;
            //        if (firstLocalSearchCall)
            //            previousSpectrum = spectrum;
            //    }
            //}
            //firstLocalSearchCall = false;
            //dyda = _mrqdyda;
            //y = _mrqy;
        }

        /// <summary>
        /// preliminary target function - fits first spectra in selected series
        /// </summary>
        /// <param name="target">collection of spectra which parameters are about to fit</param>
        /// <param name="parameters">parameter values passed by fitter</param>
        /// <param name="y">function values</param>
        /// <param name="dyda">function first derivatives. first index is channel</param>
        public void prefunction(object target, double[] a, bool[] ia, out double[] y, out double[][] dyda) {
            throw new NotSupportedException();
            //double[] t;
            //int i, j, start, stop, nact = 0;
            //ISpectrum spectrum;
            //IEnumerator<ISpectrum> sen = null;
            //j = 0;
            //if (target is ISpectrum)
            //    spectrum = (ISpectrum)target;
            //else {
            //    sen = ((ICollection<ISpectrum>)target).GetEnumerator();
            //    sen.MoveNext();
            //    spectrum = sen.Current;
            //}
            ////foreach (ISpectrum spectrum in (ICollection<ISpectrum>)target) {
            //do {
            //    for (i = spectrum.Parameters.BufferStart; i <= spectrum.Parameters.BufferStop; i++)
            //        _mrqp[i].Value = a[i];
            //    start = (int)spectrum.Parameters[4].Components[0][1].Value;
            //    stop = (int)spectrum.Parameters[4].Components[0][2].Value;
            //    t = spectrum.Container.getTheoreticalSpectrum(spectrum);
            //    for (i = start; i <= stop; i++)
            //        _mrqy[nact + i] = t[i];

            //    //calculate derivatives for this spectrum parameters
            //    for (j = spectrum.Parameters.BufferStart; j <= spectrum.Parameters.BufferStop; j++) {
            //        if (ia[j]) {
            //            //if parameter is background then derivative is equal to 1
            //            if (_mrqp[j] == spectrum.Parameters[4].Components[0][3]) {
            //                for (i = start; i <= stop; i++)
            //                    _mrqdyda[j][i] = 1;
            //            } else { //for all other parameters derivatives must be calculated with difference quotient
            //                _mrqp[j].Value += _mrqp[j].Delta;
            //                t = spectrum.Container.getTheoreticalSpectrum(spectrum);
            //                _mrqp[j].Value -= _mrqp[j].Delta;
            //                for (i = start; i <= stop; i++)
            //                    _mrqdyda[j][i] = (t[i] - _mrqy[nact + i]) / _mrqp[j].Delta;
            //            }
            //        }
            //    }
            //    nact += stop - start;
            //    if (sen != null)
            //        spectrum = (sen.MoveNext()) ? sen.Current : null;
            //    else
            //        spectrum = null;
            //} while (spectrum != null);
            //y = _mrqy;
            //dyda = _mrqdyda;
            //if (Canceled) return;
        }

        #endregion

        #region NET

        protected bool netLocalSearch(object target, double[] diffs) {
            //int diffsPosition = 1;
            ParameterStatus localFreeStatus = ParameterStatus.Local | ParameterStatus.Free;
            AnhSpectrum previousSpectrum = null;
            foreach (AnhSpectraContainer container in this._containers) {
                foreach (AnhSpectrum spectrum in container.Spectra) {
                    if (firstLocalSearchCall && (previousSpectrum != null)) {
                        spectrum.copy(previousSpectrum.Parameters, localFreeStatus, CalculatedValues);
                    }
                    //int ipi = -1;
                    //int start = (int)spectrum.Parameters[4].Components[0]["start"].Value;
                    //int stop = (int)spectrum.Parameters[4].Components[0][2].Value;
                    //int M = stop - start + 1;

                    //send(spectrum, localDiffs, out ipi);
                    //for (int i = 1; i < M + 1; i++) {
                    //    diffs[diffsPosition++] = localDiffs[i];
                    //}
                    Despatcher.despatch(Containers, diffs, this._currentEvelListeners);
                    if (firstLocalSearchCall)
                        previousSpectrum = spectrum;
                    //if ((ipi != -1) && (OnIndependentParam != null)) {
                    //    List<IParameter> parameters = container.Model.getParameters(localFreeStatus, spectrum.Parameters, new bool[] {IncludeInts, false});
                    //    OnIndependentParam(parameters[ipi]);
                    //}
                }
            }
            firstLocalSearchCall = false;
            return !Canceled;
        }

        public class Despatcher {

            private class RequestState {
                public string url;
                public HttpWebRequest request;
                public List<ISpectrum> spectra;
                public RequestState(string url, HttpWebRequest request, List<ISpectrum> spectra) {
                    this.url = url;
                    this.request = request;
                    this.spectra = spectra;
                }
            }

            private static double[] _diffs;
            private static ICollection<ISpectraContainer> _containers;
            private static Dictionary<string, bool> _listeners;
            private static char[] _separators = new char[] { '\t', ' ', '\n' };
            public static IParameter independentParameter;
            private static int _spectraToAnalyse = 0;
            private static int _spectraAnalysed = 0;
            private static List<ISpectrum> _package = new List<ISpectrum>();
            private static string _errorMessage;
            private static XmlReaderSettings readerSettings;

            //private static XmlDocument pack(ICollection<ISpectrum> spectra, ISpectraContainer container, IProject project, ref int diffPosition) {
            //    XmlDocument doc = new XmlDocument();
            //    doc.AppendChild(doc.CreateXmlDeclaration("1.0", null, null));
            //    XmlElement root = doc.CreateElement("request");
            //    //root attributes
            //    XmlAttribute attr = doc.CreateAttribute("modelclass");
            //    attr.Value = container.Model.GetType().ToString();
            //    root.Attributes.Append(attr);
            //    attr = doc.CreateAttribute("projectclass");
            //    attr.Value = container.ParentProject.GetType().ToString();
            //    root.Attributes.Append(attr);
            //    //Search flags
            //    XmlElement flags = doc.CreateElement("Searchflags");
            //    foreach (string flagName in project.Flags.Keys) {
            //        attr = doc.CreateAttribute(flagName);
            //        attr.Value = project.Flags[flagName].ToString();
            //        flags.Attributes.Append(attr);
            //    }
            //    attr = doc.CreateAttribute("includeContrib");
            //    attr.Value = false.ToString();
            //    flags.Attributes.Append(attr);
            //    root.AppendChild(flags);
            //    //Search options
            //    XmlElement options = doc.CreateElement("options");
            //    attr = doc.CreateAttribute("status");
            //    attr.Value = ((int)(ParameterStatus.Local | ParameterStatus.Free)).ToString();
            //    options.Attributes.Append(attr);
            //    root.AppendChild(options);
            //    //spectrum
            //    foreach (ISpectrum spectrum in spectra) {
            //        XmlNode spectrumNode = spectrum.exportToXmlNode(doc, true);
            //        //cache
            //        XmlElement cache = doc.CreateElement("cache");
            //        attr = doc.CreateAttribute("diffposition");
            //        attr.Value = diffPosition.ToString();
            //        cache.Attributes.Append(attr);
            //        spectrumNode.AppendChild(cache);
            //        root.AppendChild(spectrumNode);
            //        int start = (int)spectrum.Parameters[4].Components[0]["start"].Value;
            //        int stop = (int)spectrum.Parameters[4].Components[0][2].Value;
            //        int M = stop - start + 1;
            //        diffPosition += M;
            //    }
            //    doc.AppendChild(root);
            //    return doc;
            //}

            private static void sendPackage(Stream output, ICollection<ISpectrum> spectra, ISpectraContainer container, IProject project, ref int diffPosition) {
                //XmlDocument doc = new XmlDocument();
                XmlWriter writer = XmlWriter.Create(output);
                //doc.AppendChild(doc.CreateXmlDeclaration("1.0", null, null));
                //XmlElement root = doc.CreateElement("request");
                writer.WriteStartElement("request");
                //root attributes
                writer.WriteAttributeString("modelclass", container.Model.GetType().ToString());
                //XmlAttribute attr = doc.CreateAttribute("modelclass");
                //attr.Value = container.Model.GetType().ToString();
                //root.Attributes.Append(attr);
                writer.WriteAttributeString("projectclass", container.ParentProject.GetType().ToString());

                ////Search flags
                //writer.WriteStartElement("Searchflags");
                //foreach (string flagName in project.Flags.Keys) {
                //    writer.WriteAttributeString(flagName, project.Flags[flagName].ToString());
                //}
                //writer.WriteAttributeString("includeContrib", false.ToString());
                //writer.WriteEndElement(); //Searchflags

                writer.WriteStartElement("options");
                //attr = doc.CreateAttribute("status");
                writer.WriteAttributeString("status", ((int)(ParameterStatus.Local | ParameterStatus.Free)).ToString());
                //attr.Value = ((int)(ParameterStatus.Local | ParameterStatus.Free)).ToString();
                //options.Attributes.Append(attr);
                writer.WriteEndElement(); //options
                //root.AppendChild(options);
                //packages
                foreach (ISpectrum spectrum in spectra) {
                    writer.WriteStartElement("package");
                    //XmlNode spectrumNode = spectrum.exportToXmlNode(doc, true);
                    spectrum.writeToXml(writer, true, false);
                    //cache
                    //XmlElement cache = doc.CreateElement("cache");
                    writer.WriteStartElement("cache");
                    writer.WriteAttributeString("diffposition", diffPosition.ToString());
                    writer.WriteEndElement(); //cache
                    //attr = doc.CreateAttribute("diffposition");
                    //attr.Value = diffPosition.ToString();
                    //cache.Attributes.Append(attr);
                    //spectrumNode.AppendChild(cache);
                    //root.AppendChild(spectrumNode);
                    int start = (int)spectrum.Parameters[4].Components[0][1].Value;
                    int stop = (int)spectrum.Parameters[4].Components[0][2].Value;
                    int M = stop - start + 1;
                    diffPosition += M;
                    writer.WriteEndElement(); //package
                }
                //doc.AppendChild(root);
                writer.WriteEndElement(); //request
                //return doc;
                writer.Close();
            }

            //public static void unpack(IAsyncResult asyncResult) {
            //    RequestState state = (RequestState)asyncResult.AsyncState;
            //    try {

            //        HttpWebResponse response = (HttpWebResponse)state.request.EndGetResponse(asyncResult);
            //        System.IO.Stream input = response.GetResponseStream();
            //        XmlDocument doc = new XmlDocument();
            //        doc.Load(input);
            //        input.Close();
            //        IEnumerator<ISpectrum> spectrumEnumerator = state.spectra.GetEnumerator();
            //        if (doc.GetElementsByTagName("error").Count == 0) {
            //            foreach (XmlElement spectrumNode in doc.GetElementsByTagName("spectrum")) {
            //                //fit
            //                //XmlNode element = doc.DocumentElement.GetElementsByTagName("fit")[0];
            //                spectrumEnumerator.MoveNext();
            //                //state.spectrum.Fit = double.Parse(spectrumNode.Attributes["fit"].Value);
            //                spectrumEnumerator.Current.Fit = double.Parse(spectrumNode.Attributes["fit"].Value);
            //                //diffpos
            //                XmlNode element = spectrumNode.GetElementsByTagName("cache")[0];
            //                int diffpos = Int32.Parse(element.Attributes["diffposition"].Value);
            //                //diffs
            //                element = spectrumNode.GetElementsByTagName("differences")[0];
            //                string[] diffsStr = element.InnerText.Split(_separators, StringSplitOptions.RemoveEmptyEntries);
            //                lock (_diffs) {
            //                    for (int i = 1; i < diffsStr.Length; i++) {
            //                        _diffs[i + diffpos - 1] = Double.Parse(diffsStr[i]);
            //                    }
            //                }
            //                //update spectrum values
            //                //element = doc.DocumentElement.GetElementsByTagName("spectrum")[0];
            //                spectrumEnumerator.Current.copy((XmlElement)spectrumNode, ParameterStatus.Local | ParameterStatus.Free);
            //                _listeners[state.url] = true;
            //                _spectraAnalysed++;
            //                _errorMessage = "";
            //            }
            //        } else {
            //            _errorMessage = doc.GetElementsByTagName("error")[0].InnerText;
            //        }
            //    } catch { //(Exception e) {
            //        _listeners.Remove(state.url);
            //    }
            //}

            public static void unpack(IAsyncResult asyncResult) {
                RequestState state = (RequestState)asyncResult.AsyncState;
                try {
                    HttpWebResponse response = (HttpWebResponse)state.request.EndGetResponse(asyncResult);
                    System.IO.Stream input = response.GetResponseStream();
                    if (readerSettings == null) {
                        readerSettings = new XmlReaderSettings();
                        readerSettings.IgnoreWhitespace = true;
                    }
                    XmlReader reader = XmlReader.Create(input, readerSettings);


                    IEnumerator<ISpectrum> spectrumEnumerator = state.spectra.GetEnumerator();
                    reader.ReadToFollowing("response");
                    reader.Read();
                    if (reader.Name != "error") {
                        //reader.ReadToFollowing("package");
                        do {
                            spectrumEnumerator.MoveNext();
                            //fit
                            while (reader.MoveToNextAttribute()) {
                                switch (reader.Name) {
                                    case "fit": spectrumEnumerator.Current.Fit = Double.Parse(reader.Value); break;
                                }
                            }
                            reader.MoveToElement();
                            //spectrum
                            reader.ReadToDescendant("spectrum");
                            XmlReader spectrumReader = reader.ReadSubtree();
                            spectrumEnumerator.Current.copy(spectrumReader, ParameterStatus.Local | ParameterStatus.Free);
                            spectrumReader.Close();
                            //cache
                            reader.ReadToNextSibling("cache");
                            int diffposition = 1;
                            while (reader.MoveToNextAttribute()) {
                                switch (reader.Name) {
                                    case "diffposition": diffposition = Int32.Parse(reader.Value); break;
                                }
                            }
                            //diffs
                            reader.ReadToNextSibling("differences");
                            reader.Read();
                            string[] diffsStr = reader.ReadString().Split(_separators, StringSplitOptions.RemoveEmptyEntries);
                            lock (_diffs) {
                                for (int i = 1; i < diffsStr.Length; i++) {
                                    _diffs[i + diffposition - 1] = Double.Parse(diffsStr[i]);
                                }
                            }
                            //reader.ReadEndElement(); //differences
                            _spectraAnalysed++;
                        } while (reader.ReadToFollowing("package"));
                    }
                    _listeners[state.url] = true;
                    _errorMessage = "";
                    reader.Close();
                } catch { //(Exception e) {
                    _listeners.Remove(state.url);
                }
            }

            public static void despatch(ICollection<ISpectraContainer> containers, double[] diffs, string[] listeners) {
                _diffs = diffs;
                _containers = containers;
                _spectraToAnalyse = 0;
                _spectraAnalysed = 0;
                int spectrumCountInPackage = 0;
                foreach (ISpectraContainer container in containers)
                    spectrumCountInPackage += container.Spectra.Count;
                spectrumCountInPackage /= listeners.Length;
                independentParameter = null;
                if (_listeners == null)
                    _listeners = new Dictionary<string, bool>();
                else
                    _listeners.Clear();
                foreach (string listenerURL in listeners) {
                    _listeners.Add(listenerURL, true);
                }
                int diffPosition = 1;
                _package.Clear();
                //lock (_listeners) {
                foreach (ISpectraContainer container in containers) {
                    foreach (ISpectrum spectrum in container.Spectra) {
                        _spectraToAnalyse++;
                        //int start = (int)spectrum.Parameters[4].Components[0][1].Value;
                        //int stop = (int)spectrum.Parameters[4].Components[0][2].Value;
                        //int M = stop - start + 1;
                        _package.Add(spectrum);
                        if (_package.Count >= spectrumCountInPackage) {
                            bool sent = false;
                            //foreach (string listenerURL in listeners) {
                            while (!sent && _listeners.Count > 0) {
                                foreach (string listenerURL in listeners) {
                                    if (_listeners[listenerURL]) {
                                        _listeners[listenerURL] = false;
                                        //XmlDocument requestXML = pack(_package, container, container.ParentProject, ref diffPosition);
                                        HttpWebRequest request = (HttpWebRequest)WebRequest.Create(listenerURL);
                                        //request.Timeout = 100;
                                        request.Method = "POST";
                                        request.ContentType = "text/xml";
                                        System.IO.Stream output = request.GetRequestStream();
                                        //requestXML.Save("requestPack.xml");
                                        //requestXML.Save(output);

                                        sendPackage(output, _package, container, container.ParentProject, ref diffPosition);
                                        //sendPackage(new FileStream("requestPack.xml", FileMode.Create), _package, container, container.ParentProject, ref diffPosition);
                                        output.Close();
                                        List<ISpectrum> package = new List<ISpectrum>(_package);
                                        request.BeginGetResponse(new AsyncCallback(unpack), new RequestState(listenerURL, request, package));
                                        sent = true;
                                        _package.Clear();
                                        break;
                                    }
                                }
                            }
                        }
                        //diffPosition += M;
                    }
                }
                //}
                if (_spectraAnalysed != _spectraToAnalyse) {
                    throw new Exception(String.Format("Couldn't use network connections to analyse spectra!{0}", ((_errorMessage != "") ? "\nServer has thrown exception " + _errorMessage : "")));
                }
            }

        }

        //public void Search(ISpectraContainer container, ISpectrum spectrum, out double[] diffs, out int ipi) {
        //    try {
        //        //if (localMinsq == null)
        //        //    localMinsq = new Minsq();
        //        //container.Model.setDeltaX(spectrum.Parameters);
        //        //container.Model.checkParameters(spectrum.Parameters, CheckOptions.RefreshDelta);
        //        ParameterStatus localFreeStatus = ParameterStatus.Local | ParameterStatus.Free;
        //        List<IParameter> parameters = container.Model.getParameters(localFreeStatus, spectrum.Parameters, new bool[] { IncludeInts, false });
        //        foreach (IParameter parameter in parameters)
        //            container.Model.checkParameter(parameter, CheckOptions.RefreshDelta);


        //        int start = (int)spectrum.Parameters[4].Components[0][1].Value;
        //        int stop = (int)spectrum.Parameters[4].Components[0][2].Value;
        //        int M = stop - start + 1;
        //        int N = parameters.Count - 1;

        //        diffs = new double[M + 1];
        //        double chi = double.PositiveInfinity;
        //        //int ipi; //independedParamId
        //        localMinsq.findMinimum(container.getEvaluationArray, spectrum, M, N, diffs, parameters, out chi, parameters.Count * 10, 0.1, out ipi, false);
        //        spectrum.Fit = chi;
        //    } catch(Exception e) {
        //        throw new Exception(String.Format("Exception thrown by ISpectraContainer.getEvaluationArray(object, double[]): {0}", e.Message));
        //    }
        //}

        #endregion

        #region Debug

        private void WritePrivateFields(object o, StringBuilder builder) {
            Type type = o.GetType();
            foreach (FieldInfo fi in type.GetFields(BindingFlags.NonPublic | BindingFlags.Instance)) {
                builder.AppendFormat("{0} = {1}\n", fi.Name, fi.GetValue(o));
            }
        }

        public override string SaveObjectState(string filePath) {
            StringBuilder builder = new StringBuilder();
            //private fields of project
            WritePrivateFields(this, builder);
            //private fields of all containers
            foreach (ISpectraContainer container in Containers) {
                builder.AppendFormat("--------------{0} private fields--------------\n", container.Name);
                WritePrivateFields(container, builder);
            }
            //parameters and arrays in first spectrum in each container
            foreach (ISpectraContainer container in Containers) {
                List<double[]> arrays = new List<double[]>();
                Type t = container.GetType();
                FieldInfo fi = t.GetField("ug", BindingFlags.Instance | BindingFlags.NonPublic);
                arrays.Add((double[])fi.GetValue(container));
                Dictionary<Evel.interfaces.IComponent, double[]> comps = (Dictionary<Evel.interfaces.IComponent, double[]>)t.GetField("comps", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(container);

                foreach (IGroup group in container.Spectra[0].Parameters) {
                    builder.AppendFormat("<{0}>\n", group.Definition.name);
                    foreach (Evel.interfaces.IComponent component in group.Components) {

                        if (comps.ContainsKey(component))
                            arrays.Add(comps[component]);

                        foreach (IParameter parameter in component)
                            builder.AppendFormat("{0}\t", parameter.Value);
                        builder.AppendLine();
                    }
                }

                for (int c = 0; c < arrays[0].Length; c++) {
                    foreach (double[] a in arrays)
                        builder.AppendFormat("{0}\t", a[c]);
                    builder.AppendLine();
                }

            }


            if (filePath != string.Empty)
                using (StreamWriter writer = new StreamWriter(filePath)) {
                    writer.Write(builder.ToString());
                }
            return builder.ToString();
        }

        #endregion

    }
}
