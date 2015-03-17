using System;
using System.Collections.Generic;
using System.Text;
using Evel.interfaces;
using MathExpressions;

namespace Evel.engine {

    public class Parameter : IParameter {

        public event EventHandler OnValueChange;

        private User2SearchConversion u2sConversion;
        private Search2UserConversion s2uConversion;

        private IParameter _referencedParameter = null;
        private ParameterDefinition _definition;
        private double _backup;
        private double _value;
        private double _error;
        private double _delta;
        public short _referencedValues;
        //private bool _bindingParameter;
        private double _minimum = Double.NegativeInfinity;
        private double _maximum = Double.PositiveInfinity;
        private ParameterStatus _status = ParameterStatus.Local | ParameterStatus.Fixed;
        object _parent;
        private int _referenceGroup;
        //private Expression _expression = null;

        //public Expression Expression {
        //    get { return this._expression; }
        //    set { this._expression = value; }
        //}

        public override string ToString() {
            return String.Format("{0} = {1}±{2} ({3})", Definition, Value, Error, Status);
        }

        public object Parent {
            get { return this._parent; }
        }

        public ParameterDefinition Definition {
            get { return this._definition; }
        }

        public ParameterStatus Status {
            get {
                if (this._referencedParameter != null)
                    return this._referencedParameter.Status;
                else
                    return this._status;
            }
            set {
                if (this._referencedParameter != null)
                    this._referencedParameter.Status = value;
                //else {
                    bool failure = false;
                    //Common Local || Free Fixed exception
                    failure = ((value & (ParameterStatus.Local | ParameterStatus.Common)) == (ParameterStatus.Local | ParameterStatus.Common)) ||
                              ((value & (ParameterStatus.Free | ParameterStatus.Fixed)) == (ParameterStatus.Free | ParameterStatus.Fixed)) ||
                              ((value & ParameterStatus.None) == ParameterStatus.None);
                    if (failure)
                        throw new ArgumentException(String.Format("Illegal {0} status: {1}", _definition.Name, value));
                    else
                        this._status = value;
                    if ((this._status & ParameterStatus.Local) == ParameterStatus.Local)
                        this.ReferencedParameter = null;
                //}
            }
        }

        public void SaveBackup() {
            this._backup = this._value;
        }

        public void Backup() {
            this._value = this._backup;
        }

        public int ReferenceGroup {
            get { return this._referenceGroup; }
            set { this._referenceGroup = value; }
        }

        //protected Parameter(IParameter parameter) {
        //    this._definition = parameter.Definition;
        //    this._delta = parameter.Delta;
        //    this._error = parameter.Error;
        //    this.Maximum = parameter.Maximum;
        //    this.Minimum = parameter.Minimum;
        //    this._parent = parameter.Parent;
        //    this._referencedParameter = parameter.ReferencedParameter;
        //    this._referenceGroup = parameter.ReferenceGroup;
        //    this._status = parameter.Status;
        //    this._value = parameter.Value;
        //    this._referencedValues = 1;
        //}

        public Parameter(string name, object parent) {
            this._definition = new ParameterDefinition(name, ParameterStatus.Local | ParameterStatus.Fixed, 0);
            this._definition.Name = name;
            this._parent = parent;
            this.Status = _definition.DefaultStatus;
            //this._bindingParameter = false;
            this._referenceGroup = 0;
            this._referencedValues = 1;
        }

        public Parameter(ParameterDefinition definition, object parent) {
            this._definition = definition;
            //this._owningComponent = owningComponent;
            this._parent = parent;
            if (definition.BindedStatus != ParameterStatus.None)
                this.Status = definition.BindedStatus;
            else
                this.Status = definition.DefaultStatus;
            //this._bindingParameter = false;
            this._referencedValues = 1;
        }
        
        public double Value {
            get {
                if (Definition.CalculateParameterValue != null && Parent is IComponent)
                    return Definition.CalculateParameterValue((IComponent)Parent, this);
                else {
                    if (_referencedParameter != null)
                        return _referencedParameter.Value;
                    else
                        return _value;
                }
            }
            //}

            set {
                if (_referencedParameter != null)
                    _referencedParameter.Value = value;
                _value = value;
                if (OnValueChange != null)
                    OnValueChange(this, null);
            }
        }

        public double SearchValue {
            get {
                if (u2sConversion != null)
                    return u2sConversion(ref _value, ref _minimum, ref _maximum);
                else
                    return _value;
            }
            set {
                if (s2uConversion != null)
                    this._value = s2uConversion(ref value, ref _minimum, ref _maximum);
                else
                    this._value = value;
            }
        }

        public double Error { 
            get {
                if (_referencedParameter != null)
                    return _referencedParameter.Error;
                else
                    return _error;
            }
            set {
                if (_referencedParameter != null)
                    _referencedParameter.Error = value;
                _error = value;
            }
        }

        public double SearchError {
            get {
                if (u2sConversion != null)
                    return u2sConversion(ref _error, ref _minimum, ref _maximum);
                else
                    return _error;
            }
            set {
                if (s2uConversion != null)
                    this._error = s2uConversion(ref value, ref _minimum, ref _maximum);
                else
                    this._error = value;
            }
        }

        //jesli wartosc jest konwertowana, delta ma wartosc stałą, określaną przy zmianie max/min
        //dlatego podstawienie pod SearchDelta nie odniesie skutku jeśli s2uConversion != null
        public double SearchDelta {
            get {
                if (_referencedParameter != null)
                    return _referencedParameter.SearchDelta;
                else {
                    //if (u2sConversion != null) {
                    //    double d1, d2, sd1, sd2;
                    //    d1 = _value;
                    //    d2 = (_value + _delta < _maximum) ? _value + _delta : _value - _delta;
                    //    sd1 = u2sConversion(ref d1, ref _minimum, ref _maximum);
                    //    sd2 = u2sConversion(ref d2, ref _minimum, ref _maximum);
                    //    return sd2 - sd1;
                    //} else
                        return _delta;
                }
            }
            set {
                if (_referencedParameter != null)
                    _referencedParameter.SearchDelta = value;
                //if (u2sConversion != null) {
                //    double d1, d2, sd1, sd2;
                //    sd1 = _value;
                //    //sd2 = _value + value;
                //    sd2 = (_value + value < _maximum) ? _value + value : _value - value; 
                //    d1 = u2sConversion(ref sd1, ref _minimum, ref _maximum);
                //    d2 = u2sConversion(ref sd2, ref _minimum, ref _maximum);
                //    _delta = sd2 - sd1;
                //} else

                //podstaw tylko jesli nie istnieje konwersja parametrow search'u miedzy parametrami uzytkownika
                //w przeciwnym wypadku wyznacz delte na podstawie konwersji search<->user
                if (u2sConversion == null)
                    _delta = value;
                else if (double.IsNaN(_delta))
                    SetNewDelta();
            }
        }

        public double Delta {
            get {
                if (_referencedParameter != null)
                    return _referencedParameter.Delta;
                else {
                    return _delta;
                }
            }
            set {
                if (_referencedParameter != null)
                    _referencedParameter.Delta = value;
               _delta = value; 
            }
        }

        public double Maximum {
            get { return _maximum; }
            set { 
                _maximum = value;
                if (!double.IsPositiveInfinity(_maximum))
                    if (!double.IsNegativeInfinity(_minimum)) {
                        this.s2uConversion = Parameter.s2uBothConversion;
                        this.u2sConversion = Parameter.u2sBothConversion;
                    } else {
                        this.s2uConversion = Parameter.s2uMaxConversion;
                        this.u2sConversion = Parameter.u2sMaxConversion;
                    }
                else
                    if (!double.IsNegativeInfinity(_minimum)) {
                        this.s2uConversion = Parameter.s2uMinConversion;
                        this.u2sConversion = Parameter.u2sMinConversion;
                    } else {
                        this.s2uConversion = null;
                        this.u2sConversion = null;
                    }
                this._delta = double.NaN;
            }
        }

        public double Minimum {
            get { return _minimum; }
            set {
                _minimum = value;
                if (!double.IsNegativeInfinity(_minimum))
                    if (!double.IsPositiveInfinity(_maximum)) {
                        this.s2uConversion = Parameter.s2uBothConversion;
                        this.u2sConversion = Parameter.u2sBothConversion;
                    } else {
                        this.s2uConversion = Parameter.s2uMinConversion;
                        this.u2sConversion = Parameter.u2sMinConversion;
                    } else
                    if (!double.IsPositiveInfinity(_maximum)) {
                        this.s2uConversion = Parameter.s2uMinConversion;
                        this.u2sConversion = Parameter.u2sMinConversion;
                    } else {
                        this.s2uConversion = null;
                        this.u2sConversion = null;
                    }
                this._delta = double.NaN;
            }
        }

        protected void SetNewDelta() {
            if (this.u2sConversion != null) {
                double u1, u2, s1, s2, value;
                if (!double.IsInfinity(this._minimum) && !double.IsInfinity(this._maximum))
                    value = (this._minimum + this._maximum) / 2;
                else
                    value = this._value;
                u1 = value;
                u2 = (value + _delta < _maximum) ? value + _delta : value - _delta;
                s1 = u2sConversion(ref u1, ref _minimum, ref _maximum);
                s2 = u2sConversion(ref u2, ref _minimum, ref _maximum);
                _delta = s2 - s1;
            }
        }

        public IParameter ReferencedParameter {
            get { return _referencedParameter; }
            set {
                if (this._referencedParameter != value) {
                    //zrywanie wiazania
                    if (this._referencedParameter != null) {
                        this._referencedParameter.ReferencedValues--;
                        //zapamietaj wartosc taką jaka funkcjonowała w wiązaniu
                        if (value == null)
                            this._value = this._referencedParameter.Value;
                    }
                    //dodawanie wiazania
                    if (value != null) {
                        IParameter p = value;
                        while (p != this && p != null) p = p.ReferencedParameter;
                        if (p == this) throw new ArgumentException("Circular reference");

                        value.ReferencedValues++;
                        //niech prywatna wartosc bedzie taka sama jak rodzica na wypadek przyszlego zerwania wiazania
                        this._value = value.Value;
                    }

                    _referencedParameter = value;
                }
            }
        }

        public short ReferencedValues {
            get { return this._referencedValues; }
            set { this._referencedValues = value; }
        }

        public bool HasReferenceValue {
            get {
                return _referencedParameter != null;
            }
        }

        public bool BindingParameter {
            get { return (this.Status & ParameterStatus.Binding) == ParameterStatus.Binding; }
            //set { this._bindingParameter = value; }
        }

        private static double s2uMinConversion(ref double value, ref double minimum, ref double maximum) {
            return Math.Exp(value) + minimum;
        }

        private static double u2sMinConversion(ref double value, ref double minimum, ref double maximum) {
            if (value <= minimum) value = minimum + 1e-10;
            return Math.Log(value - minimum);
        }

        private static double s2uMaxConversion(ref double value, ref double minimum, ref double maximum) {
            return -Math.Exp(-value) + maximum;
        }

        private static double u2sMaxConversion(ref double value, ref double minimum, ref double maximum) {
            if (value >= maximum) value = maximum - 1e-10; 
            return -Math.Log(-value + maximum);
        }

        private static double s2uBothConversion(ref double value, ref double minimum, ref double maximum) {
            return (minimum - maximum) / (1 + Math.Exp(value)) + maximum;
        }

        private static double u2sBothConversion(ref double value, ref double minimum, ref double maximum) {
            if (value <= minimum) value = minimum + 1e-10;
            else if (value >= maximum) value = (1-1e-10)*maximum;
            return Math.Log((minimum - value) / (value - maximum));
        }

    }
}
