using System;
using System.Collections.Generic;
using System.Text;
using Evel.interfaces;
using Evel.share;
using Evel.engine.algorythms;
using System.Xml;
using System.Linq;

namespace Evel.engine.anh {

    public partial class AnhSpectraContainer : SpectraContainerBase {

        private LTCurve curve;
        public int _TEST_counts = 0;
        public double _TEST_sum = 0;
        //private Dictionary<IComponent, double[]> comps;
        private double[][] comps;
        private double[] shape;
        private int _longestRange = 0;
        private static Component zeroComponent = new Component(new ParameterDefinition[] { }, false, null);
        private double[] _theoreticalSpectrum = null;
        //private ValuesDictionary[] _valuesDictionary;
        private List<ICurveParameters> _ltCurveParams = null;

        public override void ResetArrays() {
            ug = null;
            agi = null;
            beta = null;
            shape = null;   
            sourceAdd = null;
            comps = null;
            _ltCurveParams.Clear();
        }

        /// <summary>
        /// finds longest spectrum and remembers its right range. 
        /// There is one array for each spectrum so it must have the longest dimension
        /// </summary>
        public void setLongestRange() {
            int i;
            for (i = 0; i < this._spectra.Count; i++)
            {
                double rightRange = this._spectra[i].Parameters[4].Components[0][2].Value;
                if (rightRange > _longestRange)
                    _longestRange = (int)rightRange;
            }

            if (comps == null || comps.Length <= Spectra[0].Parameters[1].Components.Size + Spectra[0].Parameters[2].Components.Size)
            {
                if (_ltCurveParams.Count == 0)
                    Model.convert(_ltCurveParams, Spectra[0].Parameters);
                comps = new double[Spectra[0].Parameters[1].Components.Size + Spectra[0].Parameters[2].Components.Size + 1][];
                comps[0] = new double[0];
            }

            if (comps[0].Length < _longestRange) {
                for (i = 0; i < comps.Length; i++)
                    comps[i] = new double[_longestRange + 3];
            }

            //shape
            if (shape == null)
                shape = new double[_longestRange + 3];
            else {
                if (shape.Length < _longestRange + 3)
                    shape = new double[_longestRange + 3];
            }
            //beta
            if (beta == null)
                beta = new double[_longestRange + 1];
            else {
                if (beta.Length < _longestRange + 1)
                    beta = new double[_longestRange + 1];
            }
            //sourceAdd
            if (sourceAdd == null)
                sourceAdd = new double[_longestRange + 1];
            else {
                if (sourceAdd.Length < _longestRange + 1)
                    sourceAdd = new double[_longestRange + 1];
            }
        }

        private void init() {
            this.curve = new LTCurve();
            this._ltCurveParams = new List<ICurveParameters>();
        }

        public AnhSpectraContainer(IProject parentProject, IModel model)
            : base(parentProject, model) {
            init();
        }

        /// <summary>
        /// Spectra container manage spectra list. It is responsible for generating theoretical 
        /// spectrum and other common routines related to spectra managing
        /// </summary>
        /// <param name="model">Theoretical model, which defines conversion of parameters</param>
        /// <param name="spectraPath">Path to directory where spectra files are stored</param>
        /// <param name="groupsDefinition">Definition of parameter groups. If this argument is null, default model groupsDefinition is in use</param>
        public AnhSpectraContainer(IProject parentProject, string name, IModel model, ICollection<string> spectraPaths, ICollection<GroupDefinition> groupsDefinition)
            : base(parentProject, name, model, spectraPaths, groupsDefinition) {
            init();
            //foreach (AnhSpectrum spectrum in Spectra)
            //    setDefaultRangeValues(spectrum, true, true, true, true);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="filePath">path to xml file holding parameters of each spectrum</param>
        public AnhSpectraContainer(IProject parentProject, XmlReader reader, string modelDirectory)
            : base(parentProject, reader, modelDirectory) {
            init();
        }

        /// <summary>
        /// Calculates shape of each component with LTCurve routine
        /// </summary>
        private void setShapes(ISpectrum spectrum, bool normedPrompt) { //, Dictionary<IComponent, double[]> comps) {
            //double[] tempArray = null;
            IGroup prompt = spectrum.Parameters[3];
            double ifree = 1;
            double tmpSum = 0;
            //bool normedPrompt = false;
            int i;

            //for (i = 0; i < prompt.Components.Size; i++)
            //    tmpSum += prompt.Components[i][0].Value;
            //if (Math.Abs(tmpSum - 1) > LTCurve.VARIABLE_ACCURACY) {
            if (!normedPrompt) {
                tmpSum = 1;
                for (i = 1; i < prompt.Components.Size; i++) {
                    if ((prompt.Components[i][0].Status & ParameterStatus.Fixed) == (ParameterStatus.Fixed)) {
                        ifree -= prompt.Components[i][0].Value;
                    } else {
                        tmpSum += prompt.Components[i][0].Value * prompt.Components[i][0].Value;
                    }
                }
                //tmpSum += prompt.Components[0][0].Value * prompt.Components[0][0].Value;
            }
            //} else {
            //    normedPrompt = true;
            //}

            Model.convert(_ltCurveParams, spectrum.Parameters);
            //preparing labour arrays
            if (comps == null) {
                setLongestRange();
            }
            else if (shape.Length < _longestRange + 3 || comps.Length < _ltCurveParams.OfType<LTCurveParams>().Max(_p => _p.id) + 1)// comps.Length < _ltCurveParams.Count / prompt.Components.Size + 1)
            {
                setLongestRange();
            }
            else
            {
                for (i = 1; i < comps.Length; i++)
                    comps[0].CopyTo(comps[i], 0);
            }

            LTCurveParams p;
            for (int pi = 0; pi < _ltCurveParams.Count; pi++) {
                p = (LTCurveParams)_ltCurveParams[pi];
                curve.curve(ref shape, p);
                double promptInt;
                if (normedPrompt || ((p.promptComponent[0].Status & ParameterStatus.Fixed) == ParameterStatus.Fixed))
                    promptInt = p.promptComponent[0].Value;
                else
                    promptInt = ifree * p.promptComponent[0].Value * p.promptComponent[0].Value / tmpSum;

                for (i = p.nstart; i <= p.nstop; i++)
                    comps[p.id + 1][i] += shape[i] * p.fraction * promptInt;
            }
        }

        private bool setQuantities(ISpectrum spectrum, bool includeInts) {
            int n, c;
            bool res = true;
            //if (activeSpectrum != spectrum) {
                getGroups(spectrum);
                activeSpectrum = spectrum;
            //}
            if (includeInts) {
                //obliczanie intensywności na podstawie tych zaproponowanych przez search
                //search podaje intensywności w pewnych liczbach. Należy unormować te liczby do jedności
                //ponieważ wszystko idzie w jednej pętli na początku obliczany jest wkład składowych próbki:
                //wykład_próbki = 1 - wkład_źródła. Potem wkład_źródła = 1 - wkład_próbki
                double scontrib; // = 1 / (1 + Math.Abs(((ContributedGroup)spectrum.Parameters[2]).contribution.Value));
                double contrib = 1;
                if (sourceGroup != null) {
                    if (!isFixed(((ContributedGroup)spectrum.Parameters[2]).contribution))
                        scontrib = Math.Abs(((ContributedGroup)spectrum.Parameters[2]).contribution.Value) / (1 + Math.Abs(((ContributedGroup)spectrum.Parameters[2]).contribution.Value));
                    else
                        scontrib = Math.Abs(((ContributedGroup)spectrum.Parameters[2]).contribution.Value);
                    contrib -= scontrib;
                }
                for (int g = 1; g < 3; g++) {
                    double sqsum = 1;  //suma kwadratów
                    double fsum = 1;   //1 - suma intensywności zablokowanych (część wolna)
                    for (c = 1; c < spectrum.Parameters[g].Components.Size; c++)
                        if (isFixed(spectrum.Parameters[g].Components[c][0])) {
                            fsum -= spectrum.Parameters[g].Components[c][0].Value;
                            ((ExtComponent)spectrum.Parameters[g].Components[c]).IntInCounts = spectrum.RangeArea * contrib * spectrum.Parameters[g].Components[c][0].Value;
                        } else
                            sqsum += spectrum.Parameters[g].Components[c][0].Value * spectrum.Parameters[g].Components[c][0].Value;
                    for (c = 0; c < spectrum.Parameters[g].Components.Size; c++)
                        if (!isFixed(spectrum.Parameters[g].Components[c][0]))
                            ((ExtComponent)spectrum.Parameters[g].Components[c]).IntInCounts = spectrum.RangeArea * contrib * fsum * spectrum.Parameters[g].Components[c][0].Value * spectrum.Parameters[g].Components[c][0].Value / sqsum;
                    if ((contrib = 1 - contrib) == 0.0)
                        break;//następna pętla będzie wyznaczała zliczenia dla źródła - dlatego wkład źródła
                }

            } else {
                //obliczanie intensywności w liczbach zliczeń za pomocą układu równań liniowych.
                //Wykonywane w :
                //1. SearchMode.Preliminary - pierwsza część wstępnego dopasowywania pierwszych widm
                //2. SearchMode.Main w procedurze lokalnej dla parametrów o statusie local free
                //      przed włączeniem ewentualnych intensywności do parametrów minimalizowanych
                EquationRow[] equations;
                double[] x;
                ugPgUsPs(spectrum, includeInts);
                getMainMatrix(spectrum, includeInts, out equations, out x, out n);
                //double sampleDoubleAsterixSum = ((ContributedGroup)sampleGroup).MemoryInt;
                //double sourceDoubleAsterixSum = sourceGroup.MemoryInt;
                ExtComponent component;
                bool sourceCondition = specialSourceCondition(sourceGroup);
                res = Gauss.solve(equations, x, n - 1);
                //if (res) {
                    //---------------usuwanie wartosci ujemnych wersja delphi-----------------
                    double s = 0;
                    bool includedBackground = false;
                    if (rangesGroup != null) 
                        if (!isFixed(rangesGroup.Components[0][3]) && !isIntInSearch(rangesGroup.Components[0][3]))
                            includedBackground = true;

                    bool positivesOnly = true;
                    //double globalArea = 0;
                    //if ((globalArea = ((ContributedGroup)sampleGroup).groupArea + ((ContributedGroup)sourceGroup).groupArea) == 0) {
                    //    for (c = (int)spectrum.Parameters[4].Components[0][1].Value; c < (int)spectrum.Parameters[4].Components[0][2].Value; c++)
                    //        globalArea += spectrum.ExperimentalSpectrum[c];
                    //    globalArea -= (spectrum.Parameters[4].Components[0][2].Value - spectrum.Parameters[4].Components[0][1].Value) * spectrum.Parameters[4].Components[0][3].Value;
                    //}

                    for (c = 0; c < n; c++) {
                        if (x[c] < 0) {
                            positivesOnly = false;
                            x[c] = 0.01 * spectrum.RangeArea;
                        }
                    }
                    if (!positivesOnly) {

                        //double area = 0;
                        //for (c = (int)spectrum.Parameters[4].Components[0][1].Value; c < (int)spectrum.Parameters[4].Components[0][2].Value; c++)
                        //    area += spectrum.ExperimentalSpectrum[c];
                        //area -= (spectrum.Parameters[4].Components[0][1].Value - spectrum.Parameters[4].Components[0][2].Value) * spectrum.Parameters[4].Components[0][3].Value;

                        for (c = 0; c < (includedBackground ? n-1 : n); c++)
                            s += x[c];
                        for (c = 0; c < (includedBackground ? n-1 : n); c++)
                            x[c] = x[c] * spectrum.RangeArea / s;
                    }
                    //********
                    //przypisywanie wyznaczonych liczb zliczeń w układzie równań odpowiednim intensywnościom
                    //********
                    int xId = 0;
                    //----------------sample group----------------
                    sampleGroup.groupArea = 0;
                    for (c = 0; c < sampleGroup.Components.Size; c++) {
                        component = (ExtComponent)sampleGroup.Components[c];

                        if (c == 0 || (!isIntInSearch(component[0]) && !isFixed(component[0]))) {
                            sampleGroup.groupArea += x[xId];
                            component[0].Value = x[xId];
                            xId++;
                        }
                    }
                    //----------------source group----------------
                    if (sourceGroup != null) {
                        sourceGroup.groupArea = 0;

                        for (c = 0; c < sourceGroup.Components.Size; c++) {
                            component = (ExtComponent)sourceGroup.Components[c];
                            if (c==0 && sourceCondition)
                                continue;
                            //if (isIntInSearch(component[0])) {
                            //    sourceDoubleAsterixSum += component[0].Value;
                            //}
                            if (c==0 || !isIntInSearch(component[0]) && !isFixed(component[0])) {// (component == sourceGroup.Components[0])) {
                                sourceGroup.groupArea += x[xId];
                                component[0].Value = x[xId];
                                xId++;
                            }
                        }

                        //wkład źródła
                        if (!sourceCondition) { //wkład wyznaczany na podstawie wyniku układu równań
                            double alfa = sourceGroup.groupArea / sampleGroup.groupArea;
                            sourceGroup.contribution.Value = 1 / (1 + 1 / alfa);                        
                            //sourceGroup.contribution.Value = 1 - 1 / alfa; //contrib test
                        } else { //wkład źródła wyznaczany w searchu globalnym lub wartość ustalona
                            //calculate int0
                            double scontrib;// = Math.Abs(sourceGroup.contribution.Value);
                            if (!isFixed(sourceGroup.contribution))
                                scontrib = Math.Abs(sourceGroup.contribution.Value) / (1 + Math.Abs(sourceGroup.contribution.Value));
                            else
                                scontrib = Math.Abs(sourceGroup.contribution.Value);
                            double alfa = 1 / (1 / scontrib - 1);
                            //double alfa = 1 / Math.Abs(1 / sourceGroup.contribution.Value - 1);
                            //double alfa = 1 / Math.Abs(1 - sourceGroup.contribution.Value); //contrib test
                            if (double.IsInfinity(alfa))
                                alfa = 1e20;
                            sourceGroup.Components[0][0].Value = alfa * (sampleGroup.groupArea + sourceGroup.groupArea) - sourceGroup.groupArea;
                            sourceGroup.groupArea += sourceGroup.Components[0][0].Value;
                        }
                    }

                    if (includedBackground)            
                        rangesGroup.Components[0][3].Value = x[xId++];
                    //}
                    //********************normalizacja wyznaczonych intensywności*********************
                    double tmpchi;
                    if (sourceGroup == null)
                        tmpchi = sampleGroup.groupArea / spectrum.RangeArea;
                    else
                        tmpchi = (sampleGroup.groupArea + sourceGroup.groupArea) / spectrum.RangeArea;
                    //sample
                    sampleGroup.groupArea /= tmpchi;
                    for (c = 0; c < sampleGroup.Components.Size; c++) {
                        component = (ExtComponent)sampleGroup.Components[c];
                        if (c == 0 || (!isIntInSearch(component[0]) && !isFixed(component[0])))
                            component[0].Value /= tmpchi;
                    }
                    //source
                    if (sourceGroup != null) {
                        sourceGroup.groupArea /= tmpchi;
                        for (c = 0; c < sourceGroup.Components.Size; c++) {
                            component = (ExtComponent)sourceGroup.Components[c];
                            if (c == 0 || !isIntInSearch(component[0]) && !isFixed(component[0]))
                                component[0].Value /= tmpchi;
                        }
                    }

                    //********************liczby zliczeń każdej ze składowych (właściwość IntInCounts)******************
                    //--------------------sample---------------------
                    double tmpSum = 1;
                    double ifree = 1;
                    //wyznaczanie współczynnika normującego
                    for (c = 1; c < sampleGroup.Components.Size; c++) {
                        if (!isIntInSearch(sampleGroup.Components[c][0]))
                            ifree -= sampleGroup.Components[c][0].Value;
                        else
                            tmpSum += sampleGroup.Components[c][0].Value * sampleGroup.Components[c][0].Value;
                    }
                    //liczby zliczeń
                    for (c = 0; c < sampleGroup.Components.Size; c++) {
                        component = (ExtComponent)sampleGroup.Components[c];
                        if (isFixed(component[0])) //ustalony ulamek
                            component.IntInCounts = component[0].Value * sampleGroup.groupArea;
                        else {
                            if (c == 0 || !isIntInSearch(component[0]))  //piersza skladowa albo intensywnosc z rownan linowych - liczba zliczen
                                component.IntInCounts = Math.Abs(component[0].Value) * pg;
                            else //intensywnosc wyliczona w search'u
                                //component.IntInCounts = sampleGroup.groupArea * pg * component[0].Value / sampleGroup.MemoryInt;
                                component.IntInCounts = sampleGroup.groupArea * ifree * component[0].Value * component[0].Value / tmpSum;

                        }
                    }
                    //--------------------source--------------------
                    if (sourceGroup != null) {
                        //wyznaczanie współczynnika normującego
                        tmpSum = 1;
                        ifree = 1;
                        for (c = 1; c < sourceGroup.Components.Size; c++) {
                            if (!isIntInSearch(sourceGroup.Components[c][0]))
                                ifree -= sourceGroup.Components[c][0].Value;
                            else
                                tmpSum += sourceGroup.Components[c][0].Value * sourceGroup.Components[c][0].Value;
                        }
                        //liczby zliczeń
                        for (c = 0; c < sourceGroup.Components.Size; c++) {
                            component = (ExtComponent)sourceGroup.Components[c];

                            if (isFixed(component[0])) //ustalony ulamek
                                component.IntInCounts = component[0].Value * sourceGroup.groupArea;
                            else {
                                if (c == 0 || !isIntInSearch(component[0]))  //piersza skladowa albo intensywnosc z rownan liniowych - liczba zliczen
                                    component.IntInCounts = Math.Abs(component[0].Value) * ps;
                                else //intensywnosc wyliczona w search'u
                                    component.IntInCounts = sourceGroup.groupArea * ifree * component[0].Value * component[0].Value / tmpSum;
                            }
                        }

                    }

                //} //gaus
            }
            return res;
        }

        public void prepareSInts(ISpectrum spectrum) {
            for (int gr = 1; gr <= 2; gr++) {
                for (int c = 1; c < spectrum.Parameters[gr].Components.Size; c++)
                    if (!isFixed(spectrum.Parameters[gr].Components[c][0]))
                        spectrum.Parameters[gr].Components[c][0].Value = Math.Sqrt(spectrum.Parameters[gr].Components[c][0].Value / spectrum.Parameters[gr].Components[0][0].Value);
                spectrum.Parameters[gr].Components[0][0].Value = 1;
            }
        }

        public void reduceIntsFromCounts(ISpectrum spectrum) {
            int c;
            IComponent component;
            double sum;
            foreach (IGroup group in spectrum.Parameters) {
                if (group is ContributedGroup) {
                    group.Components[0][0].Value = 1;
                    sum = 0;
                    for (c = group.Components.Size - 1; c > 0; c--) {
                        component = group.Components[c];
                        if (!component[0].HasReferenceValue && !isIntInSearch(component[0]) || component == group.Components[0])
                            component[0].Value = ((ExtComponent)component).IntInCounts / ((ContributedGroup)group).groupArea;
                        group.Components[0][0].Value -= component[0].Value;
                        sum += Math.Abs(component[0].Value);
                    }

                    sum += group.Components[0][0].Value;
                    //normalization
                    for (c = 0; c < group.Components.Size; c++) {
                        component = group.Components[c];
                        if (!isFixed(component[0]))
                            component[0].Value = Math.Abs(component[0].Value) / sum;
                    }

                    if (((ContributedGroup)group).MemoryInt == 0)
                        ((ContributedGroup)group).MemoryInt = group.Components[0][0].Value;
                }
            }
        }

        public void normalizeInts(ISpectrum spectrum, bool intsFromSearch) {
            int c;
            double tmpSum, ifree;
            IComponent component;
            foreach (IGroup group in spectrum.Parameters) {
                if (group is ContributedGroup) {
                    //contribution
                    IParameter contribution = ((ContributedGroup)group).contribution;
                    if ((group != spectrum.Parameters[1]) && (group.Definition.kind == 1) && !contribution.HasReferenceValue) {
                        contribution.Value = contribution.Value / ((ContributedGroup)spectrum.Parameters[1]).contribution.Value;
                    }
                    //reduction
                    double sum = 0;
                    c = 0;
                    group.Components[0][0].Value = 1;
                    for (c = 1; c < group.Components.Size; c++) {
                        component = group.Components[c];
                        if (!isFixed(component[0])) {// && !(isIntInSearch(component[0]) && intsFromSearch)) {
                            component[0].Value = ((ExtComponent)component).IntInCounts / ((ContributedGroup)group).groupArea;
                        }
                        sum += Math.Abs(component[0].Value);
                        group.Components[0][0].Value -= component[0].Value;
                    }
                    sum += group.Components[0][0].Value;
                    //normalization
                    for (c = 0; c < group.Components.Size; c++) {
                        component = group.Components[c];
                        if (!isFixed(component[0]))
                            component[0].Value = Math.Abs(component[0].Value) / sum;
                    }
                } else { //prompt case
                    //normalization
                    try {
                        //if (group.Definition.kind < 4 && group.Definition.kind > 0) {
                        if (group.Definition.kind == 3) {
                            tmpSum = 0;
                            ifree = 1;
                            //double tmpSum2 = 0;
                            //foreach (IComponent component in group.Components)
                            //    tmpSum += component[0].Value;


                            tmpSum = 0;
                            //foreach (IComponent component in group.Components) {
                            for (c = 1; c < group.Components.Size; c++) {
                                //if (component == group.Components[0])
                                //    continue;
                                component = group.Components[c];
                                //tmpSum2 += component[0].Value;
                                if (isFixed(component[0])) {
                                    ifree -= component[0].Value;
                                } else {
                                    //component[0].Value = Math.Abs(component[0].Value);
                                    //if (group.Definition.kind == 3) //prompt
                                    tmpSum += component[0].Value * component[0].Value;
                                    //else
                                    //    tmpSum += component[0].Value;
                                }
                            }
                            //if (group.Definition.kind == 3) { //prompt
                            tmpSum += group.Components[0][0].Value * group.Components[0][0].Value;
                            //foreach (IComponent component in group.Components) {
                            for (c = 0; c < group.Components.Size; c++) {
                                component = group.Components[c];
                                if (!isFixed(component[0]) && !component[0].HasReferenceValue) { //.Status & ParameterStatus.Free) == ParameterStatus.Free) {
                                    component[0].Value = ifree * component[0].Value * component[0].Value / tmpSum;
                                }
                            }
                            //}
                            //} else if (intsFromSearch) {
                            //    tmpSum += 1;
                            //    tmpSum2 = 0;
                            //    for (int i = group.Components.Size - 1; i > 0; i--) {
                            //        if (!isFixed(group.Components[i][0]) && !group.Components[i][0].HasReferenceValue) {
                            //            group.Components[i][0].Value /= tmpSum;
                            //        }
                            //        tmpSum2 += group.Components[i][0].Value;
                            //    }
                            //}

                            //if (group.Components.Size > 0)
                            //    if (group.Definition.kind == 1 || group.Definition.kind == 2) {//sample or source
                            //        group.Components[0][0].Value = 1 - tmpSum2;

                            //    }
                            //if (group is ContributedGroup)
                            //    ((ContributedGroup)group).MemoryInt = group.Components[0][0].Value;

                        }
                    } catch (Exception e) {
                        if (!(e is NullReferenceException))
                            throw e;
                    }
                }
            }
        }

        #region ISpectraContainer Members

        public override double[] getTheoreticalSpectrum(ISpectrum spectrum) {
            int k, c, ltpID;
            ExtComponent exc;
            //foreach (ISpectrum s in Spectra)

            //for (k = 0; k < _spectra.Count; k++)
            //    if (_spectra[k].Parameters[4].Components[0][2].Value > _longestRange) {
            //        setLongestRange();
            //        break;
            //    }

            setShapes(spectrum, false);//, comps);  // setComps
            ////performance --- begin ---
            //long start = 0;
            //long stop = 0;
            //long freq = 0;
            //Performancer.QueryPerformanceFrequency(ref freq);
            //Performancer.QueryPerformanceCounter(ref start);
            ////performance --- begin --- 
            //setIntensities in MSB
            //try {
            if (!setQuantities(spectrum, (ParentProject.Flags & SearchFlags.IncludeInts) == SearchFlags.IncludeInts))
                throw new IndefiniteMatrixException(String.Format("SE0002: Singular matrix while calculating {0}", spectrum.Name), spectrum);//comps2, ((AnhProject)ParentProject).includeInts);  //setIntensities
            //} catch (Exception e) {
            //    throw new Exception(String.Format("setQuantities: {0}", e.Message));
            //}
            ////performance --- end ---
            //Performancer.QueryPerformanceCounter(ref stop);
            //this._TEST_sum += (stop - start) * 1.0 / freq;
            //this._TEST_counts++;
            ////Console.WriteLine("{0}. Shapes calculation time: {1:F6} s", _TEST_counts++, (stop - start) * 1.0 / freq);
            ////performance --- end ---



            int leftRange = (int)spectrum.Parameters[4].Components[0][1].Value;
            int rightRange = (int)spectrum.Parameters[4].Components[0][2].Value;
            double background = spectrum.Parameters[4].Components[0][3].Value;
            if (_theoreticalSpectrum == null)
                _theoreticalSpectrum = new double[_longestRange + 1];
            if (_theoreticalSpectrum.Length < _longestRange + 1)
                _theoreticalSpectrum = new double[_longestRange + 1];
            //for (int i = 0; i < _theoreticalSpectrum.Length; i++)
            //    _theoreticalSpectrum[i] = 0;
            bool firstAdd = true;
            ltpID = 0;
            for (c = 1; c < comps.Length; c++) {
                //ltpID = 0;
                while (ltpID < _ltCurveParams.Count && (c - 1 != ((LTCurveParams)_ltCurveParams[ltpID]).id)) ltpID++;
                if (ltpID < _ltCurveParams.Count) {
                    exc = (ExtComponent)((LTCurveParams)_ltCurveParams[ltpID]).component;
                    if (firstAdd)
                        for (k = leftRange; k <= rightRange; k++)
                            _theoreticalSpectrum[k] = comps[c][k] * exc.IntInCounts;
                    else
                        for (k = leftRange; k <= rightRange; k++)
                            _theoreticalSpectrum[k] += comps[c][k] * exc.IntInCounts;
                    firstAdd = false;
                }
            }

            for (k = leftRange; k <= rightRange; k++) {
                _theoreticalSpectrum[k] += background;
            }
            //devel.DevTools.saveSpectrum(_theoreticalSpectrum, rightRange);
            return _theoreticalSpectrum;

        }

        /// <summary>
        /// Calculates theoretical spectrum 
        /// </summary>
        /// <param name="spectrum"></param>
        /// <param name="theoreticalCurve"></param>
        /// <param name="differences"></param>
        //public override void getTheoreticalSpectrum(ISpectrum spectrum, ref double[] theoreticalCurve, ref double[] differences, bool intensitiesFromSearch) {
        public override void getTheoreticalSpectrum(ISpectrum spectrum, ref float[][] curves, ref string[] curveNames, ref float[] differences, bool intensitiesFromSearch) {

            int start = (int)(spectrum.Parameters[4].Components[0][1].Value);
            int stop = (int)(spectrum.Parameters[4].Components[0][2].Value);
            int i, j, ltpID, count, c;

            ExtComponent exc;

            this.setShapes(spectrum, true);
            count = comps.Length + 2;
            if (curves == null || curves.Length < count || curves[0].Length < spectrum.DataLength) {
                curves = new float[count][];
                curveNames = new string[count];
                differences = new float[spectrum.DataLength];
                for (i = 0; i < curves.Length; i++)
                    curves[i] = new float[spectrum.DataLength];
                //spectrum.Container.Data.CopyTo(curves[0], spectrum.BufferStartPos);
                //spectrum.ExperimentalSpectrum.CopyTo(curves[0], 0);
                curveNames[0] = AnhSpectraContainer.EXP_LITERAL;

            }
            for (i = 0; i < curves.Length; i++) {
                curveNames[i] = String.Empty;
                for (j = 0; j < curves[i].Length; j++)
                    curves[i][j] = 0;
            }

            ltpID = 0;
            for (c = 1; c < comps.Length; c++) {
                while (ltpID < _ltCurveParams.Count && (c - 1 != ((LTCurveParams)_ltCurveParams[ltpID]).id)) ltpID++;
                if (ltpID < _ltCurveParams.Count) {
                    if (((LTCurveParams)_ltCurveParams[ltpID]).component != null) {
                        exc = (ExtComponent)((LTCurveParams)_ltCurveParams[ltpID]).component;
                        if (((Components)exc.Parent).IndexOf(exc) != -1) {
                            for (i = start; i <= stop; i++) {
                                curves[c][i] += (float)(comps[c][i] * exc.IntInCounts);
                                curves[count - 1][i] += curves[c][i];
                                curves[c][i] += (float)spectrum.Parameters[4].Components[0][3].Value;
                            }
                            IGroup group = ((ExtComponents)exc.Parent).Parent;
                            curveNames[c] = String.Format("{0} {1}",
                                group.Definition.name,
                                group.Components.IndexOf(exc) + 1);
                        } else
                            ((LTCurveParams)_ltCurveParams[ltpID]).component = null; //clean up if component is disposed but not garbage collected (?)
                    }
                }
            }

            for (i = start; i < stop; i++)
                curves[count - 1][i] += (float)spectrum.Parameters[4].Components[0][3].Value;
            curveNames[count - 1] = AnhSpectraContainer.TH_LITERAL;
            for (i = start; i <= stop; i++) {
                curves[0][i] = spectrum.Container.Data[i + spectrum.BufferStartPos-1];
                differences[i - start + 1] = (float)((curves[count - 1][i] - curves[0][i]) / Math.Sqrt(spectrum.Container.Data[i + spectrum.BufferStartPos-1])); //  spectrum.Weights[i]);
            }
        }

        /// <summary>
        /// differences array for spectrum. target function for minimalization routine
        /// </summary>
        /// <param name="target">Spectrum object</param>
        /// <param name="diffs">differences array</param>
        public override bool getEvaluationArray(object target, double[] diffs) {
            //long pStart, pStop, pFreq;
            //pStart = pStop = pFreq = 0;
            //Performancer.QueryPerformanceFrequency(ref pFreq);
            //Performancer.QueryPerformanceCounter(ref pStart);
            ISpectrum spectrum = (ISpectrum)target;
            _theoreticalSpectrum = getTheoreticalSpectrum(spectrum);
            int start = (int)spectrum.Parameters[4].Components[0][1].Value;
            int stop = (int)spectrum.Parameters[4].Components[0][2].Value;
            int[] exp = spectrum.Container.Data; // .ExperimentalSpectrum;
            int i,k=0; //, j;
            //int j;
            for (i= start; i < stop; i++, k++)
                diffs[k] = (_theoreticalSpectrum[i] - (double)exp[i + spectrum.BufferStartPos-1]) / Math.Sqrt(exp[i + spectrum.BufferStartPos-1]);

            //double weight;
            //for (i = start; i < spectrum.Thresholds[0] - spectrum.BufferStartPos && i < stop; i++, k++)
            //    diffs[k] = (_theoreticalSpectrum[i] - (double)exp[i + spectrum.BufferStartPos]) / Math.Sqrt(exp[i + spectrum.BufferStartPos]);
            //for (j = 0; j < spectrum.Thresholds.Length - 1; j += 2, k++) { //ostatni element tablicy tresholds to ostatni kanał widma eksperymentalnego
            //    diffs[k] = 0;
            //    weight = 0;
            //    for (i = spectrum.Thresholds[j]; i < spectrum.Thresholds[j + 1] && i < stop + spectrum.BufferStartPos; i++) {
            //        diffs[k] += (_theoreticalSpectrum[i - spectrum.BufferStartPos] - (double)exp[i]);
            //        weight += exp[i];
            //    }
            //    if (weight != 0)
            //        diffs[k] /= Math.Sqrt(weight);
            //    else
            //        throw new SearchException("SE0001");

            //    if (i >= stop + spectrum.BufferStartPos)
            //        break;
            //}
            //Performancer.QueryPerformanceCounter(ref pStop);
            //System.Diagnostics.Debug.WriteLine((double)(pStop - pStart) / pFreq);
            //System.Diagnostics.Debug.Write(getParameterValues(spectrum));
            return true;
        }

        ///// <summary>
        ///// Debug function which prints all parameter values in particular spectrum
        ///// </summary>
        ///// <param name="spectrum">Spectrum</param>
        ///// <returns>String holding all parameter values separated with tabulator (\t) and ended with carriage return (\r\n)</returns>
        //private string getParameterValues(ISpectrum spectrum) {
        //    StringBuilder builder = new StringBuilder(spectrum.Name);
        //    int g, c, p;
        //    for (g = 1; g < spectrum.Parameters.GroupCount; g++)
        //        if ((spectrum.Parameters[g].Definition.Type & GroupType.Hidden) != GroupType.Hidden) {
        //            for (c = 0; c < spectrum.Parameters[g].Components.Size; c++)
        //                for (p = 0; p < spectrum.Parameters[g].Components[c].Size; p++)
        //                    if (spectrum.Parameters[g].Components[c][p].Definition.BindedStatus == ParameterStatus.None)
        //                        builder.AppendFormat("\t{0}", spectrum.Parameters[g].Components[c][p].Value);
        //            if (spectrum.Parameters[g] is ContributedGroup)
        //                builder.AppendFormat("\t{0}", ((ContributedGroup)spectrum.Parameters[g]).contribution.Value);
        //        }
        //    if (this._spectra.IndexOf(spectrum) == this._spectra.Count - 1 || ParentProject.SearchMode == SearchMode.Preliminary)
        //        builder.Append("\r\n");
        //    //System.Diagnostics.Debug.WriteLine("");
        //    return builder.ToString();
        //}

        /// <summary>
        /// Creates AnhSpectrum instance.
        /// </summary>
        /// <param name="spectrumNode">must not contain filepath to experimental data. Experimental spectrum must be added to xmlnode instead</param>
        /// <returns>AnhSpectrum instance</returns>
        public override ISpectrum CreateSpectrum(System.Xml.XmlReader spectrumReader, int bufferStart) {
            return new AnhSpectrum(spectrumReader, null, this, bufferStart);
        }

        public override ISpectrum CreateSpectrum(string path, int bufferStart) {
            return new AnhSpectrum(path, this, bufferStart);
        }

        #endregion
    }
}
