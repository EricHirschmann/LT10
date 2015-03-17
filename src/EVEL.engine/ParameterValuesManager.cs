using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Evel.interfaces;
using System.Xml;

namespace Evel.engine.ParametersManagement {
    public class ParameterValuesManager {

        private IProject project;
        private List<ParameterValuesRecord> records;

        public ParameterValuesManager(IProject parentProject) {
            this.project = parentProject;
            this.records = new List<ParameterValuesRecord>();
        }

        public void Add() {
            throw new NotImplementedException();
        }

        public void Save(XmlWriter writer) {
            throw new NotImplementedException();
        }

    }
}
