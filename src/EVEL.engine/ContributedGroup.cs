using System;
using System.Collections.Generic;
using System.Text;
using Evel.interfaces;

namespace Evel.engine {

    public class ContributedGroup : RawGroup {

        public Parameter contribution;
        public double groupArea;
        public double MemoryInt = 0; ///po normalizacji
                                 ///kiedy intensywności mają wejść do parametrów searchu
                                 ///tutaj przechowywana będzie intensywność pierwszej składowej
                                 ///brana później do obliczania doubleAsterixSum (suma intensywności wolnych
                                 ///w search'u)

        public ContributedGroup(GroupDefinition definition, ISpectrum owningSpectrum) {
            this._definition = definition;
            this._components = CreateComponents(0);
            contribution = new Parameter("contribution", this);
            this._owningSpectrum = owningSpectrum;
            CreateUniqueComponent(definition);
        }

        protected override Components CreateComponents(int count) {
            return new ExtComponents(this, count);
        }

        public override IParameter GetParameter(string address) {
            IParameter parameter;
            try {
                parameter = base.GetParameter(address);
            } catch {
                string[] coords = address.Split(ProjectBase.AddressDelimiters, StringSplitOptions.RemoveEmptyEntries);
                string parameterName = coords[3];
                if (parameterName == "contribution")
                    parameter = contribution;
                else
                    parameter = null;
            }
            return parameter;
        }

        public override void Dispose() {
            if (this.contribution.HasReferenceValue)
                this.contribution.ReferencedParameter.ReferencedValues--;
            base.Dispose();
        }

    }
}
