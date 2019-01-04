using CrosshatchC.NET.GCMath;
using g3;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace SCCollection
{

    public abstract class IsoTriData
    {
        protected Vector2f _v;
        public Vector2f v { get { return _v; } }

        public IsoTriData(Vector2f _v)
        {
            this._v = _v;
        }

        public abstract float GetValue01();
    }

    public struct PixelV
    {
        public Vector2f pos;
        public Color color;
    }

    public class PixelTriData : IsoTriData
    {
        private PixelV pixel;

        public PixelTriData(PixelV _pixel) : base(_pixel.pos)
        {
            this.pixel = _pixel;
        }

        public override float GetValue01()
        {
            return pixel.color.grayscale;
        }
    }

    public class IsoTriangle<T> where T : IsoTriData
    {
        public Vector2f _base;
        public Vector2f dims;

        public bool inverted;

        List<IsoTriangle<T>> children;
        List<T> data = new List<T>();

        // limiting min size to reasonable
        // for groups of pixels 
        // (Granted) this breaks general purpose use
        public float MinBaseSize = 2f;

        public const float RootThree = 1.73205080757f;
        public const float CircumradiusOne = RootThree / 3f;

        public float Circumradius {
            get { return dims.x * CircumradiusOne; }
        }

        public float InnerRadius {
            get { return dims.x * CircumradiusOne / 2f; }
        }

        OnlineNormalEstimator _onlineNormalEstimator = new OnlineNormalEstimator();
        public OnlineNormalEstimator onlineNormalEstimator { get { return _onlineNormalEstimator; } }

        public float up { get { return inverted ? -1f : 1f; } }

        #region point-access

        public Vector2f Centroid {
            get {
                return new Vector2f(_base.x, _base.y + dims.y / 3f * (inverted ? -1f : 1f));
            }
        }

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

        public Vector2f center {
            get {
                return new Vector2f(_base.x, _base.y + (inverted ? -dims.y : dims.y) / 2f);
            }
        }

        public List<Vector2f> Border(bool closed = false)
        {
            if (closed)
            {
                return new List<Vector2f>() { baseLeft, baseRight, apex, baseLeft };
            }
            else
            {
                return new List<Vector2f>() { baseLeft, baseRight, apex };
            }
        }

        public Vector2f[] CircumRadii()
        {
            var border = Border();
            var result = new Vector2f[3];
            var centroid = Centroid;
            for(int i=0; i < 3; ++i)
            {
                result[i] = border[i] - centroid;
            }
            return result;
        }

        #endregion

        #region queries

        public void ClosestPoint(Vector2f v, out Vector2f closest, out Vector2f otherA, out Vector2f otherB)
        {
            var ps = Border();
            float dist = float.MaxValue;
            int result = 0;
            for(int i = 0; i < 3; ++i)
            {
                var p = ps[i];
                if((p - v).LengthSquared < dist)
                {
                    dist = (p - v).LengthSquared;
                    result = i;
                }
            }
            closest = ps[result];
            otherA = ps[(result + 1) % 3];
            otherB = ps[(result + 2) % 3];
        }

        public int ClosestBorderIndex(Vector2f v)
        {
            var ps = Border();
            float dist = float.MaxValue;
            int result = 0;
            for (int i = 0; i < 3; ++i)
            {
                var p = ps[i];
                if ((p - v).LengthSquared < dist)
                {
                    dist = (p - v).LengthSquared;
                    result = i;
                }
            }
            return result;
        }

        public bool IsTouching(IsoTriangle<T> other, out Vector2f touchPoint,  float epsilon = 0.001f)
        {
            //DEBUG (sanity check)
            if(center.Distance(other.center) > Circumradius + other.Circumradius)
            {
                touchPoint = Vector2f.One * 100f;
                return false;
            }
            //END DBUG

            IsoTriangle<T> larger, smaller;
            if(other.dims.x < dims.x)
            {
                smaller = other;
                larger = this;
            } else
            {
                smaller = this;
                larger = other;
            }
            var _border = smaller.Border();
            float smallestMag = float.MaxValue;
            touchPoint = _border[0];

            for(int i = 0; i < 3; ++i)
            {
                // is touching if a slightly nudged point of other
                // is inside this tri
                var dif = larger.center - _border[i];
                if(dif.LengthSquared < smallestMag)
                {
                    smallestMag = dif.LengthSquared;
                    touchPoint = _border[i];
                }

                //if(dif.LengthSquared < epsilon)
                //{
                //    touchPoint = _border[i];
                //    return true;
                //}

                var touch = _border[i] + dif * epsilon; // -Mathf.Min(other.dims.x, dims.x) / 4f;
                if (larger.Contains(touch))
                {
                    touchPoint = touch;
                    return true;
                }
            }

            return false;
        }

        public float Area {
            get { return dims.x * dims.y; }
        }

        public float Filled {
            get { return data.Count / Area; }
        }

        public bool Contains(Vector2f v)
        {

            if (v.EitherComponentLessThan(lowerLeftCorner))
            {
                return false;
            }
            if (v.EitherComponentGreaterThan(upperRightCorner))
            {
                return false;
            }


            Vector2f dif;
            if (v.x > _base.x)
            {
                dif = v - baseRight;
            }
            else
            {
                dif = v - baseLeft;
            }

            return Mathf.Abs(dif.SafeSlope) < halfXDims.Slope;

        }

        public List<Vector2f> GetTriPoints()
        {
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

        public T this[int i] {
            get {
                return data[i];
            }
        }

        #endregion

        enum ChildOrder { LowerLeft, LowerRight, Upper, Middle }

        public void Divide()
        {
            var nextBase = _base;

            nextBase.x -= dims.x / 4;
            var ll = new IsoTriangle<T> { dims = dims / 2f, _base = nextBase, inverted = inverted };

            nextBase.x = _base.x;
            var imiddle = new IsoTriangle<T> { dims = dims / 2f, _base = nextBase + new Vector2f(0, dims.y / 2) * (inverted ? -1 : 1), inverted = !inverted };
            var iupper = new IsoTriangle<T> { dims = dims / 2f, _base = nextBase + new Vector2f(0, dims.y / 2) * (inverted ? -1 : 1), inverted = inverted };

            nextBase.x += dims.x / 4;
            var lr = new IsoTriangle<T> { dims = dims / 2, _base = nextBase, inverted = inverted };

            //TODO: does children being an array break someting?
            //children = new IsoTriangle<T>[4]; // new List<IsoTriangle<T>>();
            children = new List<IsoTriangle<T>>();
            for (int i = 0; i < 4; ++i) children.Add(null);

            children[(int)ChildOrder.LowerLeft] = ll;
            children[(int)ChildOrder.LowerRight] = lr;
            children[(int)ChildOrder.Upper] = iupper;
            children[(int)ChildOrder.Middle] = imiddle;

            int DBUGLost = 0;

            foreach (var v in data)
            {
                
                if(imiddle.Contains(v.v))
                {
                    imiddle.Incorporate(v);
                }
                else
                {
                    var _center = center;
                    if((_center.y < v.v.y) == !inverted)
                    {
                        iupper.Incorporate(v);
                    } 
                    else
                    {
                        if(_center.x > v.v.x)
                        {
                            ll.Incorporate(v);
                        }else
                        {
                            lr.Incorporate(v);
                        }
                    }
                }
                
            }

            if (DBUGLost > 0)
                Debug.LogWarning("lost data: " + DBUGLost);

            data.Clear();
        }

      
        bool shouldDivide(double deviationThreshold)
        {
            return dims.x > MinBaseSize && Count > 4 && onlineNormalEstimator.standardDeviationUnbiased() > deviationThreshold;
        }

        public void DivideR(double deviationThreshold)
        {
            DivideR((IsoTriangle<T> tri) =>
            {
                return tri.shouldDivide(deviationThreshold);
            });
        }

        public void DivideR(Func<IsoTriangle<T>, bool> shouldDivide)
        {
            Stack<IsoTriangle<T>> s = new Stack<IsoTriangle<T>>();
            s.Push(this);
            while (s.Count > 0)
            {
                var tri = s.Pop();
                if (!tri.isNode())
                {
                    if (shouldDivide(tri))// tri.shouldDivide(deviationThreshold))
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



        public IEnumerable<T> GetData()
        {
            foreach (var v in data) { yield return v; }
        }


        internal List<List<Vector2f>> GetAllTriBorders(bool closeBorder = false)
        {
            List<List<Vector2f>> result = new List<List<Vector2f>>();
            Stack<IsoTriangle<T>> s = new Stack<IsoTriangle<T>>();
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
                    result.Add(tri.Border(closeBorder));
                }
            }

            return result;
        }

        public List<List<T>> GetAll()
        {
            List<List<T>> result = new List<List<T>>();
            Stack<IsoTriangle<T>> s = new Stack<IsoTriangle<T>>();
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

        public List<IsoTriangle<T>> GetChildrenR()
        {
            var result = new List<IsoTriangle<T>>();
            Stack<IsoTriangle<T>> s = new Stack<IsoTriangle<T>>();
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

        public List<IsoTriangle<T>> GetLeaves(bool nonEmpty = false)
        {
            var result = new List<IsoTriangle<T>>();
            Stack<IsoTriangle<T>> s = new Stack<IsoTriangle<T>>();
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
                    if(!nonEmpty || tri.Count > 0)
                        result.Add(tri);
                }

            }

            return result;
        }

        public int GetLeafCount()
        {
            return GetLeaves().Count;
        }

        public int GetNonEmptyLeafCount()
        {
            return GetLeaves(true).Count;
        }

        public IsoTriangle<T> Add(T v)
        {
            if (Contains(v.v))
            {
                if (isNode())
                {
                    foreach (var c in children)
                    {
                        if (c.Contains(v.v))
                        {
                            return c.Add(v);
                        }
                    }
                }
                else
                {
                    Incorporate(v);
                    return this;
                }
            }

            return null;
        }

        void Incorporate(T v)
        {
            data.Add(v);
            _onlineNormalEstimator.handle(v.GetValue01());
        }

        public void CullDataWithMaxMean(float maxMean)
        {
            var s = new Stack<IsoTriangle<T>>();
            s.Push(this);
            while (s.Count > 0)
            {
                var tri = s.Pop();
                if(tri.isNode())
                {
                    foreach (var c in tri.children) s.Push(c);
                } else
                {
                    if(tri.onlineNormalEstimator.mean() > maxMean)
                    {
                        tri.data.Clear();
                    }
                }
            }

            //TODO: clear out empty nodes
        }

        /// <summary>
        /// Split leaves into more densely populated child leaves,
        /// if a leaf's 'Filled' property is less than filledThreshold
        /// </summary>
        /// <param name="filledThreshold">the threshold</param>
        /// <param name="maxDepth">max number of recursive splits to perform</param>
        public void SplitUnderPopulatedLeaves(float filledThreshold, int maxSplitDepth)
        {
            SplitLeaves(maxSplitDepth, (IsoTriangle<T> tri) =>
            {
                return tri.Filled < filledThreshold && tri.dims.x > MinBaseSize;
            });
        }

        public void SplitLeaves(int maxSplitDepth, Func<IsoTriangle<T>, bool> shouldSplit)
        {
            var s = new Stack<DepthLeaf>();
            s.Push(new DepthLeaf( this, maxSplitDepth));
            while (s.Count > 0)
            {
                var dl = s.Pop();
                
                if (dl.tri.isNode())
                {
                    foreach (var c in dl.tri.children) s.Push(new DepthLeaf(c, dl.depth));
                }
                else
                {
                    if (dl.depth > 0)
                    {
                        if(shouldSplit(dl.tri))
                        {
                            dl.tri.Divide();
                            foreach (var c in dl.tri.children) s.Push(new DepthLeaf(c, dl.depth - 1));
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Split leaves into more densely populated child leaves,
        /// if a leaf's 'Filled' property is less than filledThreshold
        /// </summary>
        /// <param name="filledThreshold"></param>
        /// <param name="maxSplitDepth"></param>
        /// <returns>a list of all non empty leaves</returns>
        public List<IsoTriangle<T>> SplitUnderPopulatedLeavesToList(float filledThreshold, int maxSplitDepth)
        {
            var result = new List<IsoTriangle<T>>();
            var s = new Stack<DepthLeaf>();
            s.Push(new DepthLeaf(this, maxSplitDepth));
            while (s.Count > 0)
            {
                var dl = s.Pop();

                if (dl.tri.isNode())
                {
                    foreach (var c in dl.tri.children) s.Push(new DepthLeaf(c, dl.depth));
                }
                else
                {
                    if (dl.depth > 0)
                    {
                        if (dl.tri.Filled < filledThreshold)
                        {
                            dl.tri.Divide();
                            foreach (var c in dl.tri.children) s.Push(new DepthLeaf(c, dl.depth - 1));
                        } 
                        else
                        {
                            result.Add(dl.tri);
                        }
                    }
                    else
                    {
                        if(dl.tri.data.Count > 0)
                            result.Add(dl.tri);
                    }
                }
            }
            return result;
        }

        public struct DepthLeaf
        {
            public IsoTriangle<T> tri;
            public int depth;

            public DepthLeaf(IsoTriangle<T> tri, int depth)
            {
                this.tri = tri; this.depth = depth;
            }
        }



        /// <summary>
        /// Returns a list of sub trees
        /// with greater than zero and less than maxLeaves
        /// non-empty (data.Count > 0) child leaves
        /// </summary>
        /// <param name="maxLeaves"></param>
        /// <returns></returns>
        public List<IsoTriangle<T>> NonEmptyChildrenWithMaxLeaves(int maxLeaves)
        {
            var result = new List<IsoTriangle<T>>();
            var s = new Stack<IsoTriangle<T>>();
            s.Push(this);
            while(s.Count > 0)
            {
                var tri = s.Pop();
                int count = tri.GetNonEmptyLeafCount();
                if(count > 0 && count < maxLeaves)
                {
                    result.Add(tri);
                }
                else if (tri.isNode())
                {
                    foreach(var c in tri.children)
                    {
                        s.Push(c);
                    }
                }
            }
            return result;
        }

    

        public Mesh ToMesh()
        {
            var leaves = GetLeaves();// GetChildrenR();
            var vecs = new Vector3[leaves.Count * 3];
            var uvs = new Vector2[leaves.Count * 3];
            var tris = new int[leaves.Count * 3];
            var colors = new Color[leaves.Count * 3];

            int tindex;
            double val;
            float c;
            var uv3 = new Vector2[]
            {
                new Vector2(0f, 0f), //left
                new Vector2(1f, 0f), // right
                new Vector2(.5f, 1f), //apex
            };
            var triI = new int[] { 0, 2, 1 };
            for(int i=0;i<leaves.Count; ++i)
            {
                var border = leaves[i].Border();
                val =  leaves[i].onlineNormalEstimator.mean();
                float alpha = leaves[i].data.Count > 0 ? 1f : .4f;
                for(int j=0; j < 3; ++j)
                {
                    tindex = i * 3 + j;
                    vecs[tindex] = border[j].ToVector3(0f);
                    uvs[tindex] = uv3[j];
                    tris[tindex] = i * 3 + (!leaves[i].inverted ? triI[j] : j);
                    c = (float) (val);
                    colors[tindex] = new Color(c, c, c, alpha);
                }
            }

            Mesh mesh = new Mesh();
            mesh.vertices = vecs;
            mesh.uv = uvs;
            mesh.triangles = tris;
            mesh.colors = colors;
            return mesh;
        }

    }


    public class TriTree<U> where U : IsoTriData
    {
        public IsoTriangle<U> root { get; private set; }
        //private int splitMax;
        public double deviationThreshold = .1d; // total guess

        public TriTree(Vector2f _base, Vector2f dims)
        {
            root = new IsoTriangle<U> { _base = _base, dims = dims };
            //this.splitMax = splitMax;
        }

        public static TriTree<P> TreeToContainBox<P>(Box2f box, int splitMax = 20) where P : IsoTriData
        {
            double slope = -Math.Tan(Math.PI / 3d);
            double size = box.size.y - (box.size.x / 2f) * slope;

            float si = (int)(size + 1.5f);
            return new TriTree<P>(box.center - Vector2f.AxisY * box.size.y / 2f, new Vector2f(si * 2f / TriUtil.RootThree , si));
        }

        public void Add(U v)
        {
            var tri = root.Add(v);

            if (tri != null)
            {
                tri.DivideR(deviationThreshold);
            }
        }

        public List<List<Vector2f>> GetAllTriBorders(bool closeBorder = false)
        {
            return root.GetAllTriBorders(closeBorder);
        }

        public List<List<U>> GetAll()
        {
            return root.GetAll();
        }

        public List<IsoTriangle<U>> GetChildren()
        {
            return root.GetChildrenR();
        }

        public List<IsoTriangle<U>> GetLeaves()
        {
            return root.GetLeaves();
        }

        public void SplitUnderPopulatedLeaves(float filledThreshold, int maxDepth = 10)
        {
            root.SplitUnderPopulatedLeaves(filledThreshold, maxDepth);
        }




        internal Mesh getMesh()
        {
            return root.ToMesh();
        }
    }
}


