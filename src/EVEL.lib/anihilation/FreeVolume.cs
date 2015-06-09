using System;
using System.Collections.Generic;
using System.Text;
using Evel.interfaces;
using Evel.share;
using System.Linq;

namespace Evel.engine.anh.stdmodels
{
    public class FreeVolume : TwoStatesTrapping
    {

        private void gauss_integral_4(double a, double b, Action<double, double> ff)
        {
            const double t1 = -0.86113631;
            const double t2 = -0.33998104;
            const double a1 = 0.34785484;
            const double a2 = 0.65214516;
            double sum = (a + b) / 2;
            double dif = (b - a) / 2;

            Func<double, double> c = (double _a) =>
            {
                return dif * _a;
            };

            Func<double, double> x = (double _t) =>
            {
                return sum + dif * _t;
            };

            double c1 = c(a1);
            double c2 = c(a2);

            ff(x(t1), c1);
            ff(x(t2), c2);
            ff(x(-t2), c2);
            ff(x(-t1), c1);
        }

        private void AppendLtParams(double kapc, double lamIntr, double lamPs, double lam_diff,
            double sig, double taumin, Func<LTCurveParams> getCurrentParams, LTCurveParams diffsParameter)
        {

            if (sig > 0.0)
            {
                double norm = 0.39894228 / sig; //1 / Math.Sqrt(2 * Math.PI) / sig;
                double mian = 0.5 / sig / sig;
                double uu = Math.Log(1 / lamPs - taumin);

                Action<double, double> ff = (double u, double _c) =>
                {
                    if (Math.Abs(u) < 80)
                    {
                        double lambda = 1 / (Math.Exp(u) + taumin) + lamIntr;
                        double c = _c * kapc / (lam_diff - lamPs) * norm * Math.Exp(-(u - uu) * (u - uu) * mian);
                        LTCurveParams ltp = getCurrentParams();
                        ltp.tau = 1 / lambda;
                        ltp.fraction = c;

                        diffsParameter.fraction -= c * lambda / lam_diff;
                    }
                };

                gauss_integral_4(-6 * sig + uu, -3 * sig + uu, ff);

                gauss_integral_4(-3 * sig + uu, -sig + uu, ff);

                gauss_integral_4(-sig + uu, sig + uu, ff);

                gauss_integral_4(sig + uu, 3 * sig + uu, ff);

                gauss_integral_4(3 * sig + uu, 6 * sig + uu, ff);
            }
            else
            {
                double lamP = lamPs + lamIntr;
                double tmpc = kapc / (lam_diff - lamP);
                LTCurveParams ltp = getCurrentParams();
                ltp.tau = 1 / lamP;
                ltp.fraction = tmpc;
                diffsParameter.fraction -= tmpc * lamP / lam_diff;
            }
        }


        private double GetParameterValue(IComponent component, IParameter parameter)
        {
            ////double inta, intb, taua, taub, taueff;
            //DefectParameters dfA, dfB, dfC, dfFree;
            //getConversionParameters(component, out dfA, out dfB, out dfC, out dfFree);
            //if (parameter == component[8])
            //{
            //    return dfA.i;
            //}
            //else if (parameter == component[9])
            //{
            //    return dfB.i;
            //}
            //else if (parameter == component[10])
            //{
            //    return dfC.i;
            //}
            return 0;
        }

        protected override double CalculateAverageLifetime(IComponent component, IParameter parameter)
        {
            if (parameter == component[0] && component.Parent is IGroup)
            {
                //DefectParameters dfA, dfB, dfC, dfFree;
                //getConversionParameters(((IGroup)component.Parent).Components[0], out dfA, out dfB, out dfC, out dfFree);
                //return dfA.i * dfA.t + dfB.i * dfB.t + dfC.i * dfC.t + dfFree.i * dfFree.t;
                return 0.0;
            }
            else
                return 0.0;
        }

        public override string DeltaFileName
        {
            get
            {
                return "freevolume";
            }
        }

        public override string Description
        {
            get
            {
                return "[p text='Free Volume curve.\n\n']";
            }
        }

        public override string Name
        {
            get { return "Free Volume"; }
        }

        //public override CalculateParameterValueHandler CalculateParameterValue {
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
                //this._groupsDefinition[1].allowMultiple = false;
                this._groupsDefinition[1].componentCount = 1;
                this._groupsDefinition[1].name = "sample";
                this._groupsDefinition[1].parameters = new ParameterDefinition[] { 
                    new ParameterDefinition("int", ParameterStatus.Local | ParameterStatus.Free, ParameterProperties.Hidden | ParameterProperties.ComponentIntensity),
                    new ParameterDefinition("tauPo1", "[p text='t' font='symbol'][p text='po1' index='sub']", ParameterStatus.Common | ParameterStatus.Free, 0),
                    new ParameterDefinition("sigmaPo1", "[p text='s' font='symbol'][p text='po1' index='sub']", ParameterStatus.Common | ParameterStatus.Free, 0),
                    new ParameterDefinition("kappa1", "[p text='k' font='symbol'][p text='1' index='sub']", ParameterStatus.Local | ParameterStatus.Free, 0),
                    new ParameterDefinition("tauPo2", "[p text='t' font='symbol'][p text='po2' index='sub']", ParameterStatus.Common | ParameterStatus.Free, 0),
                    new ParameterDefinition("sigmaPo2", "[p text='s' font='symbol'][p text='po2' index='sub']", ParameterStatus.Common | ParameterStatus.Free, 0),
                    new ParameterDefinition("kappa2", "[p text='k' font='symbol'][p text='2' index='sub']", ParameterStatus.Local | ParameterStatus.Free, 0),
                    new ParameterDefinition("tautrapp", "[p text='t' font='symbol'][p text='trapp' index='sub']", ParameterStatus.Common | ParameterStatus.Free, 0),
                    new ParameterDefinition("sigmatrapp", "[p text='s' font='symbol'][p text='trapp' index='sub']", ParameterStatus.Common | ParameterStatus.Free, 0),
                    new ParameterDefinition("mi", "[p text='m' font='symbol']", ParameterStatus.Local | ParameterStatus.Free, 0),
                    new ParameterDefinition("taufree", "[p text='t' font='symbol'][p text='free' index='sub']", ParameterStatus.Common | ParameterStatus.Free, 0)
                    //new ParameterDefinition("intD", "[p text='int'][p text='D' index='sub']", ParameterProperties.Readonly | ParameterProperties.Unsearchable, ParameterStatus.Local | ParameterStatus.Fixed, GetParameterValue),
                    //new ParameterDefinition("tau", "[p text='t' font='symbol'][p text='avg' index='sub']", ParameterProperties.Readonly | ParameterProperties.Unsearchable | ParameterProperties.GroupUnique, ParameterStatus.Local | ParameterStatus.Fixed, CalculateAverageLifetime)
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
        public override void convert(List<ICurveParameters> curveParameters, IParameterSet parameters)
        {
            const double eps = 1e-6;
            double kappa1 = parameters[1].Components[0]["kappa1"].Value;
            double kappa2 = parameters[1].Components[0]["kappa2"].Value;
            double mi = parameters[1].Components[0]["mi"].Value;
            double sigma1 = parameters[1].Components[0]["sigmaPo1"].Value;
            double sigma2 = parameters[1].Components[0]["sigmaPo2"].Value;
            double sigmaTrapp = parameters[1].Components[0]["sigmatrapp"].Value;
            double lambdaortho1 = 1 / parameters[1].Components[0]["tauPo1"].Value;
            double lampara1 = 1 / parameters[1].Components[0]["tauPo1"].Value;
            double lambdaortho2 = 1 / parameters[1].Components[0]["tauPo2"].Value;
            double lampara2 = 1 / parameters[1].Components[0]["tauPo2"].Value;
            double lambdaTrapp = 1 / parameters[1].Components[0]["tautrapp"].Value;
            double lambdaFree = 1 / parameters[1].Components[0]["taufree"].Value;
            double lam = lambdaFree + kappa1 + kappa2 + mi;

            int currentParamsIndex = 0;

            Func<LTCurveParams> GetCurrentParameters = () =>
            {
                LTCurveParams p;
                if (currentParamsIndex >= curveParameters.Count)
                {
                    p = new LTCurveParams();
                    curveParameters.Add(p);
                }
                else
                {
                    p = curveParameters[currentParamsIndex] as LTCurveParams;
                }

                p.id = 0;
                p.component = p.promptComponent = null;
                p.dispersion = p.fraction = p.fwhm = p.shift = 0;
                p.with_fi = p.diff = false;

                currentParamsIndex++;
                return p;
            };

            LTCurveParams diffsParameters = GetCurrentParameters();

            if (kappa1 > eps)
            {
                //ortho-Ps
                AppendLtParams(0.75 * kappa1, 1.0 / 142.0, lambdaortho1, lam, sigma1, 0.54, GetCurrentParameters, diffsParameters);

                //para-Ps
                AppendLtParams(0.25 * kappa1, 1.0 / 0.125, lampara1, lam, 0.0, 0.0, GetCurrentParameters, diffsParameters);
            }

            if (kappa2 > eps)
            {
                //ortho-Ps
                AppendLtParams(0.75 * kappa2, 1.0 / 142.0, lambdaortho2, lam, sigma2, 0.54, GetCurrentParameters, diffsParameters);

                //para-Ps
                AppendLtParams(0.25 * kappa2, 1.0 / 0.125, lampara2, lam, 0.0, 0.0, GetCurrentParameters, diffsParameters);
            }

            if (mi > eps)
            {
                AppendLtParams(mi, 0, lambdaTrapp, lam, sigmaTrapp, 0.0, GetCurrentParameters, diffsParameters);
            }

            //bulk
            {
                LTCurveParams pfree = GetCurrentParameters();
                pfree.tau = 1 / lambdaFree;
                pfree.fraction = lambdaFree / lam;
            }

            diffsParameters.tau = 1 / lam;

            double sum = 0.0;

            for (int i = 0; i < currentParamsIndex; i++)
            {
                LTCurveParams p = curveParameters[i] as LTCurveParams;
                //sum += p.fraction;
                p.component = parameters[1].Components[0];
            }

            //for (int i = 0; i < currentParamsIndex; i++)
            //    (curveParameters[i] as LTCurveParams).fraction /= sum;

#if DEBUG
            sum = 0.0;
            for (int i = 0; i < currentParamsIndex; i++)
                sum += (curveParameters[i] as LTCurveParams).fraction;

            System.Diagnostics.Debug.Assert(Math.Abs(sum - 1.0) < 1e-3);
#endif

            //source components
            for (int i = 0; i < parameters[2].Components.Size; i++)
            {
                LTCurveParams p = GetCurrentParameters();
                p.tau = Math.Abs(parameters[2].Components[i][1].Value);
                p.fraction = 1.0;
                p.id = 1 + i;
                p.component = parameters[2].Components[i];
            }

            IGroup promptGroup = parameters[3];
            IGroup constantsGroup = parameters[0];
            IGroup rangesGroup = parameters[4];
            int baseLength = currentParamsIndex;

            for (int p = promptGroup.Components.Size - 1; p >= 0; p--)
            {
                for (int i = 0; i < baseLength; i++)
                {
                    LTCurveParams pi0 = curveParameters[i] as LTCurveParams;
                    LTCurveParams currentParams;

                    if (p == 0)
                    {
                        currentParams = pi0;
                    }
                    else
                    {
                        currentParams = GetCurrentParameters();
                        currentParams.tau = pi0.tau;
                        currentParams.fraction = pi0.fraction;
                        currentParams.id = pi0.id;
                    }

                    currentParams.promptComponent = promptGroup.Components[p];
                    currentParams.fwhm = Math.Abs(promptGroup.Components[p][1].Value);
                    currentParams.shift = promptGroup.Components[p][2].Value;
                    currentParams.tauleft = Math.Abs(promptGroup.Components[p][3].Value);
                    currentParams.tauright = Math.Abs(promptGroup.Components[p][4].Value);
                    currentParams.nstart = (int)rangesGroup.Components[0][1].Value;
                    currentParams.nstop = (int)rangesGroup.Components[0][2].Value;
                    currentParams.lf = false;
                    currentParams.rt = false;
                    currentParams.with_fi = true;
                    currentParams.diff = true;
                    currentParams.cs = rangesGroup.Components[0][0].Value;
                    currentParams.bs = constantsGroup.Components[0][0].Value;
                    currentParams.zeroCorrection = promptGroup.Components[0][2].Value;
                }

            }
        }

#if DEBUG
        private string SerializeParameters(List<ICurveParameters> curveParameters)
        {
            StringBuilder builder = new StringBuilder();

            int i = 0;

            foreach (LTCurveParams p in curveParameters.OfType<LTCurveParams>())
            {
                builder.AppendFormat("{0}Parameters {1}{0}\n", new String('-', 10), i);
                foreach (System.Reflection.FieldInfo f in p.GetType().GetFields().Where(_f => !_f.FieldType.IsClass))
                {
                    builder.AppendFormat("{0}={1}\n", f.Name, f.GetValue(p));
                }
                builder.AppendLine();
                i++;
            }

            return builder.ToString();
        }
#endif
    }
}