using Evel.interfaces;

namespace Evel.engine {
    public class ExtComponent : Component {

        public double IntInCounts { get; set; }

        public ExtComponent(ParameterDefinition[] parameters, bool createUnique, object parent)
            : base(parameters, createUnique, parent) {
            this.IntInCounts = 0;
        }

    }
}
