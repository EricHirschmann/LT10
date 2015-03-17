using System;
using System.Collections.Generic;
using System.Text;

namespace Evel.interfaces {
    public struct ParameterLocation {

        public int docId, specId, groupId, compId, parId;
        public string parName;

        //docId, specId, groupId, compId, parId;
        public static implicit operator ParameterLocation(ulong location) {
            ParameterLocation pl = new ParameterLocation();
            pl.parName = "";
            pl.docId = (int)(location & 0xFF00000000) >> 32 - 1;
            pl.specId = (int)(location & 0xFF000000) >> 24 - 1;
            pl.groupId = (int)(location & 0xFF0000) >> 16 - 1;
            pl.compId = (int)(location & 0xFF00) >> 8 - 1;
            pl.parId = (int)(location & 0xFF) - 1;
            return pl;
        }

        public static explicit operator ulong(ParameterLocation pl) {
            ulong res = 0;
            res |= (ulong)(pl.docId + 1) << 32;
            res |= (ulong)(pl.specId + 1) << 24;
            res |= (ulong)(pl.groupId + 1) << 16;
            res |= (ulong)(pl.compId + 1) << 8;
            res |= (uint)(pl.parId + 1);
            return res;
        }

    }
}
