using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace SCGenerator
{
    [System.Serializable]
    public class GeneratorConfig
    {
        #region TSP-generator-settings

        public int MaxTSPThreads = 8;

        public int MaxTSPTriangleCities = 300;

        [Range(.001f, .9999f)]
        public float TriFilledThreshold = .5f;

        public bool ShouldSplitUnderFilledTris = true;

        [Range(1, 20)]
        public int MaxSplitToFillRecursionDepth = 1;

        [Range(.2f, 1.8f)]
        public float SkewMaxLoops = 1f;

        [Header("In: tri pixel mean. Out: num loops factor")]
        public AnimationCurve SkewTriPixelMean;

        #endregion
    }
}
