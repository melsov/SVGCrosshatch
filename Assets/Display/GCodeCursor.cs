using g3;
using SCGCode;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace SCDisplay
{
    public struct CursorUpdate
    {
        public Vector3f from, to;
    }

    public class GCodeCursor
    {
        VelocityV3f vv;

        List<Action<CursorUpdate>> subscribers = new List<Action<CursorUpdate>>();

        public void subscribe(Action<CursorUpdate> a) { if(!subscribers.Contains(a)) subscribers.Add(a); }

        public void moveTo(string gcodeLine) {

            VelocityV3f next = vv;
            foreach(GMoveComponent gmove in GCodeToVec3.FromLine(gcodeLine)) {
                next.apply(gmove);
            }
            if (!next.validPosition) { return; }
            foreach(var sub in subscribers) { sub(new CursorUpdate() { from = vv, to = next }); }
            vv = next;
        }
        
    }
}
