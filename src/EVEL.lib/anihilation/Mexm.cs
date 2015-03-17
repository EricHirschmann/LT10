using System;
using System.Collections.Generic;
using Evel.interfaces;
using Evel.share;
using System.Xml;
using System.IO;
using System.Globalization;
using MathExpressions;

namespace Evel.engine.anh.stdmodels {

    public class DefaultRangesEventArgs : EventArgs {

        public bool defaultZero;
        public bool defaultStart;
        public bool defaultStop;
        public bool defaultBackground;

        public DefaultRangesEventArgs(bool defaultZero, bool defaultStart, bool defaultStop, bool defaultBackground)
            : base() {
            this.defaultZero = defaultZero;
            this.defaultStart = defaultStart;
            this.defaultStop = defaultStop;
            this.defaultBackground = defaultBackground;
        }
    }

    public class Mexm : IModel {

        //XmlDocument deltaxDefinition;
        protected NumberFormatInfo numberFormat;
        protected GroupDefinition[] _groupsDefinition;
        string deltaFilePath;
        protected HashSet<ParameterDefaultPattern> _defaultPatterns;

        public Mexm() {
            if (!File.Exists(String.Format("{0}/{1}.xml", AvailableAssemblies.LibraryDir, DeltaFileName)))
                throw new IOException(String.Format("Couldn't find deltax definition file ({0}/{1}.xml)", AvailableAssemblies.LibraryDir, DeltaFileName));
            //deltaxDefinition = new XmlDocument();
            //deltaxDefinition.Load(String.Format("lib/{0}.xml", DeltaFileName));
            deltaFilePath = String.Format("{0}/{1}.xml", AvailableAssemblies.LibraryDir, DeltaFileName);
            numberFormat = new CultureInfo("en-US", false).NumberFormat;
            numberFormat.NumberDecimalSeparator = ".";
            _defaultPatterns = new HashSet<ParameterDefaultPattern>();
            loadParameterDefaultPatterns();
        }

        #region IModel Members

        //public virtual ParameterChangeHandler OnParameterChange {
        //    get { return null; }
        //}

        //public virtual CalculateParameterValueHandler CalculateParameterValue {
        //    get { return null; }
        //}

        public virtual string DeltaFileName {
            get {
                return "mexm";
            }
        }

        public virtual string Description {
            get {
                return "[p text='Simple theoretical model which uses sample components built from lifetime and intensity parameters\n\n']";
            }
        }

        public virtual string Name {
            get { return "Multiexponential"; }
        }

        public virtual void convert(List<ICurveParameters> curveParameters, IParameterSet parameters) {
            IGroup promptGroup = parameters[3];
            IGroup constantsGroup = parameters[0];
            IGroup rangesGroup = parameters[4];

            //if (curveParameters == null)
            //    curveParameters = new List<ICurveParameters>();

            //int resultId = 0;
            int parametersId = 0;
            int id = 0;
            int gid, pid, cid;
            int groupStartId = 0;
            IParameter firstShift = promptGroup.Components[0][2];
            IComponent firstPromptComponent = promptGroup.Components[0];
            //foreach (IGroup group in parameters) {
            for (gid = 0; gid<parameters.GroupCount; gid++) {
                if ((parameters[gid].Definition.Type & GroupType.Contributet) == GroupType.Contributet) {
                    //foreach (IComponent promptComp in promptGroup.Components) {
                    
                    for (pid=0; pid<promptGroup.Components.Size; pid++) {
                        id = groupStartId;
                        //foreach (IComponent comp in parameters[gid].Components) {
                        for (cid=0; cid<parameters[gid].Components.Size; cid++) {
                            if (curveParameters.Count <= parametersId)
                                curveParameters.Add(new LTCurveParams());
                            LTCurveParams p = (LTCurveParams)curveParameters[parametersId];
                            parametersId++;
                            p.id = id++;
                            p.component = parameters[gid].Components[cid];
                            p.promptComponent = promptGroup.Components[pid];
                            p.fraction = 1;
                            p.tau = parameters[gid].Components[cid][1].Value; //tau
                            p.dispersion = 0;
                            p.fwhm = promptGroup.Components[pid][1].Value; //fwhm
                            p.tauleft = promptGroup.Components[pid][3].Value;
                            p.tauright = promptGroup.Components[pid][4].Value;
                            //if (pid==0) // (promptComp == firstPromptComponent) 
                            //    p.x0 = promptGroup.Components[pid][2].Value;
                            //    p.zeroCorrection = firstShift.Value;
                            //else
                            //    p.x0 = firstShift.Value + promptGroup.Components[pid][2].Value;
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
            //return result;
        }

        public virtual void loadParameterDefaultPatterns() {
            _defaultPatterns.Clear();
            XmlReaderSettings settings = new XmlReaderSettings();
            settings.IgnoreWhitespace = true;
            XmlReader reader = XmlReader.Create(this.deltaFilePath, settings);
            reader.ReadToFollowing("group");

            do {
                int kind = 0;
                while (reader.MoveToNextAttribute()) {
                    switch (reader.Name) {
                        case "kind": kind = Int32.Parse(reader.Value); break;
                    }
                }
                reader.MoveToElement();
                reader.ReadToDescendant("deltax");
                do {
                    ParameterDefaultPattern pattern = new ParameterDefaultPattern();
                    pattern.groupKind = kind;
                    //pattern.name = "";
                    //double mp = 0;
                    //double v = 0;
                    //double min = Double.NegativeInfinity;
                    //double max = Double.PositiveInfinity;
                    //double defaultValue = Double.NaN;
                    while (reader.MoveToNextAttribute()) {
                        switch (reader.Name) {
                            case "name": pattern.name = reader.Value; break;
                            case "mp": pattern.multi = Double.Parse(reader.Value, numberFormat); break;
                            case "value": pattern.value = Double.Parse(reader.Value, numberFormat); break;
                            case "min": pattern.min = Double.Parse(reader.Value, numberFormat); break;
                            case "max": pattern.max = Double.Parse(reader.Value, numberFormat); break;
                            case "default": pattern.defaultValue = Double.Parse(reader.Value, numberFormat); break;
                            case "positive": pattern.positive = bool.Parse(reader.Value); break;
                        }
                    }
                    reader.MoveToElement();
                    _defaultPatterns.Add(pattern);
                } while (reader.ReadToNextSibling("deltax"));

            } while (reader.ReadToNextSibling("group"));
            reader.Close();
        }

        //public virtual void checkParameters(IParameterSet parameters, CheckOptions options) {
        public virtual void checkParameter(IParameter parameter, ISpectrum spectrum, CheckOptions options) {
            //foreach (IParameter parameter in parameters) {
            int groupKind;
            if (parameter.Parent.GetType() == typeof(ContributedGroup))
                groupKind = ((ContributedGroup)parameter.Parent).Definition.kind;
            else
                groupKind = ((IComponents)((IComponent)parameter.Parent).Parent).Parent.Definition.kind;
            
            foreach (ParameterDefaultPattern pattern in _defaultPatterns) {
                if (pattern.name.Equals(parameter.Definition.Name) && pattern.groupKind == groupKind) {
                    if ((parameter.Status & ParameterStatus.Free) > 0) {
                        //default value
                        if ((options & CheckOptions.SetDefaultValues) > 0) {
                            if (pattern.positive)
                                parameter.Value = Math.Abs(parameter.Value);
                            if (pattern.defaultValue != Double.NaN) {
                                if ((pattern.min > parameter.Value) || (pattern.max < parameter.Value)) {
                                    parameter.Value = pattern.defaultValue;
                                }
                            }
                        }
                        //delta
                        if ((options & CheckOptions.RefreshDelta) > 0 && (!(parameter.HasReferenceValue && (options & CheckOptions.NoReferencedDelta) > 0) || (parameter.Status & ParameterStatus.Binding) > 0)) {
                            if (parameter.Definition.Name.ToLower() == "shift") {
                                parameter.Delta = spectrum.Constants[0] * 0.0001; //constants[0] = bs
                                IComponent comp = (IComponent)parameter.Parent;
                                if (((IComponents)comp.Parent).IndexOf(comp) == 0)
                                    parameter.Delta *= 10;

                            } else {
                                parameter.Delta = parameter.Value * pattern.multi + pattern.value;
                                if (parameter.Definition.Name.ToLower().IndexOf("tau")!=-1)
                                    parameter.Delta += spectrum.Constants[0] * 0.005;
                            }
                            if (parameter.ReferencedValues > 1)
                                parameter.Delta *= 5 * Math.Sqrt(parameter.ReferencedValues);
                            //if ((parameter.Status & ParameterStatus.Common) == ParameterStatus.Common)
                            //    parameter.Delta *= 10;
                        }
                    } else {
                        parameter.Delta = parameter.Value * 5e-4;
                    }
                    break;
                }
            }
            //}
        }

        protected virtual double CalculateAverageLifetime(IComponent component, IParameter parameter) {
            if (parameter == component[0] && component.Parent is IGroup) {
                double sum = 0.0;
                for (int i = 0; i < ((IGroup)component.Parent).Components.Size; i++)
                    sum += ((IGroup)component.Parent).Components[i][0].Value * ((IGroup)component.Parent).Components[i][1].Value;
                return sum;
            } else
                return 0.0;
        }        

        protected virtual GroupDefinition[] getGroupsDefinition() {
            if (this._groupsDefinition == null) {
                this._groupsDefinition = new GroupDefinition[5];
                //constants
                //this._groupsDefinition[0].allowMultiple = false;
                this._groupsDefinition[0].componentCount = 1;
                this._groupsDefinition[0].name = "constants";
                this._groupsDefinition[0].parameters = new ParameterDefinition[] { 
                    new ParameterDefinition("bs", ParameterStatus.Local | ParameterStatus.Fixed, ParameterProperties.Unsearchable), 
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
                    new ParameterDefinition("tau", "[p text='t' font='symbol'][p text='avg' index='sub']", ParameterProperties.Readonly | ParameterProperties.GroupUnique, ParameterStatus.Local | ParameterStatus.Fixed, CalculateAverageLifetime)
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

        public virtual GroupDefinition[] GroupsDefinition {
            get {
                return getGroupsDefinition();
            }
            set {
                this._groupsDefinition = value;
            }
        }

        protected virtual void UpdateStatusesAfterChange(Object sender, StatusChangeEventArgs args) {
            if ((args.parameter.Definition.Properties & ParameterProperties.ComponentIntensity) > 0) {
                ParameterStatus commonfixed = ParameterStatus.Common | ParameterStatus.Fixed;
                //zmieniaj wszystkie pozostale statusy pod warunkiem ze wybieranym statusem nie jest common fixed (common fixed moze istniec bez wzgledu na pozostale statusy)
                if ((args.status & commonfixed) != commonfixed) {
                    ParameterStatus constr = args.status & (ParameterStatus.Local | ParameterStatus.Common);
                    for (int c = 0; c < args.group.Components.Size; c++)
                        //zmieniaj tylko pozostale statusy - "wolacza" mozna sobie odpuscic
                        //
                        if (args.parameter != args.group.Components[c][0] &&
                            (args.group.Components[c][0].Status & commonfixed) != commonfixed) {
                            args.group.Components[c][0].Status = args.group.Components[c][0].Status & ~(ParameterStatus.Local | ParameterStatus.Common) | constr;
                            //update references in other parameters
                            if (args.spectrum != args.spectra[0] && (args.status & ParameterStatus.Common) > 0) {
                                args.group.Components[c][0].ReferencedParameter = args.spectra[0].Parameters[args.group.Definition.name].Components[c][0];
                            }
                        }
                }
            }
        }

        protected virtual void SetDefaultPrompt(IGroup sender, ISpectrum spectrum, EventArgs args) {
            sender.Components.Size = 2;
            foreach (IComponent component in sender.Components) {
                component[3].Status = ParameterStatus.Common | ParameterStatus.Fixed;
                component[4].Status = ParameterStatus.Common | ParameterStatus.Fixed;
            }
            if (spectrum.Constants.Length >= 3) {
                sender.Components[0][0].Value = 0.9;
                sender.Components[0][1].Value = spectrum.Constants[2];
                sender.Components[0][2].Value = 0;
                sender.Components[1][0].Value = 0.1;
                sender.Components[1][1].Value = spectrum.Constants[2] * 1.3;
                sender.Components[1][2].Value = 0;// spectrum.Constants[0];
            }
        }

        protected virtual void SetDefaultRanges(IGroup sender, ISpectrum spectrum, EventArgs args) {
            sender.Components.Size = 1;
            //peak
            bool defaultStart = true;
            bool defaultStop = true;
            bool defaultZero = true;
            bool defaultBackground = true;
            if (args is DefaultRangesEventArgs) {
                defaultStart = ((DefaultRangesEventArgs)args).defaultStart;
                defaultStop = ((DefaultRangesEventArgs)args).defaultStop;
                defaultZero = ((DefaultRangesEventArgs)args).defaultZero;
                defaultBackground = ((DefaultRangesEventArgs)args).defaultBackground;
            }
            if (defaultZero) {
                int max = 0;
                //for (int ch = 0; ch < spectrum.ExperimentalSpectrum.Length; ch++)
                //    if (spectrum.ExperimentalSpectrum[ch] > max) {
                //        max = spectrum.ExperimentalSpectrum[ch];
                //        sender.Components[0][0].Value = ch;
                //    }
                for (int ch = spectrum.BufferStartPos; ch <= spectrum.BufferEndPos; ch++)
                    if (spectrum.Container.Data[ch] > max) {
                        max = spectrum.Container.Data[ch];
                        sender.Components[0][0].Value = ch - spectrum.BufferStartPos;
                    }
                sender.Components[0][0].Value -= 2;
                //sender.Components[0][0].Value -= 0.11 / spectrum.Parameters[0].Components[0][0].Value;
            }
            //background
            if (defaultBackground) {
                double sum = 0;
                //for (int ch = spectrum.ExperimentalSpectrum.Length - 1; ch >= spectrum.ExperimentalSpectrum.Length - 100; ch--)
                //    sum += spectrum.ExperimentalSpectrum[ch];
                for (int ch = spectrum.BufferEndPos; ch >= spectrum.BufferEndPos - 100; ch--)
                    sum += spectrum.Container.Data[ch];
                sender.Components[0][3].Value = sum / 101;
            }
            
            //ranges
            if (defaultStart)
                sender.Components[0][1].Value = 10;
            if (defaultStop)
                //sender.Components[0][2].Value = spectrum.ExperimentalSpectrum.Length - 2;
                sender.Components[0][2].Value = spectrum.BufferEndPos - spectrum.BufferStartPos - 2;
        }

        protected IParameter addParameter(IParameter parameter, ISpectrum spectrum, CheckOptions co) {
            checkParameter(parameter, spectrum, co);
            if (parameter.HasReferenceValue)
                return parameter.ReferencedParameter;
            else
                return parameter;
        }

        //public virtual List<IParameter> getParameters(ParameterStatus status, IParameterSet parameters, bool[] includeFlags) {
        public virtual IEnumerable<IParameter> getParameters(ParameterStatus status, ISpectrum spectrum, bool[] includeFlags, CheckOptions co) {
            //List<IParameter> result = new List<IParameter>();
            bool includeInts = includeFlags[0];
            bool includeSourceContrib = includeFlags[1];
            bool promptOnly = includeFlags[2];
            //result.Add(null);
            IGroup group;
            //foreach (IGroup group in parameters) {
            int cid, pid;
            for (int gr = 1; gr<spectrum.Parameters.GroupCount; gr++) {
                group = spectrum.Parameters[gr];
                if (promptOnly && group.Definition.kind != 3) 
                    continue;
                //foreach (IComponent component in group.Components) {
                for (cid=0; cid<group.Components.Size; cid++) {
                    //foreach (IParameter parameter in component) {
                    for (pid=0; pid<group.Components[cid].Size; pid++) {
                        if (pid == 0 && cid == 0) continue;
                        if ((group.Components[cid][pid].Definition.Properties & ParameterProperties.Unsearchable) == ParameterProperties.Unsearchable)
                            continue;
                        if (((group.Components[cid][pid].Status & status) == status) &&
                            !((group.Components[cid][pid].Definition.Name == "int") && (group.Components[cid][pid] == group.Components[0]))) {
                            if (!includeInts && (group.Components[cid][pid].Definition.Name == "int") &&
                                !(group.Definition.kind == 3) && //prompt intensities are always put into Search parameters
                                !(group.Components[cid][pid].Definition.Name == "int" && (status & ParameterStatus.Common) == ParameterStatus.Common)) //common intensities are also always put into search
                                continue;
                            switch (group.Definition.kind) {
                                case 4: //ranges
                                    if (includeInts && (group.Components[cid][pid].Definition.Name == "background") && !group.Components[cid][pid].HasReferenceValue)
                                        yield return addParameter(group.Components[cid][pid], spectrum, co);
                                    break;
                                default:
                                    yield return addParameter(group.Components[cid][pid], spectrum, co);
                                    //if (group.Definition.kind == 3 && cid > 0 && pid == 2)
                                    //    group.Components[cid][pid].Delta /= 10;
                                    break;

                            }
                        }
                    }
                }
                if (((group.Definition.Type & GroupType.Contributet) == GroupType.Contributet) &&
                    ((group.Definition.Type & GroupType.CalcContribution) != GroupType.CalcContribution) &&
                    (group.Components.Size > 0)) {
                    IParameter contribution = ((ContributedGroup)group).contribution;
                    if ((contribution.Status & status) == status) {
                        switch (group.Definition.kind) {
                            /*sample*/
                            case 1:
                                yield return addParameter(contribution, spectrum, co);
                                break;
                            /*source*/
                            case 2:
                                if (!((contribution.Status & ParameterStatus.Fixed) == ParameterStatus.Fixed)) {
                                    if (includeInts || (includeSourceContrib && ((contribution.Status & ParameterStatus.Common)) == ParameterStatus.Common)) {
                                        yield return addParameter(contribution, spectrum, co);
                                    }
                                }
                                break;
                        }
                    }
                }
            }
        }

        public virtual Type SpectrumType {
            get {
                return typeof(Evel.engine.anh.AnhSpectrum);
            }
        }

        public virtual Type ProjectType {
            get { return typeof(AnhProject); } 
        }

        public virtual HashSet<ParameterDefaultPattern> defaultPatterns { 
            get {
                return this._defaultPatterns;
            }
        }

        #region Marquardt fit

        public virtual int setparams(IParameterSet ps, IParameter[] a, bool[] ai, bool[] f, ParameterStatus status, ISpectrum spectrum) {
            int g, c, p;
            int pc = 0;
            int bufferpos = ps.BufferStart;
            bool commonf = (status & (ParameterStatus.Common | ParameterStatus.Free)) == (ParameterStatus.Common | ParameterStatus.Free);
            IParameter ip;
            for (g = 1; g < ps.GroupCount; g++) {
                for (c = 0; c < ps[g].Components.Size; c++)
                    for (p = 0; p < ps[g].Components[c].Size; p++) {
                        ip = ps[g].Components[c][p];
                        if ((p == 0 && c == 0) || (ip.Definition.Properties & ParameterProperties.Unsearchable) == ParameterProperties.Unsearchable)
                            continue;
                        else {
                            checkParameter(ip, spectrum, CheckOptions.RefreshDelta | CheckOptions.SetDefaultValues);
                            pc++;
                            if (a != null)
                                a[bufferpos] = ip;
                            if (ai != null && f!= null) {
                                if (ai[bufferpos] = (ip.Status & status) == status && !ip.HasReferenceValue) {//if parameter doesnt have desired status then it is out of search
                                    //ai[bufferpos] = (p == 0) && (f[0] || ps[g].Definition.kind == 3 || commonf) || //if intensity and either: includeints OR prompt intensity OR common intensity
                                    //    (f[0] && p == 3 && ps[g].Definition.kind == 4); //or include ints and background is the analysing parameter
                                    ai[bufferpos] = f[0] && (p==0 || ps[g].Definition.kind == 4) || //if intensity or background with included intensities se to true
                                                    ps[g].Definition.kind == 3 || //prompt always included
                                                    !f[0] && p>0 && ps[g].Definition.kind < 4;
                                }
                            }
                            bufferpos++;
                        }
                    }
                if (ps[g].Definition.kind == 2) { //only source contribution may be putted into search
                    ip = ((ContributedGroup)ps[g]).contribution;
                    if ((ip.Definition.Properties & ParameterProperties.Unsearchable) != ParameterProperties.Unsearchable) {
                        checkParameter(ip, spectrum, CheckOptions.RefreshDelta | CheckOptions.SetDefaultValues);
                        pc++;
                        if (a != null)
                            a[bufferpos] = ip;
                        if (ai != null && f != null) {
                            if (ai[bufferpos] = (ip.Status & status) == status && !ip.HasReferenceValue) {//if parameter doesnt have defined status then it is out of search
                                ai[bufferpos] = f[0] || f[1]; // (f[1] && commonf);
                            }
                        }
                        bufferpos++;
                    }
                }
            }
            return pc;
        }

        #endregion

        #endregion

    }
}
