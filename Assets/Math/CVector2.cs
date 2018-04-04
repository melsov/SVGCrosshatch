using UnityEngine;

namespace g3
{
    public struct CVector2
    {
        public Color32 color;
        public Vector2d v;

        public CVector2(Color32 color, Vector2d v) {
            this.color = color; this.v = v;
        }
    }
}
