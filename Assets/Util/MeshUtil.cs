using UnityEngine;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using g3;

namespace SCUtil
{
    public static class MeshUtil
    {
        public class MeshData
        {
            public List<Vector3> verts = new List<Vector3>();
            public List<int> tris = new List<int>();
            public List<Color> cols = new List<Color>();
        }

        public static GameObject MakeGameObject(Mesh mesh, string name = "MeshedGO")
        {
            GameObject go = new GameObject(name);
            Material mat = Resources.Load<Material>("Materials/SimpleShowColors");

            var rendrr = go.AddComponent<MeshRenderer>();
            var filter = go.AddComponent<MeshFilter>();
            filter.mesh = mesh;
            rendrr.material = mat;

            Transform testMeshes = GameObject.Find("TestMeshes").transform;
            go.transform.position = testMeshes.position;
            go.transform.SetParent(testMeshes);

            return go;
        }

        public static Mesh Line(Vector2f from, Vector2f to,  Color col, float width = 2f)
        {
            Mesh mesh = new Mesh();

            var dif = to - from;
            var perp = Vector3.Cross(dif.Normalized.ToVector3(0f), Vector3.forward);

            mesh.vertices = new Vector3[] { from.ToVector3(0f), to.ToVector3(0f), from.ToVector3(0f) + perp * width };
            mesh.colors = new Color[] { col, col, col };
            mesh.triangles = from.x < to.x ? new int[] { 0, 1, 2 } : new int[] { 0, 2, 1 };

            return mesh;
        }

        public static void Line(ref MeshData m, Vector2f from, Vector2f to, ref int triOffset, Color col, float zOffset = 0f, float width = 2f)
        {
            var dif = to - from;
            var perp = Vector3.Cross(dif.Normalized.ToVector3(0f), Vector3.forward);

            var vertices = new Vector3[] {
                from.ToVector3(zOffset) - perp * width * .5f,
                to.ToVector3(zOffset) - perp * width * .5f,
                from.ToVector3(zOffset) + perp * width * .5f,
                to.ToVector3(zOffset) + perp * width * .5f };

            var vOrder = Vector3.Cross(vertices[0] - vertices[1], vertices[0] - vertices[2]);
            var triangles = vOrder.z < 0f ? new int[] { 0, 1, 2, 2, 1, 3 } : new int[] { 0, 2, 1, 2, 3, 1 };
            var colors = new Color[] { col, col, col, col };

            for(int i = 0; i < 4; ++i)
            {
                m.verts.Add(vertices[i]);
                m.cols.Add(colors[i]);
            }
            for (int i = 0; i < 6; ++i)
            {
                m.tris.Add(triangles[i] + triOffset);
            }
            triOffset += 4;
        }

        public static void LineSingleTriangle(ref MeshData m, Vector2f from, Vector2f to, ref int triOffset, Color col, float zOffset = 0f, float width = 2f)
        {
            var dif = to - from;
            var perp = Vector3.Cross(dif.Normalized.ToVector3(0f), Vector3.forward);

            var vertices = new Vector3[] { from.ToVector3(zOffset), to.ToVector3(zOffset), from.ToVector3(zOffset) + perp * width };

            var vOrder = Vector3.Cross(vertices[0] - vertices[1], vertices[0] - vertices[2]);
            var triangles = vOrder.z < 0f ? new int[] { 0, 1, 2 } : new int[] { 0, 2, 1 };
            var colors = new Color[] { col, col, col };

            for (int i = 0; i < 3; ++i)
            {
                m.verts.Add(vertices[i]);
                m.tris.Add(triangles[i] + triOffset);
                m.cols.Add(colors[i]);
            }
            triOffset += 3;
        }

    }
}
