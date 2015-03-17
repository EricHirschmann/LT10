using System;
using System.Collections.Generic;
using System.Text;
using Evel.interfaces;
using Evel.share;

namespace Evel.engine.anh.stdmodels
{
    public class TestModel : Mexm
    {

        //private double[] conversion = new double[4];

        private void getConversionParameters(IComponent component, out double intD, out double tauD)
        {
                    //0 "int
                    //1 tauD
                    //2 kappaD
                    //   wyrzucone taufree z modelu dwustanowego
                    //3 intD
                    //4 tau

            double lambdaA;
            //double lambdaBulk;
            double kappa;
            //tauD
            tauD = Math.Abs(component["tauD"].Value);
            if (Math.Abs(tauD) > 1e-30)
                lambdaA = 1 / tauD;
            else
                lambdaA = 0;

            ////tauEff
            //tauEff = Math.Abs(component[3].Value);
            //if (tauEff > 1e-30)
            //    lambdaBulk = 1 / tauEff;
            //else
            //    lambdaBulk = 0;

            if (Math.Abs(component[2].Value) > 1e+10)
                kappa = 1e+10;
            else
                kappa = Math.Abs(component[2].Value);
            //lamdaEf = lambdaBulk + kappaD
            //double lambdaEf = lambdaBulk + kappa;

            double sum = 0;
            IComponents components = component.Parent as IComponents;
            for (int i = 0; i < components.Size; i++)
            {
                sum += components[i]["kappaD"].Value;
            }

            if (Math.Abs(kappa + 100) > 1e-30)
                intD = kappa / sum;
            else
                intD = 1;

            //if (lambdaEf > 1e-30)
            //    tauEff = 1 / lambdaEf;
            //else
            //    tauEff = 0;
        }

        private double GetParameterValue(IComponent component, IParameter parameter)
        {
            if (parameter == component["intD"]) // zmiana indeksu bo wyrzucilem taufree
            {
                double intD, tauD;//, tauEff;
                intD = tauD = 0.0;

                getConversionParameters(component, out intD, out tauD/*, out tauEff*/);
                return intD;
            }
            else
                return 0;
        }

        protected override double CalculateAverageLifetime(IComponent component, IParameter parameter)
        {
            if (parameter == component[0] && component.Parent is IGroup)
            {
                double sum = 0.0;
                double intD, tauD;

                for (int i = 0; i < ((IGroup)component.Parent).Components.Size; i++)
                {
                    getConversionParameters(((IGroup)component.Parent).Components[i], out intD, out tauD);
                    sum += intD * tauD;
                }
                return sum;
            }
            else
                return 0.0;
        }

        public override string DeltaFileName
        {
            get
            {
                return "twostatestrapping";
            }
        }

        public override string Description
        {
            get
            {
                return "[p text='Theoretical curve is described with lifetime of free positron and lifetime of positron in defect. '][p text='k' font='symbol'][p text=' parameter gives information about trapping ability of material.\n\n']";
            }
        }

        public override string Name
        {
            get { return "Pseudo trapping"; }
        }

        //public virtual CalculateParameterValueHandler CalculateParameterValue {
        //    get {
        //        return GetParameterValue;
        //    }
        //}

        protected override GroupDefinition[] getGroupsDefinition()
        {
            if (this._groupsDefinition == null)
            {
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
                this._groupsDefinition[1].componentCount = 0;
                this._groupsDefinition[1].name = "sample";
                this._groupsDefinition[1].parameters = new ParameterDefinition[] { 
                    new ParameterDefinition("int", ParameterStatus.Local | ParameterStatus.Free, ParameterProperties.Hidden | ParameterProperties.ComponentIntensity),
                    new ParameterDefinition("tauD", "[p text='t' font='symbol']", ParameterStatus.Common | ParameterStatus.Free, 0),//ParameterProperties.IsDependency), 
                    new ParameterDefinition("kappaD", "[p text='k' font='symbol']", ParameterStatus.Local | ParameterStatus.Fixed, 0),//ParameterProperties.IsDependency),
                    //new ParameterDefinition("taufree", "[p text='t' font='symbol'][p text='free' index='sub']", ParameterStatus.Common | ParameterStatus.Free, 0),//ParameterProperties.IsDependency),
                    new ParameterDefinition("intD", "[p text='int']", ParameterProperties.Readonly | ParameterProperties.Unsearchable, ParameterStatus.Local | ParameterStatus.Fixed, GetParameterValue),
                    new ParameterDefinition("tau", "[p text='t' font='symbol'][p text='avg' index='sub']", ParameterProperties.Readonly | ParameterProperties.Unsearchable | ParameterProperties.GroupUnique, ParameterStatus.Local | ParameterStatus.Fixed, CalculateAverageLifetime)
                };
                this._groupsDefinition[1].Type = GroupType.Contributet | GroupType.CalcContribution;
                this._groupsDefinition[1].SetDefaultComponents = new DefaultComponentsFormHandler(SetDefaultSample);
                this._groupsDefinition[1].kind = 1;
                //source
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

        protected virtual void SetDefaultSample(IGroup sender, ISpectrum spectrum, EventArgs args)
        {
            sender.Components.Size = 1;
            sender.Components[0]["kappaD"].Value = 100;
        }

        //public override void convert(ref ValuesDictionary[] dictionary, IParameterSet parameters) {
        public override void convert(List<ICurveParameters> curveParameters, IParameterSet parameters)
        {
            IGroup promptGroup = parameters[3];
            IGroup constantsGroup = parameters[0];
            IGroup rangesGroup = parameters[4];
            double intD, tauD, tauEff;
            intD = tauD = tauEff = 0.0;
            //int resultId = 0;
            int parametersId = 0;
            int id = 0;
            int groupId = 0;
            IParameter firstShift = promptGroup.Components[0][2];
            IComponent firstPromptComponent = promptGroup.Components[0];
            //foreach (IGroup group in parameters) {
            int gid, pid, cid, fractionId;
            for (gid = 0; gid < parameters.GroupCount; gid++)
            {

                if ((parameters[gid].Definition.Type & GroupType.Contributet) == GroupType.Contributet)
                {
                    for (pid = 0; pid < promptGroup.Components.Size; pid++)
                    {
                        id = groupId;
                        for (cid = 0; cid < parameters[gid].Components.Size; cid++)
                        {
                            int fractionCount = (parameters[gid].Definition.kind == 1) ? 2 : 1;  //if sample
                            if (fractionCount == 2) //if sample
                                getConversionParameters(parameters[gid].Components[cid], out intD, out tauD);
                            for (fractionId = 0; fractionId < fractionCount; fractionId++)
                            {
                                if (curveParameters.Count <= parametersId)
                                    curveParameters.Add(new LTCurveParams());
                                LTCurveParams p = (LTCurveParams)curveParameters[parametersId];
                                parametersId++;

                                p.component = parameters[gid].Components[cid];
                                p.promptComponent = promptGroup.Components[pid];

                                if (parameters[gid].Definition.kind == 2)
                                {  //source
                                    p.fraction = 1;
                                    p.tau = p.component[1].Value;
                                    p.id = id++;
                                }
                                else
                                {
                                    switch (fractionId)
                                    {
                                        case 0:
                                            p.fraction = intD;
                                            p.tau = tauD;
                                            p.id = id; //nie ma ++ bo druga frakcja ma to samo id
                                            break;
                                        case 1:
                                            p.fraction = 1 - intD;
                                            p.tau = tauEff;
                                            p.id = id++;
                                            break;
                                    }
                                }
                                p.dispersion = 0;
                                p.fwhm = Math.Abs(p.promptComponent[1].Value);
                                p.tauleft = p.promptComponent[3].Value;
                                p.tauright = p.promptComponent[4].Value;
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
