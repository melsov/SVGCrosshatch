using g3;
using System;
using UnityEngine;

namespace VctorExtensions
{
    public static class VctorExtensions
    {
		//Vec2 extensions
		public static Vector3 toVector3(this Vector2 v, float z) { return new Vector3(v.x, v.y, z); }

		public static Vector3 toVector3(this Vector2 v) { return v.toVector3(0f); }

		public static float dot(this Vector2 v, Vector2 other) { return v.x * other.x + v.y * other.y; }

		public static Vector2 scale(this Vector2 v, Vector2 other) { return new Vector2(v.x * other.x, v.y * other.y); }

        public static Vector2 divide(this Vector2 v, Vector2 other) { return new Vector2(v.x / other.x, v.y / other.y); }

        public static bool grThan(this Vector2 v, Vector2 other) { return v.x > other.x && v.y > other.y; }

        public static bool lessThan(this Vector2 v, Vector2 other) { return v.x < other.x && v.y < other.y; }

        public static Vector2 min(this Vector2 v, Vector2 other) { return new Vector2(Mathf.Min(v.x, other.x), Mathf.Min(v.y, other.y)); }

        public static Vector2 max(this Vector2 v, Vector2 other) { return new Vector2(Mathf.Max(v.x, other.x), Mathf.Max(v.y, other.y)); }

        public static Vector2 perp(this Vector2 v, bool turnRight = false) { return turnRight ?  new Vector2(v.y, -v.x) : new Vector2(-v.y, v.x); }

        //Vec3 extensions
        public static Vector2 xy(this Vector3 v) { return new Vector2(v.x, v.y); }

        public static Vector2f toVector2f(this Vector3 v) { return new Vector2f(v.x, v.y); }

        //Bounds extensions
        public static bool Contains2D(this Bounds b, Vector2 v) {
            return b.min.xy().lessThan(v) && b.max.xy().grThan(v);
        }

        //Vector3f 
        public static Vector2f toVector2f(this Vector3f v) { return new Vector2f(v.x, v.y); }

        //Vec2f extensions

        public static Vector2f min(this Vector2f v, Vector2f other) {
            return new Vector2f(Mathf.Min(v.x, other.x), Mathf.Min(v.y, other.y));
        }

        public static Vector2f max(this Vector2f v, Vector2f other) {
            return new Vector2f(Mathf.Max(v.x, other.x), Mathf.Max(v.y, other.y));
        }

        public static bool grThan(this Vector2f v, Vector2f other) { return v.x > other.x && v.y > other.y; }

        public static bool lessThan(this Vector2f v, Vector2f other) { return v.x < other.x && v.y < other.y; }



    }
	
}
