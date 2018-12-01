using UnityEngine;
using UnityEditor;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using SCGenerator;
using SCPointGenerator;
using System.IO;

namespace Assets.BMapToPoints
{
    public class BMapToLKHProbFile : MonoBehaviour
    {

        [MenuItem("SC/Png To libLKH prob-params files")]
        public static void _PNGToLKH()
        {
            PNGToLKH();
        }

        public static void PNGToLKH(string bmapPath = "bitmap/manga.png")
        {
            //var _crosshatch = FindObjectOfType<SVGCrosshatch>();
            var bmapToPoint = new BitMapPointGenerator(); // _crosshatch.GetBitMapPointGenerator();
            bmapToPoint.bmapName = bmapPath;

            var points = bmapToPoint.getPoints();
            for(int i = 0; i < points.Count; ++i)
            {
                string name = bmapToPoint.GetBaseName() + i;
                var tspProb = TSPLibProblem.FromPoints(points[i].GetEnumerator(), name );
                //string name = Path.GetFileNameWithoutExtension(bmapToPoint._bmapName);
                tspProb.WriteParamAndProbWithBaseName(name);

            }

        }
    }
}
