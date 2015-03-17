using System;
using System.Collections.Generic;
using System.Text;
using Evel.interfaces;

namespace Evel.engine {

    public class ExtComponents : Components {

        protected override IComponent createNewComponent(ParameterDefinition[] parameters) {
            return new ExtComponent(parameters, false, this);
        }

        public ExtComponents(IGroup parentGroup, int componentCount)
            : base(parentGroup, componentCount) {
        }
    }
}
