using SCParse;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace SCGenerator
{
    public class SCPointSetPenPathGenerator : SCBasePenPathGenerator
    {
        public SCPointSetPenPathGenerator(
           MachineConfig machineConfig,
           Box2 viewBox,
           GeneratorConfig generatorConfig)
           : base(machineConfig, viewBox, generatorConfig)
        {
        }

        public TSPPointSets pointSets;

        public override IEnumerable<PenDrawingPath> generate()
        {
            if(pointSets != null)
            {
                for(int i = 0; i < pointSets.Count; ++i)
                {
                    PenDrawingPath pdPath = new PenDrawingPath();
                    var points = pointSets[i];
                    foreach(var p in points)
                        pdPath.addDrawMove(p * viewBoxToPaperScale);

                    yield return pdPath;
                }
            }
        }
    }
}
