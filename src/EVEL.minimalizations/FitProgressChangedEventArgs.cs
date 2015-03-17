using System;
using System.Collections.Generic;
using System.Text;
using System.ComponentModel;
using Evel.interfaces;

namespace Evel.engine.algorythms {

    public delegate void FitChangeEventHandler(object sender, FitProgressChangedEventArgs args);
    public delegate bool GetArrayHandler(object sender, double[] f);

    public class FitProgressChangedEventArgs : ProgressChangedEventArgs {
        private double _chisq;
        private int _iteration;
        private Exception _error;
        private object _target;
        private int _functionCalls;
        public double Chisq { get { return _chisq; } set { this._chisq = value; } }
        public int Iteration { get { return _iteration; }  set { this._iteration = value; } }
        public Exception Error { get { return _error; } set { this._error = value; } }
        public object Target { get { return _target; } set { this._target = value; } }
        public int FunctionCallCount { get { return _functionCalls; } set { _functionCalls = value; } }
        //public List<IParameter> Independencies;
        public FitProgressChangedEventArgs(double chisq, int iteration, Exception error, object target)
            : base(0, null) {
            this._chisq = chisq;
            this._error = error;
            this._iteration = iteration;
            this._target = target;
            this._functionCalls = 0;
            //this.Independencies = new List<IParameter>();
        }
    }
}
