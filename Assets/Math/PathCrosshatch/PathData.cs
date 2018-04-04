using NanoSvg;
using g3;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using SCParse;
using UnityEngine;

namespace SCGenerator
{
    public enum ArrowEnterExit
    {
        Enters, Exits, DoesntIntersect
    }

    public class PathData
    {
        public SvgParser.SvgPath path { get; private set; }

        private List<Vector2f> pathPoints;
        private int[] horizontal { get; set; }
        private int[] vertical { get; set; }
        public List<int> enterFromLeftEdgeIndices { get; private set; }
        public List<int> exitFromLeftEdgeIndices { get; private set; }
        public Box2f bounds { get; private set; }
        public bool isClockwiseOrdered { get; private set; }
        public bool isYAxisInverted { get; private set; }
        public float area { get; private set; }
        public Vector2f average { get; private set; }

        public PathData(SvgParser.SvgPath path, bool isYAxisInverted = true) {
            this.path = path;
            this.isYAxisInverted = isYAxisInverted;
            addPathPoints(path);
            if(pathPoints.Count < 2) { throw new Exception("Not sure I can work with this path. (fewer than 2 points)"); }
            setup();
        }

        public IEnumerable<Vector2f> points() {
            foreach(var v in pathPoints) {
                yield return v;
            }
        }

        // clockwise detection: https://stackoverflow.com/questions/1165647/how-to-determine-if-a-list-of-polygon-points-are-in-clockwise-order

        private void addPathPoints(SvgParser.SvgPath path) {
            pathPoints = new List<Vector2f>(path.npts);
            float shoeLaceSum = 0f;
            Vector2f v, vNext;
            for(int i=0; i<path.npts; ++i) {
                v = PathToVector.PointAt(path, i);
                vNext = PathToVector.PointAt(path, (i + 1) % path.npts);
                shoeLaceSum += (vNext.x - v.x) * (vNext.y + v.y);
                pathPoints.Add(v);
                average += v;
            }
            area = Math.Abs(shoeLaceSum) / 2f;
            isClockwiseOrdered = (shoeLaceSum > 0f) != isYAxisInverted;
            average /= path.npts;
        } 

        protected void setup() {
            int i = 0;
            var indexLookup = new Dictionary<int, Vector2f>(pathPoints.Count);

            for (i = 0; i < pathPoints.Count; ++i) { indexLookup.Add(i, pathPoints[i]); }

            horizontal = new int[pathPoints.Count];
            vertical = new int[pathPoints.Count];

            var keyValues = indexLookup.ToList();
            i = 0;
            foreach (var kv in keyValues.OrderBy((_kv) => _kv.Value.x)) {
                horizontal[i++] = kv.Key;
            }
            i = 0;
            foreach (var kv in keyValues.OrderBy((_kv) => _kv.Value.y)) {
                vertical[i++] = kv.Key;
            }
            bounds = new Box2f()
            {
                min = new Vector2f(leftMost.x, lowest.y),
                max = new Vector2f(rightMost.x, highest.y)
            };
            setupEnterExitEdgeFromLeft();
        }

        public bool areEnterLeftEdgesPosDelta {
            get {
                return isYAxisInverted != isClockwiseOrdered;
            }
        }

        private void setupEnterExitEdgeFromLeft() {
            enterFromLeftEdgeIndices = new List<int>(pathPoints.Count / 2);
            exitFromLeftEdgeIndices = new List<int>(pathPoints.Count / 2);
            for(int i = 0; i < pathPoints.Count; ++i) {
                Vector2f current = this[i];
                Vector2f next = nextPoint(i);
                if (Mathf.Abs(next.y - current.y) > Mathf.Epsilon) {
                    if (next.y > current.y == areEnterLeftEdgesPosDelta) {
                        enterFromLeftEdgeIndices.Add(i);
                    }
                    else {
                        exitFromLeftEdgeIndices.Add(i);
                    }
                }
            }
        }

        public int Count { get { return pathPoints.Count; } }


        //TODO: dividable edge

        public int indexBelow(float vy, float epsilon = 0.00001f) {
            vy += epsilon;
            if(lowest.y > vy) { return -1; }
            if(highest.y < vy) { return -2; }
            for(int i = vertical.Length - 2; i >= 0; --i) {
                if(this[vertical[i]].y < vy) { return i; }
            }
            return -1;
        }

        public int indexLeftOf(float vx, float epsilon = 0.00001f) {
            vx += epsilon;
            if(leftMost.x > vx) { return -1; }
            if(rightMost.x < vx) { return -2; }
            for(int i = horizontal.Length - 2; i >= 0; --i) {
                if (this[horizontal[i]].x < vx) { return i; }
            }
            return -1;
        }

        public Vector2f this[int i] {
            get {
                return pathPoints[i];
            }
        }

        public Vector2f nextPoint(int i) { return this[(i + 1) % Count]; }

        public Vector2f precedingPoint(int i) { return this[(i == 0 ? Count - 1 : i - 1)]; }

        public Edge2f edgeAt(int i) {
            return new Edge2f(this[i], nextPoint(i));
        }

        public Corner2f cornerAt(int i) {
            return new Corner2f(this[i], precedingPoint(i), nextPoint(i));
        }

        public Vector2f leftMost { get { return getHorizontalOrderedV(0); } }

        public Vector2f rightMost { get { return getHorizontalOrderedV(horizontal.Length - 1); } }

        public Vector2f lowest { get { return getVerticalOrderedV(0); } }

        public Vector2f highest { get { return getVerticalOrderedV(vertical.Length - 1); } }

        public Vector2f getHorizontalOrderedV(int i) {
            return pathPoints[horizontal[i]];
        }

        public Vector2f getVerticalOrderedV(int i) {
            return pathPoints[vertical[i]];
        }

        public IEnumerable<Vector2f> getHorizontals() {
            foreach(int i in Enumerable.Range(0, Count)) { yield return getHorizontalOrderedV(i); }
        }

        public IEnumerable<Vector2f> getVerticals() {
            foreach(int i in Enumerable.Range(0, Count)) { yield return getVerticalOrderedV(i); }
        }

        public ArrowEnterExit fromLeftAtYWithEdgeAtIndex(float y, int index, out Vector2f intersectionPoint) {

            Vector2f v = pathPoints[index];
            Vector2f vNext = nextPoint(index);
            Edge2f edge = new Edge2f(v, vNext);
            bool intersects = edge.intersectionPointWithY(y, out intersectionPoint);

            if(!intersects) {
                intersectionPoint = Vector2f.Zero;
                return ArrowEnterExit.DoesntIntersect;
            }

            if (vNext.y > v.y == isClockwiseOrdered) {
                return ArrowEnterExit.Enters;
            }
            return ArrowEnterExit.Exits;
        }

        public IEnumerable<Vector2f> arrowFromThisToNext(int i, bool wantPreceding = false) {
            Vector2f current = this[i];
            Vector2f next = nextPoint(i);
            Vector2f dir = next - current;
            yield return current;
            yield return current + dir * .75f;
            yield return (current + dir * .75f) + (new Vector2f(dir.y, -dir.x) - dir)/4f;

        }


        public bool isIntersectionAtY(int pathIndex, float y, bool wantNext, out Vector2f intersectionPoint) {

            Vector2f v = pathPoints[pathIndex];
            Vector2f b = wantNext ? nextPoint(pathIndex) : precedingPoint(pathIndex);

            if(v.y > b.y) {
                Vector2f temp = v;
                b = v;
                v = temp;
            }

            if(v.y < y && y < b.y) {
                Vector2f dif = b - v;
                intersectionPoint = new Vector2f(v.x + (y - v.y) * dif.x / dif.y,  y);
                return true;
            }

            intersectionPoint = Vector2f.Zero;
            return false;

        }
    }
}
