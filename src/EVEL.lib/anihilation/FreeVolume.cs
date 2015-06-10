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

        private enum InfoParameterType
        {
            Unknown,
            RMean1,
            SigmaR1,
            VMean1,
            SigmaV1,
            RMean2,
            SigmaR2,
            VMean2,
            SigmaV2
        }

        #region Maths
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

        /// <summary>
        /// Searches the passed function's root using bisection method.
        /// </summary>
        /// <returns>true if function has odd root count in the given range,
        /// false if function has even root count in the given range.</returns>
        private bool Bisection(double y, out double x, double a, double b, Func<double, double, double> f)
        {
            const double eps = 1e-12;
            double fa, fb;
            fa = f(a, y);
            fb = f(b, y);
            x = 0.0;
            if (fa * fb > 0)
                return false;

            do
            {
                if (Math.Abs(fa) < eps)
                {
                    x = a;
                    break;
                }
                else if (Math.Abs(fb) < eps)
                {
                    x = b;
                    break;
                }

                x = (a + b) / 2.0;
                if (fa * f(x, y) > 0)
                {
                    a = x;
                    fa = f(a, y);
                }
                else
                {
                    b = x;
                    fb = f(b, y);
                }
            } while (Math.Abs(b - a) > eps);

            return true;
        }

        /// <summary>
        /// Calculates integral for the given function
        /// </summary>
        /// <returns></returns>
        private bool Simpson(double x0, double tau, double sigma, double a, double b,
            Func<double, double, double, double, double> f, double eps, int mMax, out double integral)
        {
            int maximum = (int)Math.Pow(2, mMax);
            double fa = f(a, x0, tau, sigma);
            double fb = f(b, x0, tau, sigma);
            int m = 1;
            double h = (b - a) / 2.0;
            double sig1 = f(a + h, x0, tau, sigma);
            double sig2 = 0.0;
            double Iprim = h / 3.0 * (fa + fb + 4.0 * sig1);
            do
            {
                m = 2 * m;
                double hprim = h;
                h = h / 2.0;
                sig2 = sig1 + sig2;
                double x = a + h;
                sig1 = f(x, x0, tau, sigma);

                for (int i = 2; i <= m; i++)
                {
                    x += hprim;
                    sig1 += f(x, x0, tau, sigma);
                }

                integral = h / 3.0 * (fa + fb + 4 * sig1 + 2.0 * sig2);

                if (Math.Abs(integral - Iprim) < eps)
                    return true;

                Iprim = integral;

            } while (m <= maximum);

            return false;
        }

        private double sqr(double x)
        {
            return x * x;
        }

        #endregion

        #region Info parameters

        private double fDist(double r, double tau, double sigma)
        {
            const double R0 = 0.166;

            double rpr0 = r + R0;
            double rpr02 = rpr0 * rpr0;
            double rd = r / rpr0;
            double lambda = 2.0 * (1.0 - rd + 1.0 / 2.0 / Math.PI * Math.Sin(2 * Math.PI * rd));

            double tmp = Math.Log((lambda) * tau) / sigma;
            double f_lambda = 0.39894228 / sigma / (lambda) * Math.Exp(-0.5 * tmp * tmp);

            double dlambda = 2.0 * R0 / rpr02 * (-1 + Math.Cos(2.0 * Math.PI * rd));
            return -f_lambda * dlambda;
        }

        private double FR_R(double R, double nic, double tau, double sigma)
        {
            return fDist(R, tau, sigma) * R;
        }

        private double DR_R(double R, double RMean, double tau, double sigma)
        {
            return fDist(R, tau, sigma) * (R - RMean) * (R - RMean);
        }

        private double FR_V(double R, double nic, double tau, double sigma)
        {
            return fDist(R, tau, sigma) * 4.188790205 * R * R * R;
        }

        private double DR_V(double R, double VMean, double tau, double sigma)
        {
            double tmp = 4.188790205 * R * R * R - VMean;
            return fDist(R, tau, sigma) * tmp * tmp;
        }

        private double FTao(double R, double tau)
        {
            const double R0 = 0.166;
            return 0.5 / (1 - R / (R + R0) + 0.159154943 * Math.Sin(6.283185307 * R / (R + R0))) - tau;
        }

        private void divpart(double tau, double sigma, double dx, double RMean, double sigR, double VMean, double sigV, double rrmin, double rrmax,
                          out double divR, out double divsig, out double divV, out double divSigV)
        {
            double RMean2, sigR2, VMean2, sigV2;

            Simpson(0, tau, sigma, rrmin, rrmax, FR_R, 1e-8, 10, out RMean2);
            Simpson(RMean2, tau, sigma, rrmin, rrmax, DR_R, 1e-8, 10, out sigR2);
            sigR2 = Math.Sqrt(sigR2);
            Simpson(0, tau, sigma, rrmin, rrmax, FR_V, 1e-8, 10, out VMean2);
            Simpson(VMean2, tau, sigma, rrmin, rrmax, DR_V, 1e-8, 10, out sigV2);
            sigV2 = Math.Sqrt(sigV2);
            divR = (RMean2 - RMean) / dx;
            divsig = (sigR2 - sigR) / dx;
            divV = (VMean2 - VMean) / dx;
            divSigV = (sigV2 - sigV) / dx;
        }

        private void Tau2Rad(double tau, double tauDev, double sigma, double sigmaDev,
                     out double rMean, out double devRMean, out double sigR, out double devSigR,
                         out double vMean, out double devVMean, out double sigV, out double devSigV)
        {
            double rr1, rr2, rrmin, rrmax,
                    divRMean_tau, divsig_R_tau, divVmean_tau, divSigV_tau,
                    divRMean_sig, divsig_R_sig, divVmean_sig, divSigV_sig;

            if (Math.Abs(sigma) < 1e-12)
            {
                Bisection(tau, out rMean, 0.05, 100, FTao);
                Bisection(tau + tauDev, out rr2, 0.05, 100, FTao);
                Bisection(tau - tauDev, out rr1, 0.05, 100, FTao);
                devRMean = rr2 - rr1;
                sigR = 0;
                devSigR = 0;
                vMean = 4.188790205 * rMean * rMean * rMean;
                devVMean = 4.188790205 * (rr2 * rr2 * rr2 - rr1 * rr1 * rr1);
                sigV = 0;
                devSigV = 0;
            }
            else
            {
                Bisection(0.54, out rrmin, 0.005, 100, FTao);
                Bisection(tau + 0.54 + 4 * sigma, out rrmax, 0.05, 100, FTao);
                Simpson(0, tau, sigma, rrmin, rrmax, FR_R, 1e-8, 10, out rMean);
                Simpson(rMean, tau, sigma, rrmin, rrmax, DR_R, 1e-8, 10, out sigR);
                sigR = Math.Sqrt(sigR);
                Simpson(0, tau, sigma, rrmin, rrmax, FR_V, 1e-8, 10, out vMean);
                Simpson(vMean, tau, sigma, rrmin, rrmax, DR_V, 1e-8, 10, out sigV);
                sigV = Math.Sqrt(sigV);

                divpart(1.01 * tau, sigma, 0.01 * tau, rMean, sigR, vMean, sigV, rrmin, rrmax,
                               out divRMean_tau, out divsig_R_tau, out divVmean_tau, out divSigV_tau);
                divpart(tau, 1.01 * sigma, 0.01 * sigma, rMean, sigR, vMean, sigV, rrmin, rrmax,
                               out divRMean_sig, out divsig_R_sig, out divVmean_sig, out divSigV_sig);

                devRMean = Math.Sqrt(sqr(divRMean_tau * tauDev) + sqr(divRMean_sig * sigmaDev));
                devSigR = Math.Sqrt(sqr(divsig_R_tau * tauDev) + sqr(divsig_R_sig * sigmaDev));
                devVMean = Math.Sqrt(sqr(divVmean_tau * tauDev) + sqr(divVmean_sig * sigmaDev));
                devSigV = Math.Sqrt(sqr(divSigV_tau * tauDev) + sqr(divSigV_sig * sigmaDev));
            }
        }

        #endregion

        private void AppendLtParams(double kapc, double lamIntr, double lamPs, double lam_diff,
            double sig, double taumin, Func<LTCurveParams> getCurrentParams, LTCurveParams diffsParameter)
        {

            if (sig > 0.0)
            {
                double norm = 0.39894228 / sig; //1 / Math.Math.Sqrt(2 * Math.PI) / sig;
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

        private double GetIntensity(IComponent component, IParameter parameter)
        {
            double lambdafree = 1 / component["taufree"].Value;
            double mi = component["mi"].Value;
            double kappa1 = component["kappa1"].Value;
            double kappa2 = component["kappa2"].Value;

            double denominator = lambdafree + mi + kappa1 + kappa2;
            double nominator = 0.0;

            if (parameter == component["intfree"])
                nominator = lambdafree;
            else if (parameter == component["inttrapp"])
                nominator = mi;
            else if (parameter == component["intpo1"])
                nominator = kappa1;
            else if (parameter == component["intpo2"])
                nominator = kappa2;

            return nominator / denominator;
        }

        private InfoParameterType GetParameterType(IParameter parameter)
        {
            switch (parameter.Definition.Name)
            {
                case "rmean1" : return InfoParameterType.RMean1;
                case "rmean2" : return InfoParameterType.RMean2;
                case "sigmar1": return InfoParameterType.SigmaR1;
                case "sigmar2": return InfoParameterType.SigmaR2;
                case "sigmav1": return InfoParameterType.SigmaV1;
                case "sigmav2": return InfoParameterType.SigmaV2;
                case "vmean1" : return InfoParameterType.VMean1;
                case "vmean2" : return InfoParameterType.VMean1;
                default: return InfoParameterType.Unknown;
            }
        }

        private double GetParameterValue(IComponent component, IParameter parameter)
        {
            double tau, tauDev, sigma, sigmaDev;

            switch (GetParameterType(parameter))
            {
                case InfoParameterType.RMean1:
                case InfoParameterType.SigmaR1:
                case InfoParameterType.SigmaV1:
                case InfoParameterType.VMean1:
                    tau = component["taupo1"].Value;
                    tauDev = component["taupo1"].Error;
                    sigma = component["sigmapo1"].Value;
                    sigmaDev = component["sigmapo1"].Error;
                    break;
                case InfoParameterType.RMean2:
                case InfoParameterType.SigmaR2:
                case InfoParameterType.SigmaV2:
                case InfoParameterType.VMean2:
                    tau = component["taupo2"].Value;
                    tauDev = component["taupo2"].Error;
                    sigma = component["sigmapo2"].Value;
                    sigmaDev = component["sigmapo2"].Error;
                    break;
                default:
                    return 0.0;
            }

            double rMean, devRMean, sigR, devSigR, vMean, devVMean, sigV, devSigV;

            Tau2Rad(tau, tauDev, sigma, sigmaDev, out rMean, out devRMean, out sigR, out devSigR, out vMean, out devVMean, out sigV, out devSigV);

            switch (GetParameterType(parameter))
            {
                case InfoParameterType.RMean1:
                case InfoParameterType.RMean2:
                    return rMean;
                case InfoParameterType.SigmaR1:
                case InfoParameterType.SigmaR2:
                    return sigR;
                case InfoParameterType.VMean1:
                case InfoParameterType.VMean2:
                    return vMean;
                case InfoParameterType.SigmaV1:
                case InfoParameterType.SigmaV2:
                    return sigV;

                default:
                    return 0.0;
            }
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
                    new ParameterDefinition("taufree", "[p text='t' font='symbol'][p text='free' index='sub']", ParameterStatus.Common | ParameterStatus.Free, 0),
                    new ParameterDefinition("tautrapp", "[p text='t' font='symbol'][p text='trapp' index='sub']", ParameterStatus.Common | ParameterStatus.Free, 0),
                    new ParameterDefinition("sigmatrapp", "[p text='s' font='symbol'][p text='trapp' index='sub']", ParameterStatus.Common | ParameterStatus.Free, 0),
                    new ParameterDefinition("mi", "[p text='m' font='symbol']", ParameterStatus.Local | ParameterStatus.Free, 0),
                    new ParameterDefinition("taupo1", "[p text='t' font='symbol'][p text='po1' index='sub']", ParameterStatus.Common | ParameterStatus.Free, 0),
                    new ParameterDefinition("sigmapo1", "[p text='s' font='symbol'][p text='po1' index='sub']", ParameterStatus.Common | ParameterStatus.Free, 0),
                    new ParameterDefinition("kappa1", "[p text='k' font='symbol'][p text='1' index='sub']", ParameterStatus.Local | ParameterStatus.Free, 0),
                    new ParameterDefinition("taupo2", "[p text='t' font='symbol'][p text='po2' index='sub']", ParameterStatus.Common | ParameterStatus.Free, 0),
                    new ParameterDefinition("sigmapo2", "[p text='s' font='symbol'][p text='po2' index='sub']", ParameterStatus.Common | ParameterStatus.Free, 0),
                    new ParameterDefinition("kappa2", "[p text='k' font='symbol'][p text='2' index='sub']", ParameterStatus.Local | ParameterStatus.Free, 0),

                    new ParameterDefinition("rmean1" , "[p text='R'][p text='mean1' index='sub']", ParameterProperties.Readonly | ParameterProperties.Unsearchable, ParameterStatus.Local | ParameterStatus.Fixed, GetParameterValue),
                    new ParameterDefinition("sigmar1", "[p text='s' font='symbol'][p text='R1' index='sub']", ParameterProperties.Readonly | ParameterProperties.Unsearchable, ParameterStatus.Local | ParameterStatus.Fixed, GetParameterValue),
                    new ParameterDefinition("vmean1" , "[p text='V'][p text='mean1' index='sub']", ParameterProperties.Readonly | ParameterProperties.Unsearchable, ParameterStatus.Local | ParameterStatus.Fixed, GetParameterValue),
                    new ParameterDefinition("sigmav1", "[p text='s' font='symbol'][p text='V1' index='sub']", ParameterProperties.Readonly | ParameterProperties.Unsearchable, ParameterStatus.Local | ParameterStatus.Fixed, GetParameterValue),
                    new ParameterDefinition("rmean2" , "[p text='R'][p text='mean2' index='sub']", ParameterProperties.Readonly | ParameterProperties.Unsearchable, ParameterStatus.Local | ParameterStatus.Fixed, GetParameterValue),
                    new ParameterDefinition("sigmar2", "[p text='s' font='symbol'][p text='R2' index='sub']", ParameterProperties.Readonly | ParameterProperties.Unsearchable, ParameterStatus.Local | ParameterStatus.Fixed, GetParameterValue),
                    new ParameterDefinition("vmean2" , "[p text='V'][p text='mean2' index='sub']", ParameterProperties.Readonly | ParameterProperties.Unsearchable, ParameterStatus.Local | ParameterStatus.Fixed, GetParameterValue),
                    new ParameterDefinition("sigmav2", "[p text='s' font='symbol'][p text='V2' index='sub']", ParameterProperties.Readonly | ParameterProperties.Unsearchable, ParameterStatus.Local | ParameterStatus.Fixed, GetParameterValue),

                    new ParameterDefinition("intfree", "[p text='I'][p text='free' index='sub']", ParameterProperties.Readonly | ParameterProperties.Unsearchable, ParameterStatus.Local | ParameterStatus.Fixed, GetIntensity),
                    new ParameterDefinition("inttrapp", "[p text='I'][p text='trapp' index='sub']", ParameterProperties.Readonly | ParameterProperties.Unsearchable, ParameterStatus.Local | ParameterStatus.Fixed, GetIntensity),
                    new ParameterDefinition("intpo1", "[p text='I'][p text='po1' index='sub']", ParameterProperties.Readonly | ParameterProperties.Unsearchable, ParameterStatus.Local | ParameterStatus.Fixed, GetIntensity),
                    new ParameterDefinition("intpo2", "[p text='I'][p text='po2' index='sub']", ParameterProperties.Readonly | ParameterProperties.Unsearchable, ParameterStatus.Local | ParameterStatus.Fixed, GetIntensity),

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