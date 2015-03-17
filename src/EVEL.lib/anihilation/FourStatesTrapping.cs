using System;
using System.Collections.Generic;
using System.Text;
using Evel.interfaces;
using Evel.share;

namespace Evel.engine.anh.stdmodels {

    struct DefectParameters {
        public double t;
        public double i;
    }

    public class FourStatesTrapping : TwoStatesTrapping {

        //private double[] conversion = new double[6];

        private void getConversionParameters(IComponent component, out DefectParameters dfA, out DefectParameters dfB, out DefectParameters dfC, out DefectParameters free) {
            double lambdaA;
            double lambdaB;
            double lambdaC;
            double lambdaBulk;
            double kappaA, kappaB, kappaC;

            //tauA
            dfA.t = Math.Abs(component[1].Value);
            if (dfA.t > 1e-30) //taua
                lambdaA = 1 / dfA.t;
            else
                lambdaA = 0;

            //tauB
            dfB.t = Math.Abs(component[2].Value);
            if (dfB.t > 1e-30) //taua
                lambdaB = 1 / dfB.t;
            else
                lambdaB = 0;

            //tauC
            dfC.t = Math.Abs(component[3].Value);
            if (dfC.t > 1e-30) //taua
                lambdaC = 1 / dfC.t;
            else
                lambdaC = 0;

            //tauFree
            free.t = Math.Abs(component[7].Value);
            if (free.t > 1e-30) //taua
                lambdaBulk = 1 / free.t;
            else
                lambdaBulk = 0;

            //kappaA
            if (Math.Abs(component[4].Value) > 1e+10)
                kappaA = 1e+10;
            else
                kappaA = Math.Abs(component[4].Value);
            //kappaB
            if (Math.Abs(component[5].Value) > 1e+10)
                kappaB = 1e+10;
            else
                kappaB = Math.Abs(component[5].Value);
            //kappaC
            if (Math.Abs(component[6].Value) > 1e+10)
                kappaC = 1e+10;
            else
                kappaC = Math.Abs(component[6].Value);
            //lambdaEf = lambdaBulk + kappaA + kappaB
            double lambdaEf = lambdaBulk + kappaA + kappaB + kappaC;
            //intA
            if (Math.Abs(lambdaEf - lambdaA) > 1e-30)
                dfA.i = kappaA / (lambdaEf - lambdaA);
            else
                dfA.i = 1;
            //intB
            if (Math.Abs(lambdaEf - lambdaB) > 1e-30)
                dfB.i = kappaB / (lambdaEf - lambdaB);
            else
                dfB.i = 1;
            //intC
            if (Math.Abs(lambdaEf - lambdaC) > 1e-30)
                dfC.i = kappaC / (lambdaEf - lambdaC);
            else
                dfC.i = 1;
            
            //intFree = 1 - intA - intB
            //conversion[4] = 1 - conversion[0] - conversion[2];
            if (lambdaEf > 1e-30)
                free.t = 1 / lambdaEf;
            else
                free.t = 0;
            free.i = 1 - dfA.i - dfB.i - dfC.i;
        }

        private double GetParameterValue(IComponent component, IParameter parameter) {
            //double inta, intb, taua, taub, taueff;
            DefectParameters dfA, dfB, dfC, dfFree;
            getConversionParameters(component, out dfA, out dfB, out dfC, out dfFree);
            if (parameter == component[8]) {
                return dfA.i;
            } else if (parameter == component[9]) {
                return dfB.i;
            } else if (parameter == component[10]) {
                return dfC.i;
            }
            return 0;
        }

        protected override double CalculateAverageLifetime(IComponent component, IParameter parameter) {
            if (parameter == component[0] && component.Parent is IGroup) {
                DefectParameters dfA, dfB, dfC, dfFree;
                getConversionParameters(((IGroup)component.Parent).Components[0], out dfA, out dfB, out dfC, out dfFree);
                return dfA.i * dfA.t + dfB.i * dfB.t + dfC.i * dfC.t + dfFree.i * dfFree.t;
            } else
                return 0.0;
        }  

        public override string DeltaFileName {
            get {
                return "fourstatestrapping";
            }
        }

        public override string Description {
            get {
                return "[p text='Theoretical curve is described with lifetime of free positron and lifetime of positron in three kinds of defect. '][p text='k' font='symbol'][p text='A' index='sub'][p text=', '][p text='k' font='symbol'][p text='B' index='sub'][p text=' and '][p text='k' font='symbol'][p text='C' index='sub'][p text=' parameters gives information about trapping ability of material.\n\n']";
            }
        }

        public override string Name {
            get { return "Four states trapping model"; }
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
                    new ParameterDefinition("tauC", "[p text='t' font='symbol'][p text='C' index='sub']", ParameterStatus.Common | ParameterStatus.Free, 0),//ParameterProperties.IsDependency), 
                    new ParameterDefinition("kappaA", "[p text='k' font='symbol'][p text='A' index='sub']", ParameterStatus.Local | ParameterStatus.Free, 0),//ParameterProperties.IsDependency),
                    new ParameterDefinition("kappaB", "[p text='k' font='symbol'][p text='B' index='sub']", ParameterStatus.Local | ParameterStatus.Free, 0),//ParameterProperties.IsDependency),
                    new ParameterDefinition("kappaC", "[p text='k' font='symbol'][p text='C' index='sub']", ParameterStatus.Local | ParameterStatus.Free, 0),//ParameterProperties.IsDependency),
                    new ParameterDefinition("taufree", "[p text='t' font='symbol'][p text='free' index='sub']", ParameterStatus.Common | ParameterStatus.Free, 0),//ParameterProperties.IsDependency),
                    new ParameterDefinition("intA", "[p text='int'][p text='A' index='sub']", ParameterProperties.Readonly | ParameterProperties.Unsearchable, ParameterStatus.Local | ParameterStatus.Fixed, GetParameterValue),
                    new ParameterDefinition("intB", "[p text='int'][p text='B' index='sub']", ParameterProperties.Readonly | ParameterProperties.Unsearchable, ParameterStatus.Local | ParameterStatus.Fixed, GetParameterValue),
                    new ParameterDefinition("intC", "[p text='int'][p text='C' index='sub']", ParameterProperties.Readonly | ParameterProperties.Unsearchable, ParameterStatus.Local | ParameterStatus.Fixed, GetParameterValue),
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
            //double inta, intb, taua, taub, taueff;
            //inta = intb = taua = taub = taueff = 0.0;
            DefectParameters dfA, dfB, dfC, dfFree;
            dfA.i = 1;
            dfA.t = 0;
            dfB = dfC = dfFree = dfA;
            IGroup promptGroup = parameters[3];
            IGroup constantsGroup = parameters[0];
            IGroup rangesGroup = parameters[4];
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

                            int fractionCount = (parameters[gid].Definition.kind == 1) ? 4 : 1;  //if sample
                            if (fractionCount == 4 && pid == 0)
                                getConversionParameters(parameters[gid].Components[cid], out dfA, out dfB, out dfC, out dfFree);
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
                                            p.fraction = dfA.i; // conversion[0];
                                            p.tau = dfA.t; //conversion[1];
                                            p.id = id;
                                            break;
                                        case 1:
                                            //p.fraction = p.component[7].Value;
                                            //p.tau = Math.Abs(p.component[2].Value);
                                            p.fraction = dfB.i; // conversion[2];
                                            p.tau = dfB.t; // conversion[3];
                                            p.id = id;
                                            break;
                                        case 2:
                                            //p.fraction = 1 - p.component[6].Value - p.component[7].Value;
                                            //p.tau = 1 / (1 / Math.Abs(p.component[5].Value) + Math.Abs(p.component[3].Value) + Math.Abs(p.component[4].Value));
                                            p.fraction = dfC.i; // conversion[4];
                                            p.tau = dfC.t;
                                            p.id = id;
                                            break;
                                        case 3:
                                            //p.fraction = 1 - p.component[6].Value - p.component[7].Value;
                                            //p.tau = 1 / (1 / Math.Abs(p.component[5].Value) + Math.Abs(p.component[3].Value) + Math.Abs(p.component[4].Value));
                                            p.fraction = dfFree.i; // conversion[4];
                                            p.tau = dfFree.t;
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