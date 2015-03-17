using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Evel.share {
    public class SimpleDisposable : IDisposable {
        #region IDisposable Members

        public void Dispose() {
            GC.SuppressFinalize(this);
        }

        #endregion
    }
}
