using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using SCParse;
using g3;
using NanoSvg;
using UnityEngine;

namespace SCGenerator
{
    public class TSPPointSets
    {
        List<List<Vector2f>> pset = new List<List<Vector2f>>();

        public int Count {
            get {
                return pset.Count;
                
            }
        }

        public int AggregateCount {
            get {
                int c = 0;
                foreach (var ps in pset) { c += ps.Count; }
                return c;
            }
        }

        public List<Vector2f> this[int i] {
            get {
                if(i < pset.Count) { return pset[i]; }
                return null;
            }
        }

        public void Add(List<Vector2f> points)
        {
            pset.Add(points);
        }
    }

    public class SCTSPCrosshatchGenerator : SCBasePenPathGenerator
    {
        public SCTSPCrosshatchGenerator(
            MachineConfig machineConfig,
            Box2 viewBox) 
            : base(machineConfig, viewBox)
        {
        }

        TSPPointSets pointSets = new TSPPointSets();

        public void SetTSPPointSets(TSPPointSets tspPointSet)
        {
            pointSets = tspPointSet;
            Debug.Log("points count: " + pointSets.Count);
        }

        public string BaseFileName;

        public void AddPoints(List<Vector2f> _points)
        {
            pointSets.Add(_points);
        }

        public override string GetProcessIntensityComments()
        {
            if(pointSets == null)
            {
                return "no points yet. no time estimate";
            }
            string comment = "This could take ";
            string est;
            int Count = pointSets.AggregateCount;
            if(Count < 50)
            {
                est = "not too long";
            }
            else if(Count < 200)
            {
                est = "a few minutes";
            }
            else if(Count < 1000)
            {
                est = "quite a few minutes";
            }
            else
            {
                est = "forever? days? just let the program run.";
            }
            return comment + est;

        }

        public override IEnumerable<PenDrawingPath> generate() //StripedPathSet spSet)
        {
            for(int i = 0; i < pointSets.Count; ++i)
            {
                yield return fromPoints(pointSets[i], i);
            }
        }

        PenDrawingPath fromPoints(List<Vector2f> pointsList, int subscript)
        {
            PenDrawingPath pdpath = new PenDrawingPath();
            TSPLibProblem tsp = TSPLibProblem.FromPoints(pointsList.GetEnumerator(), BaseFileName + "." + subscript);
            if (pointsList.Count > 0)
            {

                bool success = tsp.runSolver();
                Debug.Log("TSP success? " + success);
                if (success)
                {

                    for (int i = 0; i < pointsList.Count; ++i)
                    {

                        pdpath.addDrawMove(pointsList[tsp.indexAt(i)] * viewBoxToPaperScale);
                    }
                }
                else
                {
                    Debug.LogWarning("tsp encountered an error");
                }
                Debug.Log("after add pts ");
            }
            return pdpath;
        }


    }
}
