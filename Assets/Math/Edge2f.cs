using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace g3
{
    public struct Edge2f
    {
        public Vector2f a, b;

        public static Edge2f Zero() { return new Edge2f(Vector2f.Zero, Vector2f.Zero); }

        public Edge2f(Vector2f a, Vector2f b) {
            this.a = a; this.b = b;
        }

        public static Edge2f operator +(Edge2f e, Vector2f v) { return new Edge2f(e.a + v, e.b + v); }

        public Vector2f lesserY { get { return a.y < b.y ? a : b; } }

        public Vector2f greaterY { get { return a.y < b.y ? b : a; } }

        public Segment2f toSegment2f() { return new Segment2f(a, b); }

        public float LengthSquared { get { return (a - b).LengthSquared; } }

        public float Length { get { return (a - b).Length; } }

        public float fastDistanceSquared(Vector2f v) {
            return Segment2f.FastDistanceSquared(ref a,ref b,ref v);
        }

        public bool spansY(float y) {
            if(a.y > b.y) {
                return b.y < y && y < a.y;
            } else {
                return a.y < y && y < b.y;
            }
        }

        public Vector2f difference {
            get { return b - a; }
        }

        public bool intersectionPointWithY(float y, out Vector2f intersectionPoint, float epsilonLower = 0f, float epsilonUpper = 0f) {
            Vector2f lowY = lesserY; Vector2f highY = greaterY;

            if (lowY.y - epsilonLower < y && y < highY.y + epsilonUpper) {
                Vector2f dif = highY - lowY;
                intersectionPoint = new Vector2f(lowY.x + (y - lowY.y) * dif.x / dif.y, y);
                return true;
            }
            intersectionPoint = Vector2f.Zero;
            return false;
        }

        public Vector2f intersectionWithLineAtY(float y) {
            Vector2f result;
            bool inter = intersectionPointWithY(y, out result, 99999999f, 99999999f);
            if(!inter) { Debug.LogWarning("no inter for y: " + y); }
            return result;
            //Vector2f dif = b - a;
            //return new Vector2f(a.x + (y - a.y) * dif.x / dif.y, y);
        }

        public override string ToString() {
            return string.Format("Edge2f: {0} . {1}", lesserY, greaterY);
        }
    }

    public struct Corner2f
    {
        public Vector2f apex, a, b;

        public static Corner2f Zero() { return new Corner2f(Vector2f.Zero, Vector2f.Zero, Vector2f.Zero); }

        public Corner2f(Vector2f apex, Vector2f a, Vector2f b) {
            this.apex = apex; this.a = a; this.b = a;
        }


    }
}
