using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;

namespace Evel.share {
    public class ValuesDictionary : DictionaryBase {

        private static List<Type> acceptableTypes = null;

        public ValuesDictionary() {
            if (acceptableTypes == null) {
                acceptableTypes = new List<Type>();
                acceptableTypes.Add(typeof(double));
                acceptableTypes.Add(typeof(int));
                acceptableTypes.Add(typeof(bool));
                acceptableTypes.Add(typeof(object));
            }
        }

        public ICollection Keys {
            get {
                return Dictionary.Keys;
            }
        }

        public ICollection Values {
            get {
                return Dictionary.Values;
            }
        }

        public void Add(string key, object value) {
            Dictionary.Add(key, value);
        }

        public object this[string index] {
            get { return Dictionary[index]; }
            set { Dictionary[index] = value; }
        }

        protected override void OnRemove(object key, object value) {
            if (key.GetType() != typeof(String)) {
                throw new ArgumentException("key must be of type String", "key");
            }
            /*if (!acceptableTypes.Contains(value.GetType())) {
                throw new ArgumentException("value must be of one of the following types: object, double, int or bool");
            } */            
        }
        
        protected override void OnInsert(object key, object value) {
            if (key.GetType() != typeof(String)) {
                throw new ArgumentException("key must be of type String", "key");
            }
            /*if (!acceptableTypes.Contains(value.GetType())) {
                throw new ArgumentException("value must be of one of the following types: object, double, int or bool");
            }   */         
        } 

        protected override void OnSet(object key, object oldValue, object newValue) {
            if (key.GetType() != typeof(String)) {
                throw new ArgumentException("key must be of type String", "key");
            }
            /*if (!acceptableTypes.Contains(oldValue.GetType()) || !acceptableTypes.Contains(newValue.GetType())) {
                throw new ArgumentException("either oldValue and newValue must be of one of the following types: object, double, int or bool");
            }*/
        }

    }
}
