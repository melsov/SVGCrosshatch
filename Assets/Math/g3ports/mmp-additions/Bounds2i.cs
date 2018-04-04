using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace g3
{
    public struct Bounds2i
    {
        public Vector2i start;
        public Vector2i extent;

        public Bounds2i(Vector2i start, Vector2i extent) {
            this.start = start; this.extent = extent;
        }
        
        public static Bounds2i FromTexture2D(Texture2D tex) {
            return new Bounds2i(Vector2i.Zero, new Vector2i(tex.width, tex.height));
        }

        public bool contains(Vector2i v) {
            return extent.greatrThan(v) && start.lessThan(v);
        }

        public bool containsInclusive(Vector2i v) {
            return extent.greatrThan(v) && start.lessThanOrEqualTo(v);
        }

        public Vector2i size {
            get {
                return extent - start;
            }
        }
        
    }
}
