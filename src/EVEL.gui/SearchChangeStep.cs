using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Evel.engine.parametersImport;
using Evel.interfaces;

namespace Evel.gui {
    class SearchChangeStep : ChangeStep {

        private ParameterValuesRecord record;
        List<ISpectrum> spectra;
        ProjectForm projectForm;

        public SearchChangeStep(ProjectForm projectForm, string name, List<ISpectrum> spectra)
            : base(name, null) {
            this.spectra = new List<ISpectrum>(spectra);
            this.record = new ParameterValuesRecord(spectra);
            this.projectForm = projectForm;
        }

        public override void Commit() {
            ParameterValuesRecord tmpRecord = new ParameterValuesRecord(this.spectra);
            for (int i = 0; i < this.spectra.Count; i++)
                this.record.FillSpectrum(this.spectra[i], 0);
            record = tmpRecord;
            projectForm.RepaintGrids();
        }

    }
}
