using Evel.interfaces;
using Evel.share;
using System;
using System.Collections.Generic;

namespace Evel.engine.anh.stdmodels {
    public class Wmexm : Mexm {

        public override string DeltaFileName {
            get {
                return "wmexm";
            }
        }

        public override string Description {
            get {
                return "[p text='Wided multiexponential model\n\n']";
            }
        }

        public override string Name {
            get { return "Wided multiexponential"; }
        }

        protected override GroupDefinition[] getGroupsDefinition() {
            if (this._groupsDefinition == null) {
                this._groupsDefinition = new GroupDefinition[5];
                //constants
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
                this._groupsDefinition[1].componentCount = 0;
                this._groupsDefinition[1].name = "sample";
                this._groupsDefinition[1].parameters = new ParameterDefinition[] { 
                    new ParameterDefinition("int", ParameterStatus.Local | ParameterStatus.Free, ParameterProperties.ComponentIntensity),
                    new ParameterDefinition("tau", "[p text='t' font='symbol']", ParameterStatus.Local | ParameterStatus.Free), 
                    new ParameterDefinition("sigma", "[p text='s' font='symbol']", ParameterStatus.Local | ParameterStatus.Free) 
                };
                this._groupsDefinition[1].Type = GroupType.Contributet | GroupType.CalcContribution;
                this._groupsDefinition[1].kind = 1;
                this._groupsDefinition[1].StatusChanged = this.UpdateStatusesAfterChange;
                this._groupsDefinition[1].defaultSortedParameter = 1; //tau
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

        public override void convert(List<ICurveParameters> curveParameters, IParameterSet parameters) {
            IGroup promptGroup = parameters[3];
            IGroup constantsGroup = parameters[0];
            IGroup rangesGroup = parameters[4];

            int parametersId = 0;
            int id = 0;
            int gid, pid, cid;
            int groupStartId = 0;
            IParameter firstShift = promptGroup.Components[0][2];
            IComponent firstPromptComponent = promptGroup.Components[0];

            for (gid = 0; gid < parameters.GroupCount; gid++) {
                if ((parameters[gid].Definition.Type & GroupType.Contributet) == GroupType.Contributet) {
                    //foreach (IComponent promptComp in promptGroup.Components) {

                    for (pid = 0; pid < promptGroup.Components.Size; pid++) {
                        id = groupStartId;
                        //foreach (IComponent comp in parameters[gid].Components) {
                        for (cid = 0; cid < parameters[gid].Components.Size; cid++) {
                            if (curveParameters.Count <= parametersId)
                                curveParameters.Add(new LTCurveParams());
                            LTCurveParams p = (LTCurveParams)curveParameters[parametersId];
                            parametersId++;
                            p.id = id++;
                            p.component = parameters[gid].Components[cid];
                            p.promptComponent = promptGroup.Components[pid];
                            p.fraction = 1;

                            p.tau = parameters[gid].Components[cid][1].Value; //tau
                            if (parameters[gid].Components[cid].ContainsParameter("sigma"))
                                p.dispersion = parameters[gid].Components[cid][2].Value;
                            else
                                p.dispersion = 0;
                            p.fwhm = promptGroup.Components[pid][1].Value; //fwhm
                            p.tauleft = promptGroup.Components[pid][3].Value;
                            p.tauright = promptGroup.Components[pid][4].Value;

                            if (pid == 0)
                                p.shift = 0;
                            else
                                p.shift = promptGroup.Components[pid][2].Value;
                            p.zeroCorrection = firstShift.Value;


                            p.nstart = (int)rangesGroup.Components[0][1].Value;
                            p.nstop = (int)rangesGroup.Components[0][2].Value;
                            p.lf = false;
                            p.rt = false;
                            p.with_fi = true;
                            p.diff = true;
                            p.cs = rangesGroup.Components[0][0].Value; //zero
                            p.bs = constantsGroup.Components[0][0].Value;
                        }

                    }
                    groupStartId += parameters[gid].Components.Size;
                }
            }
        }

    }
}
