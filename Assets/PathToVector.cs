using NanoSvg;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using g3;

namespace SCParse
{
    public static class PathToVector
    {
        public static Vector2f PointAt(SvgParser.SvgPath path, int i) {
            return new Vector2f(path.pts[i * 2], path.pts[i * 2 + 1]);
        }

        //public static CPoint2D CPointAt(SvgParser.SvgPath path, int i) {
        //    return new CPoint2D(path.pts[i * 2], path.pts[i * 2 + 1]);
        //}

        public static int PointCount(SvgParser.SvgPath path) {
            return path.npts + (path.closed ? 1 : 0);
        }

        public static IEnumerable<Vector2f> GetPoints(SvgParser.SvgPath path, bool appendFirstPointIfClosed = true) {
            for (int i = 0; i < path.npts; ++i) {
                yield return PointAt(path, i);
            }
            if (appendFirstPointIfClosed && path.closed) {
                yield return PointAt(path, 0);
            }
        }

        public static List<Vector2f> GetPointList(SvgParser.SvgPath path, bool appendFirstPointIfClosed = true) {
            return Enumerable.ToList(GetPoints(path, appendFirstPointIfClosed));
        }

        //public static IEnumerable<CPoint2D> GetCPoints(SvgParser.SvgPath path, bool appendFirstPointIfClosed = true) {
        //    for (int i = 0; i < path.npts; ++i) {
        //        yield return CPointAt(path, i);
        //    }
        //    if (appendFirstPointIfClosed && path.closed) {
        //        yield return CPointAt(path, 0);
        //    }
        //}

        //public static List<CPoint2D> GetCPointList(SvgParser.SvgPath path, bool appendFirstPointIfClosed = true) {
        //    return Enumerable.ToList(GetCPoints(path, appendFirstPointIfClosed));
        //}
    }
}
