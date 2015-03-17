using System;
using System.Collections.Generic;
using System.Text;
using Evel.interfaces;
using Evel.share;
using System.Collections;
using Evel.engine.algorythms;

namespace Evel.engine.anh {
    public partial class AnhSpectraContainer : SpectraContainerBase {

        ContributedGroup sampleGroup;
        //IGroup zeroGroup;
        //IGroup[] innerGroups;
        //IGroup unpackedComps;
        ContributedGroup sourceGroup;
        IGroup rangesGroup;
        ISpectrum activeSpectrum = null;
        

        //private double[][] ug;
        private double[] ug, us;
        private double pg, ps;
        //private double sampleIntSearchSum, sourceIntSearchSum; //free intensities calculated in search
        //private double[] pg, us;
        //private double ps;
        private double[] beta;
        private double[] sourceAdd;
        private double[][] agi;
        //private double[] _weights;

        //private bool isFixed(ParameterStatus status) {
        //    return (status & ParameterStatus.Fixed) == ParameterStatus.Fixed;
        //}

        private EquationRow[] _equationRows = null;
        private double[] _equationsResult = null;

        private bool isIntInSearch(IParameter parameter) {
            if (isFixed(parameter)) return false;
            bool projectsIncludeInts = (ParentProject.Flags & SearchFlags.IncludeInts) == SearchFlags.IncludeInts;
            if (parameter.Parent is IComponent) {
                Components comps = (Components)(parameter.Parent as Component).Parent;
                if (parameter.Parent == comps[0] && parameter == comps[0][0]) //if intensity from first component
                    return false;
            }
            bool inSearch = false;
                if (ParentProject.SearchMode == SearchMode.Main)
                    inSearch = (projectsIncludeInts && isLocalFree(parameter)) || isCommonFree(parameter);
                else //preliminary
                    inSearch = projectsIncludeInts;
            
            return inSearch;
        }

        private bool isFixed(IParameter parameter) {
            //if (parameter.Definition.Name != "int")
                return (parameter.Status & ParameterStatus.Fixed) == ParameterStatus.Fixed;
            //else
            //    return ((parameter.Status & ParameterStatus.Fixed) == ParameterStatus.Fixed)
            //        || (//((ParentProject.Flags & SearchFlags.IncludeInts) == SearchFlags.IncludeInts)
            //            ParentProject.SearchMode == SearchMode.Main 
            //            && (parameter.Status & ParameterStatus.Common) == ParameterStatus.Common);
        }

        private bool isLocalFree(IParameter parameter) {
            return (parameter.Status & (ParameterStatus.Local| ParameterStatus.Free)) == (ParameterStatus.Local | ParameterStatus.Free);
        }

        private bool isCommonFree(IParameter parameter) {
            return (parameter.Status & (ParameterStatus.Common | ParameterStatus.Free)) == (ParameterStatus.Common | ParameterStatus.Free);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="sourceGroup"></param>
        /// <returns></returns>
        private bool specialSourceCondition(ContributedGroup sourceGroup) {
            if (sourceGroup == null)
                return false;
            else
                return isFixed(sourceGroup.contribution) ||
                //    (((sourceGroup.contribution.Status & ParameterStatus.Free) > 0) &&
                //        (((sourceGroup.contribution.Status & ParameterStatus.Common) > 0 && ParentProject.SearchMode != SearchMode.Preliminary) ||
                //        ParentProject.SearchMode == SearchMode.PreliminaryInts));
                (((sourceGroup.contribution.Status & (ParameterStatus.Common | ParameterStatus.Free)) == (ParameterStatus.Common | ParameterStatus.Free)) &&
                ParentProject.SearchMode != SearchMode.Preliminary);
        }

        private void  getUP(ISpectrum spectrum, IGroup group, ref double[] u, ref double p,
            bool includedInts, ref int compsPosition) { //, Dictionary<IComponent, double[]> comps) {
            int leftRange = (int)spectrum.Parameters[4].Components[0][1].Value;
            int rightRange = (int)spectrum.Parameters[4].Components[0][2].Value;
            if (u == null)
                u = new double[_longestRange + 1];
            else {
                if (u.Length < _longestRange + 1)
                    u = new double[_longestRange + 1];
                else {
                    for (int i = 0; i < u.Length; i++) u[i] = 0;
                }
            }
            ///------------------nowa wersja----------------------   
            ExtComponent component;
            int c, ch,pos = compsPosition+1;
            double xbis;
            double searchSum = 1; // ((ContributedGroup)group).MemoryInt;
            //searchSum *= searchSum;
            //searchSum = 1;
            double freeRest;

            p = 1;
            //fixed intensities. xbis sum is calculated as well
            for (c = 1; c < group.Components.Size; c++) {
                component = (ExtComponent)group.Components[c];
                
                if (isFixed(component[0])) {
                    p -= component[0].Value;
                    for (ch = leftRange; ch < rightRange; ch++) u[ch] += component[0].Value * comps[pos][ch];
                } else if (isIntInSearch(component[0])) {
                    searchSum += component[0].Value * component[0].Value;
                }
                pos++;
            }
            freeRest = p;
            //intensities from search (xbis)
            pos = compsPosition+1;
            for (c = 1; c < group.Components.Size; c++) {
                component = (ExtComponent)group.Components[c];
                if (isIntInSearch(component[0])) {
                    xbis = freeRest * component[0].Value * component[0].Value / searchSum;
                    p -= xbis;
                    for (ch = leftRange; ch < rightRange; ch++) u[ch] += xbis * comps[pos][ch];
                }
                pos++;
            }
            compsPosition += group.Components.Size;

            ///----------------------stara wersja----------------------------
            //searchSum = ((ContributedGroup)group).MemoryInt;
            //if (searchSum == 0) searchSum = 1;
            //double fixedSum = 0;
            ////if (includedInts) {
            //foreach (IComponent component in group.Components) {
            //    //if (component == group.Components[0] || !isIntInSearch(component[0]))
            //    //    continue;
            //    if (isFixed(component[0]))
            //        fixedSum += component[0].Value;
            //    else if (component != group.Components[0] && isIntInSearch(component[0]) && !isFixed(component[0]))
            //        //if (!isFixed(component[0]))
            //        searchSum += component[0].Value;
            //}
            ////}
            //foreach (IComponent component in group.Components) {
            //    if (component == group.Components[0])
            //        continue;

            //    if (isFixed(component[0])) { // || includedInts
            //        for (int k = leftRange; k <= rightRange; k++) {
            //            u[k] += comps[component][k] * component[0].Value;
            //        }
            //        p += component[0].Value;
            //    } else {
            //        //if (includedInts || isCommonFree(component[0])) {
            //        if (isIntInSearch(component[0])) {
            //            double tmpInt = (1 - fixedSum) * component[0].Value / searchSum;
            //            for (int k = leftRange; k <= rightRange; k++) {
            //                u[k] += comps[component][k] * tmpInt;
            //            }
            //            p += tmpInt;
            //        }
            //    }
            //}
            //p = 1 - p;
        }

        private void ugPgUsPs(ISpectrum spectrum, bool includedInts) {
            int compsPosition = 1;
            getUP(spectrum, sampleGroup, ref ug, ref pg, includedInts, ref compsPosition);
            if (sourceGroup != null)
                getUP(spectrum, sourceGroup, ref us, ref ps, includedInts, ref compsPosition);
            else
                ps = 0;
        }

        private void getGroups(ISpectrum spectrum) {//, out IGroup zeroGroup, 
            sourceGroup = null;
            rangesGroup = null;
            sampleGroup = null;
            for (int i=0; i<spectrum.Parameters.GroupCount; i++)
                switch (spectrum.Parameters[i].Definition.kind) {
                    case 1: sampleGroup = (ContributedGroup)spectrum.Parameters[i]; break;
                    case 2:
                        if (spectrum.Parameters[i].Components.Size > 0)
                            sourceGroup = (ContributedGroup)spectrum.Parameters[i];
                        break;
                    case 4: rangesGroup = spectrum.Parameters[i]; break;
                }

            //foreach (IGroup group in spectrum.Parameters) {
            //    GroupType gt = group.Definition.Type;
            //    if (group.Definition.kind == 1) {
            //        sampleGroup = (ContributedGroup)group;
            //    } else {
            //        if (group.Definition.kind == 2 && group.Components.Size > 0)  //source
            //            sourceGroup = (ContributedGroup)group;
            //        else {
            //            if (group.Definition.kind == 4)  //ranges
            //                rangesGroup = group;
            //        }

            //    }
            //}
        }

        private void setAgiSize(bool includedInts) {
            //setting maximal possible size for agi, nomatter if something is fixed or not. Some spectra may have different agi size!
            int agisize = 0;
            int i;
            //sample group
            for (i = 0; i < sampleGroup.Components.Size; i++)
                //if ((!isIntInSearch(sampleGroup.Components[i][0]) && !isFixed(sampleGroup.Components[i][0])) || i == 0) 
                    agisize++;
            //source
            if (sourceGroup != null) {
                bool sourceCondition = specialSourceCondition((ContributedGroup)sourceGroup);
                for (i = 0; i < sourceGroup.Components.Size; i++) {
                    //if (sourceCondition && i == 0)
                    //    continue;  //if sourceCondition is met, don't include first component
                    //if ((!isIntInSearch(sourceGroup.Components[i][0]) && !isFixed(sourceGroup.Components[i][0])) || i == 0)
                        agisize++;
                }
            }
            //background
            //if (!isFixed(rangesGroup.Components[0][3]) && !isIntInSearch(rangesGroup.Components[0][3]))
                agisize++;
            agi = new double[agisize][];
            for (i = 0; i < agi.Length; i++)
                agi[i] = new double[_longestRange + 1];   

            //backup may 12, 2011
            //int agisize = 0;
            //int i;
            ////sample group
            //for (i=0; i<sampleGroup.Components.Size; i++)
            //    if ((!isIntInSearch(sampleGroup.Components[i][0]) && !isFixed(sampleGroup.Components[i][0])) || i==0) // (comp == sampleGroup.Components[0]))
            //        agisize++;
            ////source
            //if (sourceGroup != null) {
            //    bool sourceCondition = specialSourceCondition((ContributedGroup)sourceGroup);
            //    for (i=0; i<sourceGroup.Components.Size; i++) {
            //        if (sourceCondition && i==0)
            //            continue;  //if sourceCondition is met, don't include first component
            //        if ((!isIntInSearch(sourceGroup.Components[i][0]) && !isFixed(sourceGroup.Components[i][0])) || i==0)// (component == sourceGroup.Components[0]))
            //            agisize++;
            //    }
            //}
            ////background
            //if (!isFixed(rangesGroup.Components[0][3]) && !isIntInSearch(rangesGroup.Components[0][3]))
            //    agisize++;
            //agi = new double[agisize][];
            //for (i = 0; i < agi.Length; i++)
            //    agi[i] = new double[_longestRange + 1];     
        }

        //backup september 12, 2010
        //private void setAgi(ISpectrum spectrum, bool includedInts) {
        //    if (agi == null) {
        //        setAgiSize(includedInts);
        //    } else {
        //        if (agi[0].Length < _longestRange) {
        //            for (int i = 0; i < agi.Length; i++)
        //                agi[i] = new double[_longestRange + 1];
        //        }
        //    }
        //    int leftRange = (int)spectrum.Parameters[4].Components[0][1].Value;
        //    int rightRange = (int)spectrum.Parameters[4].Components[0][2].Value;
        //    IParameter sourceContrib = null;
        //    bool sourceCondition = false;
        //    double alfa = 0;
        //    double[] firstSourceComp = null;
        //    if (sourceGroup!= null) {
        //        sourceContrib = ((ContributedGroup)sourceGroup).contribution;
        //        alfa = 1/(1/sourceContrib.Value - 1);
        //        firstSourceComp = comps[sourceGroup.Components[0]];
        //        sourceCondition = specialSourceCondition((ContributedGroup)sourceGroup);
        //    }
        //    int agiIndex = 0;
        //     // = comps[sourceGroup.Components[0]];
        //    //sample group
        //    foreach (IComponent comp in sampleGroup.Components) {
        //        if ((!includedInts && !isFixed(comp[0].Status)) || (comp == sampleGroup.Components[0])) {
        //            for (int k = leftRange; k <= rightRange; k++) {
        //                agi[agiIndex][k] = pg * comps[comp][k] + ug[k];
        //                if (sourceCondition)
        //                    agi[agiIndex][k] += alfa * (ps * firstSourceComp[k] + us[k]);                       
        //            }
        //            agiIndex++;
        //        }
        //    }
        //    //source group
        //    if (sourceGroup != null) {
        //        foreach (IComponent component in sourceGroup.Components) {
        //            if (component == sourceGroup.Components[0] && sourceCondition)
        //                continue;  //if sourceCondition or intensities are included to search, don't include last component
        //            if ((!includedInts && !isFixed(component[0].Status)) || (component == sourceGroup.Components[0])) {
        //                for (int k = leftRange; k <= rightRange; k++) {
        //                    if (sourceCondition) {
        //                        agi[agiIndex][k] = ps * (comps[component][k] - firstSourceComp[k]);
        //                    }  else
        //                        agi[agiIndex][k] = ps * comps[component][k] + us[k];
        //                }
        //                agiIndex++;
        //            }
        //        }
        //    }
        //    //background
        //    if (!isFixed(rangesGroup.Components[0][3].Status) && !includedInts) {
        //        for (int k=leftRange; k<=rightRange; k++)
        //            agi[agiIndex][k] = 1;
        //    }
        //}

        private int setAgi(ISpectrum spectrum, bool includedInts) {
            //TODO : w tym miejscu możliwe nieprzechwycone wyjątki spowodowane zbyt małym rozmiarem macierzy agi!
            //prawdopodobnie konieczny jest warunek sprawdzający rozmiar pierwszego wymiaru macierzy.
            if (agi == null) {
                setAgiSize(includedInts);
            } else {
                if (agi[0].Length < _longestRange) {
                    for (int i = 0; i < agi.Length; i++)
                        agi[i] = new double[_longestRange + 1];
                }
            }
            int leftRange = (int)spectrum.Parameters[4].Components[0][1].Value;
            int rightRange = (int)spectrum.Parameters[4].Components[0][2].Value;
            int c,k;
            //IParameter sourceContrib = null;
            bool sourceCondition = false;
            double alfa = 0;
            double[] firstSourceComp = null;
            double[] shape;
            IComponent component;
            if (sourceCondition = specialSourceCondition(sourceGroup)) {
                double scontrib; // = Math.Abs(sourceGroup.contribution.Value);
                if (!isFixed(sourceGroup.contribution))
                    scontrib = Math.Abs(sourceGroup.contribution.Value) / (1 + Math.Abs(sourceGroup.contribution.Value));
                else
                    scontrib = Math.Abs(sourceGroup.contribution.Value);

                alfa = 1 / (1 / scontrib - 1);
                //alfa = 1 / Math.Abs(1 - sourceContrib.Value); //contrib test
                if (double.IsInfinity(alfa))
                    alfa = 1e20;
                firstSourceComp = comps[sampleGroup.Components.Size + 1];
            }
            int agiIndex = 0;
            // = comps[sourceGroup.Components[0]];
            //sample group
            for (c = 0; c < sampleGroup.Components.Size; c++) {
                component = sampleGroup.Components[c];
                //shape = comps[component];
                shape = comps[c + 1];
                if ((!isIntInSearch(component[0]) && !isFixed(component[0])) || c == 0) {
                    for (k = leftRange; k <= rightRange; k++) {
                        agi[agiIndex][k] = pg * shape[k] + ug[k];
                        if (sourceCondition)
                            agi[agiIndex][k] += alfa * (ps * firstSourceComp[k] + us[k]);
                    }
                    agiIndex++;
                }
            }

            //source group
            if (sourceGroup != null) {
                //foreach (IComponent component in sourceGroup.Components) {
                for (c=0; c<sourceGroup.Components.Size; c++) {
                    if (c == 0 && sourceCondition)
                        continue;  //if sourceCondition or intensities are included to search, don't include last component
                    component = sourceGroup.Components[c];
                    //shape = comps[component];
                    shape = comps[sampleGroup.Components.Size + 1 + c];
                    if ((!isIntInSearch(component[0]) && !isFixed(component[0])) || (c==0)) {
                        if (sourceCondition)
                            for (k = leftRange; k <= rightRange; k++)
                                agi[agiIndex][k] = (shape[k] - ps * firstSourceComp[k]); //TODO : roznica z poprzednimi obliczeniami!!! - ps mnozylo roznice
                        else
                            for (k = leftRange; k <= rightRange; k++)
                                agi[agiIndex][k] = ps * shape[k] + us[k];
                        
                        agiIndex++;
                    }
                }
            }
            //background
            //if (!isFixed(rangesGroup.Components[0][3]) && !includedInts) {
            if (!isFixed(rangesGroup.Components[0][3]) && !isIntInSearch(rangesGroup.Components[0][3])) {
                for (k = leftRange; k <= rightRange; k++)
                    agi[agiIndex][k] = 1;
                agiIndex++;
            }
            return agiIndex;
        }
        
        private void getMainMatrix(ISpectrum spectrum, bool includedInts, out EquationRow[] equationRows, out double[] equationsResult, out int size) {

            int agiSize = setAgi(spectrum, includedInts);

            int leftRange = (int)spectrum.Parameters[4].Components[0][1].Value;
            int rightRange = (int)spectrum.Parameters[4].Components[0][2].Value;
            int row, col, k;
            //EquationRow[] result = new EquationRow[agiSize];
            if (_equationRows == null || _equationRows.Length < agiSize)
                _equationRows = new EquationRow[agiSize];
            if (_equationsResult == null || _equationsResult.Length < agiSize)
                _equationsResult = new double[agiSize];
            //if (this._weights == null)
            //    this._weights = new double[spectrum.ExperimentalSpectrum.Length];
            //else {
            //    if (this._weights.Length<spectrum.ExperimentalSpectrum.Length)
            //        this._weights = new double[spectrum.ExperimentalSpectrum.Length];
            //}

            //for (int i = 0; i < spectrum.ExperimentalSpectrum.Length; i++)
            //    _weights[i] = 1 / Math.Sqrt(spectrum.ExperimentalSpectrum[i]);
            double backgroundSub = 0;
            double e;
            if (isFixed(spectrum.Parameters[4].Components[0][3]) || isIntInSearch(spectrum.Parameters[4].Components[0][3]))
              backgroundSub = spectrum.Parameters[4].Components[0][3].Value;
            ////performance --- begin ---
            //long start = 0;
            //long stop = 0;
            //long freq = 0;
            //Performancer.QueryPerformanceFrequency(ref freq);
            //Performancer.QueryPerformanceCounter(ref start);
            ////performance --- begin ---
            for (row = 0; row < agiSize; row++) {
                if (_equationRows[row] == null)
                    _equationRows[row] = new EquationRow(new double[agiSize], 0);
                
                for (col = 0; col < agiSize; col++) {
                    _equationRows[row].coeff[col] = 0;
                    for (k = leftRange; k <= rightRange; k++) //{
                        //_equationRows[row].coeff[col] += agi[row][k] * agi[col][k] / spectrum.ExperimentalSpectrum[k]; // *spectrum.Weights[k]; // _weights[k];
                        _equationRows[row].coeff[col] += agi[row][k] * agi[col][k] / _data[k+spectrum.BufferStartPos-1];
                    //}
                }


                //right side
                _equationRows[row].b = 0;
                
                for (k = leftRange; k <= rightRange; k++) {
                    e = _data[k + spectrum.BufferStartPos-1] - backgroundSub;
                    _equationRows[row].b += e * agi[row][k] / _data[k + spectrum.BufferStartPos-1]; // spectrum.Weights[k]; // _weights[k];

                }

            }
            ////performance --- end ---
            //Performancer.QueryPerformanceCounter(ref stop);
            //this._TEST_sum += (stop - start) * 1.0 / freq;
            //this._TEST_counts++;
            ////Console.WriteLine("{0}. Shapes calculation time: {1:F6} s", _TEST_counts++, (stop - start) * 1.0 / freq);
            ////performance --- end ---
            //return _equationRows;
            equationRows = _equationRows;
            equationsResult = _equationsResult;
            size = agiSize;
        }

    }
}