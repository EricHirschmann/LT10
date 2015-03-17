using System;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;

namespace Evel.engine {
    public struct Lt10Version {

        public int v1, v2, v3, v4;

        public static bool operator >(Lt10Version obj1, Lt10Version obj2) {
            return obj1.v1 > obj2.v1 && obj1.v2 > obj2.v2 && obj1.v3 > obj2.v3 && obj1.v4 > obj2.v4;
        }

        public static bool operator <(Lt10Version obj1, Lt10Version obj2) {
            return obj1.v1 < obj2.v1 && obj1.v2 < obj2.v2 && obj1.v3 < obj2.v3 && obj1.v4 < obj2.v4;
        }

        public static Lt10Version Parse(string s) {
            Regex regex = new Regex(@"\d+\.\d+\.\d+\.\d+\.");
            if (regex.Match(s).Success) {
                Lt10Version v = new Lt10Version();
                v.v1 = 0;
                v.v2 = 0;
                v.v3 = 0;
                v.v4 = 0;
                return v;
            } else
                throw new ArgumentException();
                
        }

    }
}
