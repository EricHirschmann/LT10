using System;
using System.Collections.Generic;
using System.Text;
using Evel.interfaces;

namespace Evel.engine.algorythms {
    public interface IFitter {

        event FitChangeEventHandler Changed;
        //event FitChangeEventHandler Finished;
        event IndependencyFoundEventHandler IndependencyFound;
        double[] DiffsArray { get; set; }
        double[] Parameters { get; set; }
        //double[] Delta { get; set; }
        double[] Error { get; }
        GetArrayHandler Function { get; set; }
        void SetParameters(IParameter[] parameters, double[] diffs, int dataLength, GetArrayHandler Function);
        void SetParameters();
        double fit(object target, bool emptyRun);
        //double fit(object target);
        int StartChannel { get; set; }
        int DataLength { get; set; }
        int MaxIterationCount { get; set; }
        int Iteration { get; set; }

    }
}
