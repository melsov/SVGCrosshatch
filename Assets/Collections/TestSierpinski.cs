using UnityEngine;
using UnityEditor;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using g3;

namespace SCCollection
{
    public class TestSierpinski : MonoBehaviour
    {
        SierpinskiVectorTree stree;
        [SerializeField]
        bool shouldRedraw;

        [MenuItem("SC/ResetTestSierp")]
        static void ResetTestSierp()
        {
            var sierpTest = FindObjectOfType<TestSierpinski>();
            sierpTest.stree = null;
        }

        private void OnDrawGizmosSelected()
        {
            Box2f box = new Box2f()
            {
                min = new Vector2f(-16f, 0f),
                max = new Vector2f(16f, 32f)
            };

            Gizmos.color = Color.cyan;
            Box2f.GizmosDraw(box);

            stree = null;

            if(stree == null)
            {
                stree = SierpinskiVectorTree.TreeToContainBox(box.size, 16);

                foreach (var v in box.size.GridPoints(4f))
                {
                    var add = v * 1.5f + box.min - Vector2f.One * 4f; 
                    if (stree.root.Contains(add))
                    {
                        Gizmos.color = Color.red;
                    } else
                    {
                        Gizmos.color = Color.blue;
                    }

                    stree.Add(add);
                }
            }

            var lists = stree.GetAll();
            float lerp = 0f;
            float lerpIncr = 1f / 8f;

            var tris = stree.GetChildren();
            Vector3 zOff = Vector3.zero;
            foreach(var tri in tris)
            {
                Gizmos.color = Color.Lerp(tri.inverted ? Color.red : Color.yellow, tri.inverted ? Color.blue : Color.white, lerp);

                var triData = tri.GetData().ToList();
                if(triData.Count == 0) { continue; }

                foreach(var v in triData)
                {
                    Gizmos.DrawSphere(v + zOff, .3f);
                }

                var points = tri.GetTriPoints();
                for(int i = 1; i <= points.Count; ++i)
                {
                    Gizmos.DrawLine(points[i - 1] + zOff, points[i % points.Count] + zOff);
                }

                zOff.z += .3f;
                lerp += lerpIncr;
                lerp = lerp > 1 ? 0 : lerp;
            }

        }
    }
}
