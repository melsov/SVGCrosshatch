using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CrosshatchC.NET.GCMath;
using g3;
using SCCollection;
using SCParse;
using SCPointGenerator;
using SCUtil;
using UnityEngine;

namespace SCGenerator
{
    public class SCTriangleCrosshatchGenerator : SCBasePenPathGenerator
    {
        private TriTree<PixelTriData> triTree;

        string BaseFileName;

        TSPPointSets tspPointSets;

        BitMapPointGenerator bitMapPointGenerator;


        public SCTriangleCrosshatchGenerator(
            MachineConfig machineConfig, 
            GeneratorConfig generatorConfig,
            TriTree<PixelTriData> triTree,
            BitMapPointGenerator bitMapPointGenerator) : 
            base(machineConfig,bitMapPointGenerator.inputImageBox, generatorConfig)
        {
            this.triTree = triTree;
            this.BaseFileName = bitMapPointGenerator.GetBaseName();
            this.bitMapPointGenerator = bitMapPointGenerator;
        }

        void SolveTriTree(PenDrawingPath pdPath)
        {
            //CONSIDER: two passes
            // 1.) Dark very detailed
            // 2.) mid to light don't sweat the details as much

            //split tri tree leaves if not sufficiently populated
            if (generatorConfig.ShouldSplitUnderFilledTris)
            {
                triTree.root.SplitUnderPopulatedLeaves(generatorConfig.TriFilledThreshold, generatorConfig.MaxSplitToFillRecursionDepth);
            }

            triTree.root.CullDataWithMaxMean(bitMapPointGenerator.MaxGrayScale);

            List<IsoTriangle<PixelTriData>> subTrees;
            // split tree into max cities sub trees
            subTrees = triTree.root.NonEmptyChildrenWithMaxLeaves(generatorConfig.MaxTSPTriangleCities);



            //DEBUG
            var meshGO = MeshUtil.MakeGameObject(triTree.getMesh(), "CrosshatchTriTree");

            var pool = new CMDProcessPool(generatorConfig.MaxTSPThreads);
            var subLeaves = new List<IsoTriangle<PixelTriData>>[subTrees.Count];
            var tsps = new TSPLibProblem[subTrees.Count];

            //DEBUG delete any previously generated files
            TSPLibProblem.GetDeleteAllCommand(BaseFileName + "*").ToCMDProcess().run();

            for (int i = 0; i < subTrees.Count; ++i)
            {
                var tree = subTrees[i];

                //get leaf centers
                var leaves = tree.GetLeaves(true);

                var centers = new List<Vector2f>(leaves.Count);

                for (int j = 0; j < leaves.Count; ++j)
                {
                    centers.Add(leaves[j].center);
                }

                // solve each sub tree
                var tsp = TSPLibProblem.FromPoints(centers.GetEnumerator(), BaseFileName + ".tri." + i);
                if (!tsp.OutputFileExists())
                {
                    pool.Add(tsp.GetTSPCommand());
                }

                subLeaves[i] = leaves;
                tsps[i] = tsp;
            }
            pool.run();

            for(int i=0; i<subTrees.Count; ++i)
            {
                // crosshatch sub trees
                var tsp = tsps[i];
                if(!tsp.setIndicesFromOutputFile())
                {
                    Debug.LogWarning("no tsp indices");
                    continue; //TODO: handle more gracefully
                }
                var leaves = subLeaves[i];

                TriCrosshatch<PixelTriData> tch = null, last = null;

                for(int j = 0; j < leaves.Count; ++j)
                {
                    tch = new TriCrosshatch<PixelTriData>(this, leaves[tsp.indexAt(j)]);
                    tch.last = last;
                    tch.next = j == leaves.Count - 1 ? null : new TriCrosshatch<PixelTriData>(this, leaves[tsp.indexAt(j + 1)]);

                    tch.AddDrawPoints(pdPath, viewBoxToPaperScale, (float)machineConfig.toolDiameterMM);

                    last = tch;
                }
            }
        }

        void DebugDrawTestTri(PenDrawingPath pdPath, Vector2f offset, int ientry, int iexit, float size = 50f, float dark = .5f, bool inverted = false)
        {
            IsoTriangle<PixelTriData> tri = new IsoTriangle<PixelTriData>
            {
                _base = offset + Vector2f.AxisX * size / 2f,
                dims = new Vector2f(size, size * TriUtil.RootThree / 2f),
                inverted = inverted
            };

            tri.Add(new PixelTriData(new PixelV {
                pos = tri.Centroid,
                color = new Color(dark, dark, dark, 1f)
            }));


            var triCross = new TriCrosshatch<PixelTriData>(this, tri);
            triCross.DrawTriSpiral(pdPath, ientry, iexit);
        }

        class TriCrosshatch<T> where T : IsoTriData
        {
            private SCTriangleCrosshatchGenerator generator;
            IsoTriangle<T> tri;
            public TriCrosshatch<T> last;
            public TriCrosshatch<T> next;

            Vector2f entryPoint;
            Vector2f exitPoint;
            bool adjacentExitPoint;
            int ientry, iexit;

            public TriCrosshatch(SCTriangleCrosshatchGenerator generator, IsoTriangle<T> tri)
            {
                this.generator = generator;
                this.tri = tri;
            }

            public void AddDrawPoints(PenDrawingPath pdPath, float imageBoxToPaperScale, float toolDiam)
            {
                var borders = tri.Border();
                ientry = 0; iexit = 1;
                exitPoint = borders[1];
                entryPoint = borders[0];
                bool touchingLast = false;
                if(last != null)
                {
                    touchingLast = last.adjacentExitPoint;
                    if (touchingLast)
                    {
                        entryPoint = last.exitPoint;
                    }
                    ientry = tri.ClosestBorderIndex(last.exitPoint);
                }

                if(next != null)
                {
                    Vector2f _exit;
                    if (tri.IsTouching(next.tri, out _exit))
                    {
                        adjacentExitPoint = true;
                        exitPoint = _exit;
                        iexit = tri.ClosestBorderIndex(_exit);
                    }
                }


                if (!touchingLast)
                {
                    pdPath.addTravelMove(entryPoint * imageBoxToPaperScale, ColorUtil.fuschia);
                }

                //if (tri.dims.x < toolDiam)
                //{
                //    pdPath.addDrawMove(tri.center * imageBoxToPaperScale);
                //}
                //else
                {
                    //FillTri(pdPath, imageBoxToPaperScale, toolDiam);
                    //TraceTri(pdPath, imageBoxToPaperScale, toolDiam);
                    DrawTriSpiral(pdPath, ientry, iexit);
                }
                // CONSIDER: fill tris based on grayscale and populatedness
                // keep track of spread-out-ness of data within tris?
            }

            internal void DrawTriSpiral(PenDrawingPath pdPath, int ientry, int iexit)
            {
                int origExit = iexit;
                if (ientry == iexit)
                {
                    iexit = (ientry + 1) % 3;
                }
                int iother = (ientry + 1) % 3;
                if (iother == iexit)
                {
                    iother = (ientry + 2) % 3;
                }

                var border = tri.Border();
                var lookup = new int[] { ientry, iother, iexit };

                bool ccw;
                {
                    Vector3 a = (border[iother] - border[ientry]).ToVector3(0f);
                    Vector3 b = (border[iexit] - border[ientry]).ToVector3(0f);
                    ccw = Vector3.Cross(a, b).z > 0f;
                }

                var centroid = tri.Centroid;
                var radii = new Vector2f[3];
                for (int i = 0; i < 3; ++i)
                {
                    radii[lookup[i]] = border[lookup[i]] - centroid;
                }

                float maxLoops = generator.generatorConfig.SkewMaxLoops * tri.InnerRadius / ((float)generator.machineConfig.toolDiameterMM / generator.viewBoxToPaperScale);
                maxLoops *= .5f;
                float loopsF = maxLoops * (float)(1d - (generator.generatorConfig.SkewTriPixelMean.Evaluate((float)tri.onlineNormalEstimator.mean())));
                int loops = Mathf.CeilToInt(loopsF);

                float incr = 1f / loopsF;
                float halfIncr = incr / 2f;
                float thirdIncr = incr / 3f;
                float sixthIncr = incr / 6f;

                float rad;
                Vector2f move;

                pdPath.addDrawMove(border[ientry] * generator.viewBoxToPaperScale, Color.magenta);

                rad = 1f - halfIncr;
                for (int i = loops - 1; i >= 0; --i)
                {
                    for (int jj = 0; jj < 3; ++jj)
                    {
                        if (rad > 0f)
                        {
                            int j = ccw ? (2 - jj) : jj;
                            move = centroid + radii[lookup[j]] * rad;
                            pdPath.addDrawMove(move * generator.viewBoxToPaperScale, jj == 2 ? Color.cyan : Color.blue);
                        }
                        rad -= thirdIncr;
                    }
                }

                
                rad += thirdIncr + halfIncr;
                for (int i = 1; i <= loops; ++i)
                {
                    for (int jj = 2; jj >= 0; --jj)
                    {
                        if (rad > 0f)
                        {
                            int j = ccw ? (2 - jj) : jj;
                            move = centroid + radii[lookup[j]] * rad;
                            pdPath.addDrawMove(move * generator.viewBoxToPaperScale, jj == 0 ? ColorUtil.pink : ColorUtil.fuschia);
                        }
                        rad += thirdIncr;
                    }
                }


                pdPath.addDrawMove(border[origExit] * generator.viewBoxToPaperScale, ColorUtil.aqua);

            }

            void TraceTri(PenDrawingPath pdPath, float imageToPaperScale, float toolDiam)
            {
                Vector2f closest, a, b;
                tri.ClosestPoint(entryPoint, out closest, out a, out b);

                if((a - exitPoint).LengthSquared < (b - exitPoint).LengthSquared)
                {
                    Vector2f temp = a;
                    a = b;
                    b = temp;
                }

                closest *= imageToPaperScale; a *= imageToPaperScale; b *= imageToPaperScale;

                pdPath.addDrawMove(closest, adjacentExitPoint ? Color.blue : Color.yellow);
                pdPath.addDrawMove(a, ColorUtil.powderBlue);
                pdPath.addDrawMove(b, adjacentExitPoint ? ColorUtil.aqua : ColorUtil.pink);
            }

            void FillTri(PenDrawingPath pdPath, float imageToPaperScale, float toolDiam)
            {
                //TODO: add some notion of pixel to pen diameter?

                float dark = Mathf.Clamp(.01f, .999f, (float)tri.onlineNormalEstimator.mean() * (1f - tri.Filled));

                Vector2f closest, a, b;
                tri.ClosestPoint(entryPoint, out closest, out a, out b);

                closest *= imageToPaperScale; a *= imageToPaperScale; b *= imageToPaperScale;

                Vector2f dA = a - closest, dB = b - closest;

                float length = tri.dims.x * imageToPaperScale;
                float incr = toolDiam + 5f * toolDiam * dark; //  length * dark;
                incr = Mathf.Max(toolDiam, incr);

                int zigs = (int)(length / incr);

                Vector2f m,n;
                for(int i=0; i<zigs * 2; i+=2)
                {
                    m = dA * i / (float)zigs;
                    pdPath.addDrawMove(closest + m);
                    n = dB * (i + 1) / (float)zigs;
                    pdPath.addDrawMove(closest + n);
                }

                // move to exit
                pdPath.addDrawMove(exitPoint * imageToPaperScale);
            }
        }

        void DoTestTris(PenDrawingPath pdPath)
        {
            Vector2f off = Vector2f.One * 4f;
            float size = 40f;
            DebugDrawTestTri(
                pdPath, 
                off + Vector2f.Zero,
                0,
                1,
                size: size,
                dark: .1f,
                inverted: false);
            DebugDrawTestTri(
                pdPath,
                off + Vector2f.AxisX * (size * 1.05f),
                2,
                0,
                size: size,
                dark: .2f,
                inverted: false);

            DebugDrawTestTri(
                pdPath,
                off + Vector2f.AxisX * (size * 1.05f * .5f) + Vector2f.AxisY * size,
                0,
                0,
                size: size,
                dark: .3f,
                inverted: true);
            DebugDrawTestTri(
                pdPath,
                off + Vector2f.AxisX * (size * 1.05f * .5f) + Vector2f.AxisY * size,
                0,
                0,
                size: size,
                dark: .4f,
                inverted: false);
            DebugDrawTestTri(
                pdPath,
                off + Vector2f.AxisX * (size * 3.0125f * .5f) + Vector2f.AxisY * size,
                0,
                0,
                size: size,
                dark: .5f,
                inverted: false);

        }

        public override IEnumerable<PenDrawingPath> generate()
        {
            PenDrawingPath pdPath = new PenDrawingPath();
            SolveTriTree(pdPath);
            //DoTestTris(pdPath);

            yield return pdPath;
        }
    }
}
