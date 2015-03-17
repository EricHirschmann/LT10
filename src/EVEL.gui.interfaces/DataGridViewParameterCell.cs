using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Evel.interfaces;
using System.Windows.Forms;

namespace Evel.gui.interfaces {
    public delegate void UserValueConversionHandler(IParameter parameter, ref double value);

    public class DataGridViewParameterCell : DataGridViewTextBoxCell {

        private double _backupValue;

        public UserValueConversionHandler ConvertFromUserValue;
        public UserValueConversionHandler ConvertToUserValue;
        public event EventHandler UserValueChanged;

        public DataGridViewParameterCell(IParameter parameter)
            : base() {
            this.parameter = parameter;
            this._backupValue = parameter.Value;
        }
        public override object Clone() {
            //Evel.engine.Parameter par = new Evel.engine.Parameter("unnamed", null);
            //par.Status = parameter.Status;
            //DataGridViewParameterCell cell = new DataGridViewParameterCell((IParameter)this.parameter.Clone());
            DataGridViewParameterCell cell = new DataGridViewParameterCell(this.parameter);
            cell.ConvertToUserValue += this.ConvertToUserValue;
            return cell;
        }
        private IParameter parameter;
        public double UserError {
            get {
                double d = parameter.Error;
                if (ConvertToUserValue != null)
                    ConvertToUserValue(parameter, ref d);
                return d;
            }
        }
        private double UserValue {
            get {
                double tmpValue;
                //if (this.DataGridView.ReadOnly)
                //    tmpValue = _backupValue;
                //else
                tmpValue = parameter.Value;
                if (ConvertToUserValue != null)
                    ConvertToUserValue(parameter, ref tmpValue);
                return tmpValue;
            }
            set {
                double v;
                v = value; //Value = 
                if (ConvertFromUserValue != null)
                    ConvertFromUserValue(parameter, ref v);
                parameter.Value = v;
                _backupValue = parameter.Value;
                if (UserValueChanged != null)
                    UserValueChanged(this, new EventArgs());
            }
        }
        public IParameter Parameter {
            get { return this.parameter; }
        }

        protected override object GetValue(int rowIndex) {
            base.GetValue(rowIndex);
            //return this.parameter.Value;
            return UserValue;
        }

        protected override bool SetValue(int rowIndex, object value) {
            double d;
            if (value != null)
                if (double.TryParse(value.ToString(), out d))
                    this.UserValue = d;
            return base.SetValue(rowIndex, value);
        }

        

    }
}
