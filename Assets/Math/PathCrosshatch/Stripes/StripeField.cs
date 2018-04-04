using g3;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace SCGenerator
{

    public class NullableMat2f
    {
        public Matrix2f mat;

        public static implicit operator Matrix2f(NullableMat2f nm) { return nm.mat; }
    }

    public class StripeField
    {
        public Vector2f origin;
        public float interval;
        //public Vector2f direction;
        public float angleRadians;
        public StripeField next;

        private NullableMat2f _rotation;
        public Matrix2f rotation {
            get {
                if (_rotation == null) {
                    _rotation = new NullableMat2f()
                    {
                        mat = new Matrix2f(angleRadians) // Mathf.Atan2(direction.y, direction.x))
                    };
                }
                return _rotation.mat;
            }
        }

        public float nextStripeYPosAbove(float minY, float epsilon = 0.01f) {
            if(minY < 0f) {
                return minY - (minY % interval);
            }
            return minY + (interval - (minY % interval));
        }
    }
}
