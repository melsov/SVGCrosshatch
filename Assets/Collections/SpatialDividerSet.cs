using g3;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace SCCollection
{
    public abstract class Cubbie
    {
        public Vector2i pos;
    }

    public class SpatialDividerSet 
    {

    }

    public class IsoTrianglei
    {
        public Vector2f _base;
        public Vector2f dims;

        public bool inverted;

        List<IsoTrianglei> children;
        List<Vector2f> data = new List<Vector2f>();

        float up { get { return inverted ? -1f : 1f; } }
        
        Vector2f lowerLeftCorner {
            get {
                return new Vector2f(_base.x - dims.x / 2, _base.y - dims.y * (inverted ? 1f : 0f));
            }
        }

        Vector2f upperRightCorner {
            get {
                return new Vector2f(_base.x + dims.x / 2, _base.y + dims.y * (inverted ? 0f : 1f));
            }
        }

        Vector2f halfXDims {
            get {
                return new Vector2f(dims.x / 2f, dims.y);
            }
        }

        Vector2f baseLeft {
            get {
                return new Vector2f(_base.x - dims.x / 2, _base.y);
            }
        }

        Vector2f baseRight {
            get {
                return new Vector2f(_base.x + dims.x / 2, _base.y);
            }
        }

        Vector2f apex {
            get {
                return new Vector2f(_base.x, _base.y + (inverted ? -dims.y : dims.y));
            }
        }

        public bool Contains(Vector2f v)
        {

            if(v.EitherComponentLessThan(lowerLeftCorner))
            {
                return false;
            }
            if(v.EitherComponentGreaterThan(upperRightCorner))
            {
                return false;
            }


            Vector2f dif;
            if(v.x > _base.x)
            {
                dif = v - baseRight;
            } else
            {
                dif = v - baseLeft;
            }

            return Mathf.Abs(dif.SafeSlope) < halfXDims.Slope;

        }

        public List<Vector2f> GetTriPoints()
        {
            Vector2f inv = inverted ? new Vector2f(0f, dims.y) : Vector2f.Zero;
            return new List<Vector2f>()
            {
                baseLeft,
                baseRight,
                apex,

            };
        }

        public bool isNode()
        {
            return children != null && children.Count == 4;
        }

        public int Count {
            get { return data.Count; }
        }

        public Vector2f this[int i] {
            get {
                return data[i];
            }
        }

        public void Divide()
        {
            var nextBase = _base;

            nextBase.x -= dims.x / 4;
            var ll = new IsoTrianglei { dims = dims / 2f, _base = nextBase, inverted = inverted };

            nextBase.x = _base.x;
            var imiddle = new IsoTrianglei { dims = dims / 2f, _base = nextBase + new Vector2f(0, dims.y / 2) * (inverted ? -1 : 1), inverted = !inverted };
            var iupper = new IsoTrianglei { dims = dims / 2f, _base = nextBase + new Vector2f(0, dims.y / 2) * (inverted ? -1 : 1), inverted = inverted };

            nextBase.x += dims.x / 4;
            var lr = new IsoTrianglei { dims = dims / 2, _base = nextBase, inverted = inverted };

            children = new List<IsoTrianglei>();

            children.Add(ll);
            children.Add(imiddle);
            children.Add(iupper);
            children.Add(lr);

            int DBUGLost = 0;
            foreach(var v in data)
            {
                bool lostOne = true;
                foreach(var c in children)
                {
                    if (c.Contains(v))
                    {
                        c.data.Add(v);
                        lostOne = false;
                        break;
                    }
                }
                if(lostOne)
                {
                    DBUGLost++;
                    //Gizmos.color = Color.magenta;
                    //Gizmos.DrawWireCube(v, Vector3.one * .4f);
                }
            }

            if(DBUGLost > 0)
                Debug.LogWarning("lost data: " + DBUGLost);

            data.Clear();
        }

        public void DivideR(int splitMax)
        {
            Stack<IsoTrianglei> s = new Stack<IsoTrianglei>();
            s.Push(this);
            while(s.Count > 0)
            {
                var tri = s.Pop();
                if (!tri.isNode())
                {
                    if (tri.data.Count > splitMax)
                    {
                        tri.Divide();
                    }

                }

                if (tri.isNode())
                {
                    foreach (var c in tri.children)
                    {
                        s.Push(c);
                    }
                }

            }
        }



        public IEnumerable<Vector2f> GetData()
        {
            foreach(var v in data) { yield return v; }
        }

        public List<List<Vector2f>> GetAll()
        {
            List<List<Vector2f>> result = new List<List<Vector2f>>();
            Stack<IsoTrianglei> s = new Stack<IsoTrianglei>();
            s.Push(this);
            while (s.Count > 0)
            {
                var tri = s.Pop();
                if (tri.isNode())
                {
                    foreach (var c in tri.children)
                    {
                        s.Push(c);
                    }
                }
                else
                {
                    result.Add(tri.data);
                }
            }

            return result;
        }

        public List<IsoTrianglei> GetChildrenR()
        {
            var result = new List<IsoTrianglei>();
            Stack<IsoTrianglei> s = new Stack<IsoTrianglei>();
            s.Push(this);
            while (s.Count > 0)
            {
                var tri = s.Pop();
                result.Add(tri);
                if (tri.isNode())
                {
                    foreach (var c in tri.children)
                    {
                        s.Push(c);
                    }
                }
               
            }

            return result;
        }

        public List<IsoTrianglei> GetLeaves()
        {
            var result = new List<IsoTrianglei>();
            Stack<IsoTrianglei> s = new Stack<IsoTrianglei>();
            s.Push(this);
            while (s.Count > 0)
            {
                var tri = s.Pop();
                if (tri.isNode())
                {
                    foreach (var c in tri.children)
                    {
                        s.Push(c);
                    }
                }
                else
                {
                    result.Add(tri);
                }

            }

            return result;
        }

        public IsoTrianglei Add(Vector2f v)
        {
            if (Contains(v))
            {
                if (isNode())
                {
                    foreach (var c in children)
                    {
                        if (c.Contains(v))
                        {
                            return c.Add(v);
                        }
                    }
                } 
                else
                {
                    data.Add(v);
                    return this;
                }
            }
            Debug.Log("contains ? " + Contains(v));
            return null;
        }

        
    }

    public class SierpinskiVectorTree
    {
        public IsoTrianglei root { get; private set; }
        private int splitMax;

        public SierpinskiVectorTree(Vector2f _base, Vector2f dims, int splitMax = 20)
        {
            root = new IsoTrianglei { _base = _base, dims = dims };
            this.splitMax = splitMax;
        }

        public static SierpinskiVectorTree TreeToContainBox(Vector2f dims, int splitMax = 20)
        {
            return TreeToContainBox(new Box2f { min = new Vector2f(-dims.x / 2f, 0), max = new Vector2f(dims.x / 2f, dims.y) }, splitMax);
        }
        public static SierpinskiVectorTree TreeToContainBox(Box2f box, int splitMax = 20)
        {
            double slope = -Math.Tan(Math.PI / 3d);
            double size = box.size.y - (box.size.x / 2f) * slope;

            int si = (int)(size + 1.5f);
            return new SierpinskiVectorTree(box.center - Vector2f.AxisY * box.size.y / 2f, new Vector2i(si, si), splitMax);
        }

        public void Add(Vector2f v)
        {
            IsoTrianglei tri = root.Add(v);

            if (tri != null && tri.Count > splitMax)
            {
                tri.DivideR(splitMax);
            }
        }

        public void DebugDivide(int times = 1)
        {
            for(int i=0; i<times; ++i)
            {
                var leaves = root.GetLeaves();
                foreach(var l in leaves)
                {
                    l.Divide();
                }
            }
        }

        public List<List<Vector2f>> GetAll()
        {
            return root.GetAll();
        }

        public List<IsoTrianglei> GetChildren()
        {
            return root.GetChildrenR();
        }

        public List<IsoTrianglei> GetLeaves()
        {
            return root.GetLeaves();
        }
    }
}
