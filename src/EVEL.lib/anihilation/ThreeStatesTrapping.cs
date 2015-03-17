using System;
using System.Collections.Generic;
using System.Text;
using Evel.interfaces;
using Evel.share;

namespace Evel.engine.anh.stdmodels {
    public class ThreeStatesTrapping : TwoStatesTrapping {

        //private double[] conversion = new double[6];

        private void getConversionParameters(IComponent component, out double intA, out double tauA, out double intB, out double tauB, out double tauEff) {
            double lambdaA;
            double lambdaB;
            double lambdaBulk;
            double kappaA, kappaB;

            //tauA
            tauA = Math.Abs(component[1].Value);
            if (tauA > 1e-30) //taua
                lambdaA = 1 / tauA;
            else
                lambdaA = 0;

            //tauB
            tauB = Math.Abs(component[2].Value);
            if (tauB > 1e-30) //taub
                lambdaB = 1 / tauB;
            else
                lambdaB = 0;

            //tauFree
            tauEff = Math.Abs(component[5].Value);
            if (tauEff > 1e-30) //taufree
                lambdaBulk = 1 / tauEff;
            else
                lambdaBulk = 0;

            if (Math.Abs(component[3].Value) > 1e+10)
                kappaA = 1e+10;
            else
                kappaA = Math.Abs(component[3].Value);
            if (Math.Abs(component[4].Value) > 1e+10)
                kappaB = 1e+10;
            else
                kappaB = Math.Abs(component[4].Value);
            //lambdaEf = lambdaBulk + kappaA + kappaB
            double lambdaEf = lambdaBulk + kappaA + kappaB;
            if (Math.Abs(lambdaEf - lambdaA) > 1e-30)
                intA = kappaA / (lambdaEf - lambdaA);
            else
                intA = 1;

            if (Math.Abs(lambdaEf - lambdaB) > 1e-30)
                intB = kappaB / (lambdaEf - lambdaB);
            else
                intB = 1;
            //intFree = 1 - intA - intB
            //conversion[4] = 1 - conversion[0] - conversion[2];
            if (lambdaEf > 1e-30)
                tauEff = 1 / lambdaEf;
            else
                tauEff = 0;
        }

        private double GetParameterValue(IComponent component, IParameter parameter) {
            double inta, intb, taua, taub, taueff;
            getConversionParameters(component, out inta, out taua, out intb, out taub, out taueff);
            if (parameter == component[6]) {
                return inta;
            } else if (parameter == component[7]) {
                return intb;
            }
            return 0;
        }

        protected override double CalculateAverageLifetime(IComponent component, IParameter parameter) {
            if (parameter == component[0] && component.Parent is IGroup) {
                double inta, taua, intb, taub, tauEff;
                getConversionParameters(((IGroup)component.Parent).Components[0], out inta, out taua, out intb, out taub, out tauEff);
                return inta * taua + intb * taub + (1 - inta - intb) * tauEff;
            } else
                return 0.0;
        }  

        public override string DeltaFileName {
            get {
                return "threestatestrapping";
            }
        }

        public override string Description {
            get {
                return "[p text='Theoretical curve is described with lifetime of free positron and lifetime of positron in two kinds of defect. '][p text='k' font='symbol'][p text='A' index='sub'][p text=' and '][p text='k' font='symbol'][p text='B' index='sub'][p text=' parameters gives information about trapping ability of material.\n\n']";
            }
        }

        public override string Name {
            get { return "Three states trapping model"; }
        }

        //public override CalculateParameterValueHandler CalculateParameterValue {
        //    get {
        //        return GetParameterValue;
        //    }
        //}

        protected override GroupDefinition[] getGroupsDefinition() {
            if (this._groupsDefinition == null) {
                this._groupsDefinition = new GroupDefinition[5];
                //constants
                //this._groupsDefinition[0].allowMultiple = false;
                this._groupsDefinition[0].componentCount = 1;
                this._groupsDefinition[0].name = "constants";
                this._groupsDefinition[0].parameters = new ParameterDefinition[] { 
                    new ParameterDefinition("bs", ParameterStatus.Local | ParameterStatus.Fixed), 
                    new ParameterDefinition("key value", ParameterProperties.KeyValue | ParameterProperties.Unsearchable, ParameterStatus.Local | ParameterStatus.Fixed) 
                };
                this._groupsDefinition[0].Type = GroupType.Raw | GroupType.SpectrumConstants | GroupType.Hidden;
                this._groupsDefinition[0].kind = 0;
                //sample
                //this._groupsDefinition[1].allowMultiple = false;
                this._groupsDefinition[1].componentCount = 1;
                this._groupsDefinition[1].name = "sample";
                this._groupsDefinition[1].parameters = new ParameterDefinition[] { 
                    new ParameterDefinition("int", ParameterStatus.Local | ParameterStatus.Free, ParameterProperties.Hidden | ParameterProperties.ComponentIntensity),
                    new ParameterDefinition("tauA", "[p text='t' font='symbol'][p text='A' index='sub']", ParameterStatus.Common | ParameterStatus.Free, 0),//ParameterProperties.IsDependency), 
                    new ParameterDefinition("tauB", "[p text='t' font='symbol'][p text='B' index='sub']", ParameterStatus.Common | ParameterStatus.Free, 0),//ParameterProperties.IsDependency), 
                    new ParameterDefinition("kappaA", "[p text='k' font='symbol'][p text='A' index='sub']", ParameterStatus.Local | ParameterStatus.Free, 0),//ParameterProperties.IsDependency),
                    new ParameterDefinition("kappaB", "[p text='k' font='symbol'][p text='B' index='sub']", ParameterStatus.Local | ParameterStatus.Free, 0),//ParameterProperties.IsDependency),
                    new ParameterDefinition("taufree", "[p text='t' font='symbol'][p text='free' index='sub']", ParameterStatus.Common | ParameterStatus.Free, 0),//ParameterProperties.IsDependency),
                    new ParameterDefinition("intA", "[p text='int'][p text='A' index='sub']", ParameterProperties.Readonly | ParameterProperties.Unsearchable, ParameterStatus.Local | ParameterStatus.Fixed, GetParameterValue),
                    new ParameterDefinition("intB", "[p text='int'][p text='B' index='sub']", ParameterProperties.Readonly | ParameterProperties.Unsearchable, ParameterStatus.Local | ParameterStatus.Fixed, GetParameterValue),
                    new ParameterDefinition("tau", "[p text='t' font='symbol'][p text='avg' index='sub']", ParameterProperties.Readonly | ParameterProperties.Unsearchable | ParameterProperties.GroupUnique, ParameterStatus.Local | ParameterStatus.Fixed, CalculateAverageLifetime)
                };
                this._groupsDefinition[1].Type = GroupType.Contributet | GroupType.CalcContribution;
                this._groupsDefinition[1].SetDefaultComponents = new DefaultComponentsFormHandler(SetDefaultSample);
                this._groupsDefinition[1].kind = 1;
                //source
                //this._groupsDefinition[2].allowMultiple = false;
                this._groupsDefinition[2].componentCount = 0;
                this._groupsDefinition[2].name = "source";
                this._groupsDefinition[2].parameters = new ParameterDefinition[] { 
                    new ParameterDefinition("int", ParameterStatus.Common | ParameterStatus.Free, ParameterProperties.ComponentIntensity), 
                    new ParameterDefinition("tau", "[p text='t' font='symbol']", ParameterStatus.Common | ParameterStatus.Free) 
                };
                this._groupsDefinition[2].Type = GroupType.Contributet;
                this._groupsDefinition[2].kind = 2;
                this._groupsDefinition[2].StatusChanged = this.UpdateStatusesAfterChange;
                this._groupsDefinition[2].defaultSortedParameter = 1; //tau
                //prompt
                //this._groupsDefinition[3].allowMultiple = false;
                this._groupsDefinition[3].componentCount = 0;
                this._groupsDefinition[3].name = "prompt";
                this._groupsDefinition[3].parameters = new ParameterDefinition[] { 
                    new ParameterDefinition("int", ParameterStatus.Common | ParameterStatus.Free, ParameterProperties.ComponentIntensity), 
                    new ParameterDefinition("fwhm", ParameterStatus.Common | ParameterStatus.Free), 
                    new ParameterDefinition("shift", ParameterStatus.Local | ParameterStatus.Free), 
                    new ParameterDefinition("tauleft", "[p text='t' font='symbol'][p text='left' index='sub']", ParameterStatus.Local | ParameterStatus.Fixed), 
                    new ParameterDefinition("tauright", "[p text='t' font='symbol'][p text='right' index='sub']", ParameterStatus.Local | ParameterStatus.Fixed) 
                };
                this._groupsDefinition[3].Type = GroupType.Raw;
                this._groupsDefinition[3].kind = 3;
                this._groupsDefinition[3].SetDefaultComponents = new DefaultComponentsFormHandler(SetDefaultPrompt);
                this._groupsDefinition[3].StatusChanged = this.UpdateStatusesAfterChange;
                this._groupsDefinition[3].defaultSortedParameter = 0; //int
                //ranges
                //this._groupsDefinition[4].allowMultiple = false;
                this._groupsDefinition[4].componentCount = 1;
                this._groupsDefinition[4].name = "ranges";
                this._groupsDefinition[4].parameters = new ParameterDefinition[] { 
                    new ParameterDefinition("zero", ParameterProperties.Unsearchable, ParameterStatus.Local | ParameterStatus.Fixed), 
                    new ParameterDefinition("start", ParameterProperties.Unsearchable, ParameterStatus.Local | ParameterStatus.Fixed),
                    new ParameterDefinition("stop", ParameterProperties.Unsearchable, ParameterStatus.Local | ParameterStatus.Fixed), 
                    new ParameterDefinition("background", ParameterStatus.Local | ParameterStatus.Free)
                };
                this._groupsDefinition[4].Type = GroupType.Raw;
                this._groupsDefinition[4].kind = 4;
                this._groupsDefinition[4].SetDefaultComponents = new DefaultComponentsFormHandler(SetDefaultRanges);
            }
            return this._groupsDefinition;
        }

        //public virtual ValuesDictionary[] convert(IParameterSet parameters) {
        //public override void convert(ref ValuesDictionary[] dictionary, IParameterSet parameters) {
        public override void convert(List<ICurveParameters> curveParameters, IParameterSet parameters) {
            double inta, intb, taua, taub, taueff;
            inta = intb = taua = taub = taueff = 0.0;
            IGroup promptGroup = parameters[3];
            IGroup constantsGroup = parameters[0];
            IGroup rangesGroup = parameters[4];
            //if (dictionary == null) {
            //    int resultDim = 0;
            //    foreach (IGroup group in parameters) {
            //        if ((group.Definition.Type & GroupType.Contributet) == GroupType.Contributet) {
            //            if (group.Definition.kind == 1)  //sample
            //                resultDim += promptGroup.Components.Size * 3; //2 fractions in sample. sample component count is fixed
            //            else
            //                resultDim += promptGroup.Components.Size * group.Components.Size;
            //        }
            //    }
            //    //ValuesDictionary[] result = new ValuesDictionary[resultDim];
            //    dictionary = new ValuesDictionary[resultDim];
            //}
            //int resultId = 0;
            int parametersId = 0;
            int id = 0;
            int groupId = 0;
            IParameter firstShift = promptGroup.Components[0][2];
            IComponent firstPromptComponent = promptGroup.Components[0];

            int gid, pid, cid, fractionId;
            for (gid=0; gid<parameters.GroupCount; gid++) {

                if ((parameters[gid].Definition.Type & GroupType.Contributet) == GroupType.Contributet) {
                    for (pid = 0; pid<promptGroup.Components.Size; pid++) {
                        id = groupId;
                        for (cid=0; cid<parameters[gid].Components.Size; cid++) {

            //foreach (IGroup group in parameters) {
            //    if ((group.Definition.Type & GroupType.Contributet) == GroupType.Contributet) {
            //        foreach (IComponent promptComp in promptGroup.Components) {
            //            foreach (IComponent comp in group.Components) {
                            int fractionCount = (parameters[gid].Definition.kind == 1) ? 3 : 1;  //if sample
                            if (fractionCount == 3)
                                getConversionParameters(parameters[gid].Components[cid], out inta, out taua, out intb, out taub, out taueff);
                                //setSampleConversionArray(parameters[gid].Components[cid]);
                            for (fractionId = 0; fractionId < fractionCount; fractionId++) {
                                if (curveParameters.Count <= parametersId)
                                    curveParameters.Add(new LTCurveParams());
                                LTCurveParams p = (LTCurveParams)curveParameters[parametersId];
                                parametersId++;

                                p.component = parameters[gid].Components[cid];
                                p.promptComponent = promptGroup.Components[pid];

                                if (parameters[gid].Definition.kind == 2) {  //source
                                    p.fraction = 1;
                                    p.tau = p.component[1].Value;
                                    p.id = id++;
                                } else {
                                    switch (fractionId) {
                                        case 0:
                                            //p.fraction = p.component[6].Value;
                                            //p.tau = Math.Abs(p.component[1].Value);
                                            p.fraction = inta; // conversion[0];
                                            p.tau = taua; //conversion[1];
                                            p.id = id;
                                            break;
                                        case 1:
                                            //p.fraction = p.component[7].Value;
                                            //p.tau = Math.Abs(p.component[2].Value);
                                            p.fraction = intb; // conversion[2];
                                            p.tau = taub; // conversion[3];
                                            p.id = id;
                                            break;
                                        case 2:
                                            //p.fraction = 1 - p.component[6].Value - p.component[7].Value;
                                            //p.tau = 1 / (1 / Math.Abs(p.component[5].Value) + Math.Abs(p.component[3].Value) + Math.Abs(p.component[4].Value));
                                            p.fraction = 1 - inta - intb; // conversion[4];
                                            p.tau = taueff;
                                            p.id = id++;
                                            break;
                                    }
                                }
                                p.dispersion = 0;
                                p.fwhm = Math.Abs(p.promptComponent[1].Value);
                                p.tauleft = Math.Abs(p.promptComponent[3].Value);
                                p.tauright = Math.Abs(p.promptComponent[4].Value);
                                //if (promptComp == firstPromptComponent)
                                //    p.x0 = promptComp[2].Value;
                                //else
                                //    p.x0 = firstShift.Value + promptComp[2].Value;
                                if (p.promptComponent == firstPromptComponent)
                                    p.shift = 0;
                                else
                                    p.shift = p.promptComponent[2].Value;
                                p.zeroCorrection = firstShift.Value;

                                p.nstart = (int)rangesGroup.Components[0][1].Value;
                                p.nstop = (int)rangesGroup.Components[0][2].Value;
                                p.lf = false;
                                p.rt = false;
                                p.with_fi = true;
                                p.diff = true;
                                p.cs = rangesGroup.Components[0][0].Value;
                                p.bs = constantsGroup.Components[0][0].Value;
                            }
                        }
                    }
                    groupId += parameters[gid].Components.Size;
                }
            }
            //return result;
        }

    }
}