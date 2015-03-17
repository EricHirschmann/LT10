using System;
using System.Collections.Generic;
using System.Text;

namespace Evel.gui {
    abstract class ChangeStep {

        protected string name;
        public GroupTabPage holder;

        public ChangeStep(string name, GroupTabPage holder) {
            this.name = name;
            this.holder = holder;
        }

        public string Name {
            get { return this.name; }
        }

        public abstract void Commit();

        public override string ToString() {
            return this.name;
        }

    }
}
