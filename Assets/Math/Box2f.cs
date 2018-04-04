using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using VctorExtensions;

namespace g3
{
    public struct Box2f
    {
        public Vector2f min, max;

        public Vector2f size { get { return max - min; } }

        public void expandToContain(Vector2f v) {
            min = min.min(v);
            max = max.max(v);
        }

        public Vector2f lowerRight { get { return new Vector2f(max.x, min.y); } }

        public Vector2f upperLeft { get { return new Vector2f(min.x, max.y); } }

        public IEnumerable<Vector2f> getHorizontalLines(float yIncr = 1f) {
            for(float y = min.y; y < max.y; y += yIncr) {
                yield return new Vector2f(min.x, y);
                yield return new Vector2f(max.x, y);
            }
        }

        public bool tallerThanWide {
            get {
                return (max.y - min.y) > (max.x - min.x);
            }
        }

        public float smallerOverLargerDimension {
            get {
                if(tallerThanWide) {
                    return size.x / size.y; 
                } else {
                    return size.y / size.y;
                }
            }
        }

        public float Area {
            get { return size.x * size.y; }
        }

        public bool Contains(Vector2f v) {
            return v.grThan(min) && v.lessThan(max);
        }

        public float getFitScale(Vector2f reference) {
            Vector2f div = reference / size;
            return Mathf.Min(div.x, div.y);
        }

        public override string ToString() {
            return string.Format("ViewBox: min: {0} max: {1}", min.ToString(), max.ToString());
        }
    }
}
