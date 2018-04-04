using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace g3
{
    public static class SCVecUtil
    {
        public static Vector2f directionForFractionOfPi(int wedge, int wedges) {
            return directionForFractionOfPi(wedge / (float)wedges);
        }

        public static Vector2f directionForFractionOfPi(float frac) {
            return new Vector2f(Mathf.Cos(frac), Mathf.Sin(frac));
        }
    }
}
