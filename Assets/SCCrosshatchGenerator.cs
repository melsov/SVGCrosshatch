using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using NanoSvg;
using SCDisplay;
using SCParse;
using g3;
using VctorExtensions;
using System.Collections;
using UnityEngine.Assertions;
using UnityEngine.UI;

namespace SCGenerator
{

    // NOTES:
    // PenMoves may need/want: 
    //
    // Simple Version:
    // --a pen up flag
    // --destination Vector2d
    //
    // Fancy Version:
    // --pen 'micro lifting' in z. (e.g. to modulate line width)
    // --speed settings
    // --color setting enum. (for multi-color pens)
    public class PenMove
    {
        public Vector2f destination;
        public bool up;
        public NullableColor color;
        public string text;

        public bool hasColor {
            get { return color != null; }
        }

        public static implicit operator Vector2f(PenMove pm) { return pm.destination; }
    }

    public class NullableColor
    {
        public Color color;

        public NullableColor() : this(Color.red) {

        }
        public NullableColor(Color c) {
            color = c;
        }

        public static implicit operator Color(NullableColor nc) {
            if(nc == null) {
                return Color.magenta;
            }
            return nc.color;
        }

        public static implicit operator NullableColor(Color c) { return new NullableColor(c); }
    }

    public class PenDrawingPath
    {

        private List<PenMove> moves = new List<PenMove>();

        public IEnumerable<PenMove> GetMoves() {
            foreach(PenMove m in moves) { yield return m; }
        }

        public PenMove this[int i] {
            get {
                return moves[i];
            }
        }

        public List<Vector2f> GetPoints() {
            var result = new List<Vector2f>(Count);
            foreach(int i in Enumerable.Range(0, Count)) {
                result.Add(moves[i].destination);
            }
            return result;
        }

        public int Count { get { return moves.Count; } }

        public void Add(PenMove p) { moves.Add(p); }

        public void addTravelMode(Vector2f to) {
            Add(new PenMove() { destination = to, up = true });
        }

        public void addDrawMove(Vector2f to) {
            Add(new PenMove() { destination = to, up = false });
        }

        public void addDownUpDrawMove(Vector2f start, Vector2f end, Color c) {
            Add(new PenMove() { destination = start, up = false, color = c });
            Add(new PenMove() { destination = end, up = true });
        }

        public void Rotate(Matrix2f m) {
            for (int i = 0; i < moves.Count; ++i) {
                moves[i].destination = m * moves[i].destination;
            }
        }
        

    }


    public class SCCrosshatchGenerator : SCBasePenPathGenerator
    {

        //public MachineConfig machineConfig { get; private set; }
        public HatchConfig hatchConfig { get; private set; }

        //protected float viewBoxToPaperScale;

        protected float epsilonSVG;

        protected SCSvgFileData _svgFileData;

        protected DbugSettings dbugSettings;

        StripedPathSet spSet;

        public void UsePathSet(StripedPathSet _spSet)
        {
            spSet = _spSet;
        }

        public SCCrosshatchGenerator(
            MachineConfig machineConfig, 
            HatchConfig hatchConfig, 
            SCSvgFileData scSvgFileData,
            Box2 svgViewBox,
            GeneratorConfig generatorConfig
            ) : base(machineConfig, svgViewBox, generatorConfig) {

            //this.machineConfig = machineConfig;
            //viewBoxToPaperScale = svgViewBox.getFitScale(machineConfig.paper.size);

            this.hatchConfig = hatchConfig;
            this._svgFileData = scSvgFileData;
            epsilonSVG = 1f / Mathf.Max(svgViewBox.size.y, svgViewBox.size.x) * 10f;

            this.dbugSettings = GameObject.FindObjectOfType<DbugSettings>();
        }




        //
        // Overrideable
        //
        public override IEnumerable<PenDrawingPath> generate() 
        {

            foreach (var stripedPath in spSet.iter())
            {

                foreach(PenDrawingPath ppath in crosshatches(stripedPath)) {
                    yield return ppath;
                }

                //DEBUG
                if (dbugSettings.pathOutlines)
                {
                    foreach (PenDrawingPath ppath in outline(stripedPath)) {
                        yield return ppath;
                    }
                }
            }
            

        }


        #region crosshatch-func

        IEnumerable<PenDrawingPath> crosshatches(StripedPath stripedPath)
        {

            Matrix2f inverseRotation;
            PathData pdata;

            for(var stripeField = stripedPath.stripeField; stripeField != null; stripeField = stripeField.next)
            {

                inverseRotation = stripeField.rotation.Inverse();
                if(dbugSettings.dontRotateBackFinalPenPaths) {
                    inverseRotation = Matrix2f.Identity;
                }
                pdata = new PathData(SvgParser.SvgPath.RotatedClone(stripedPath.path, stripeField.rotation), _svgFileData.isYAxisInverted);

                int lastEnterEdgeIndex = -100;
                foreach(int i in pdata.enterFromLeftEdgeIndices) {

                    Vector2f edgeLower = pdata.areEnterLeftEdgesPosDelta ? pdata[i] : pdata.nextPoint(i);
                    Vector2f edgeUpper = pdata.areEnterLeftEdgesPosDelta ? pdata.nextPoint(i) : pdata[i];
                    Assert.IsTrue(edgeLower.y < edgeUpper.y, "something wrong at index: " + i);

                    Edge2f leftEdge = new Edge2f(edgeLower, edgeUpper);
                    Vector2f leftIntersection;

                    // foreach y: find exit edges that span the y, find closest intersecion point
                    float y = stripeField.nextStripeYPosAbove(edgeLower.y);
                    for (; y < stripeField.nextStripeYPosAbove(edgeUpper.y); y += stripeField.interval) {

                        if (!leftEdge.intersectionPointWithY(y, out leftIntersection)) {
                            continue;
                            //break; // want?
                        }
                        

                        float closestXDelta = float.MaxValue;

                        for(int j = 0; j < pdata.exitFromLeftEdgeIndices.Count; ++j) {
                            int jV = pdata.exitFromLeftEdgeIndices[j];
                            Edge2f jEdge = pdata.edgeAt(jV);
                           
                            Vector2f intrsection;
                            if(jEdge.intersectionPointWithY(y, out intrsection)) {
                                float dif = intrsection.x - leftIntersection.x;
                                if(dif > 0f && dif < closestXDelta) {
                                    closestXDelta = dif;
                                }
                            }
                        }

                        if (closestXDelta < float.MaxValue) {
                            PenDrawingPath penPath = new PenDrawingPath();
                            
                            penPath.addDownUpDrawMove(
                                (inverseRotation * leftIntersection) * viewBoxToPaperScale,
                                (inverseRotation * new Vector2f(leftIntersection.x + closestXDelta, y)) * viewBoxToPaperScale, 
                                Color.black);
                            yield return penPath;
                           
                        }
                    }

                    lastEnterEdgeIndex = i;
                }
            }
        }

        #endregion

        #region debug-shapes
        private PenDrawingPath arrowToNextDBUG(int i, PathData pdata, Matrix2f rot) {
            var pp = new PenDrawingPath();
            Vector2f last = Vector2f.Zero;
            foreach(var v in pdata.arrowFromThisToNext(i)) {
                last = (rot * v) * viewBoxToPaperScale;
                pp.Add(new PenMove() { destination = last});
            }
            pp.Add(new PenMove() { destination = last, up = true });
            return pp;
        }

        private PenDrawingPath dotAtDBUG(Vector2f v, Matrix2f rot, Color c, float size = 5f) {
            var pp = new PenDrawingPath();
            v = rot * v;
            Box2f box = new Box2f() { min = v, max = v + Vector2f.One * size };
            foreach(Vector2f l in box.getHorizontalLines()) {
                pp.Add(new PenMove() { destination = l * viewBoxToPaperScale, color = c });
            }
            pp.Add(new PenMove() { destination = v * viewBoxToPaperScale, up = true });
            return pp;
        }

        private PenDrawingPath pathForBoxDBUG(Box2f box) {
            PenDrawingPath pp = new PenDrawingPath();
            pp.Add(new PenMove() { destination = box.min * viewBoxToPaperScale, color = Color.red });
            pp.Add(new PenMove() { destination = box.lowerRight * viewBoxToPaperScale });
            pp.Add(new PenMove() { destination = box.max * viewBoxToPaperScale });
            pp.Add(new PenMove() { destination = box.upperLeft * viewBoxToPaperScale });
            pp.Add(new PenMove() { destination = box.min * viewBoxToPaperScale, color = Color.black });
            return pp;
        }

        private PenDrawingPath shortLineDBUG(Vector2f v, Matrix2f rot, Color c, float length = 8f) {
            return twoPointPenPath(v, v + Vector2f.One * length, rot, c);
        }

        private PenDrawingPath drawEdge(Edge2f e, Matrix2f rot, Color c) {
            return twoPointPenPath(e.a, e.b, rot, c);
        }

#endregion
        private PenDrawingPath twoPointPenPath(Vector2f start, Vector2f end, Matrix2f rot) {
            return twoPointPenPath(start, end, rot, Color.black);
        }
        private PenDrawingPath twoPointPenPath(Vector2f start, Vector2f end, Matrix2f rot, Color c) {
            PenDrawingPath pp = new PenDrawingPath();
            pp.addDownUpDrawMove((rot * start) * viewBoxToPaperScale, (rot * end) * viewBoxToPaperScale, c);
            //pp.Add(new PenMove() { destination = (rot * start) * viewBoxToPaperScale, up = false, color = c });
            //pp.Add(new PenMove() { destination = (rot * end) * viewBoxToPaperScale, up = true });
            return pp;
        }
        

        //private PenPath travelMoveTo(Vector2f end, Matrix2f rot, Color c) {
        //    PenPath pp = new PenPath();
        //    pp.Add(new PenMove() { up = false, color = c });
        //    pp.Add(new PenMove() { destination = (rot * end) * viewBoxToPaperScale, up = true });
        //    return pp;
        //}

        IEnumerable<PenDrawingPath> outline(StripedPath stripedPath) {
            Matrix2f inverseRotation;
            PathData pdata;
            for (var stripeField = stripedPath.stripeField; stripeField != null; stripeField = stripeField.next) {

                inverseRotation = stripeField.rotation.Inverse();
                pdata = new PathData(SvgParser.SvgPath.RotatedClone(stripedPath.path, stripeField.rotation), _svgFileData.isYAxisInverted);

                foreach (int entranceI in pdata.enterFromLeftEdgeIndices) {
                    Edge2f enterEdge = pdata.edgeAt(entranceI);
                    yield return drawEdge(enterEdge + enterEdge.difference.Normalized.Perp * 5f, inverseRotation, ColorUtil.roygbivMod(entranceI));
                    yield return drawEdge(enterEdge, inverseRotation, pdata.isClockwiseOrdered ? ColorUtil.brown : ColorUtil.fuschia);
                }
                foreach(int exitI in pdata.exitFromLeftEdgeIndices) {
                    yield return drawEdge(pdata.edgeAt(exitI) + pdata.edgeAt(exitI).difference.Normalized.Perp * 5f, inverseRotation, ColorUtil.roygbivMod(exitI));
                    yield return drawEdge(pdata.edgeAt(exitI), inverseRotation, pdata.isClockwiseOrdered ? new Color(.05f, .5f, 0f) : ColorUtil.pink);
                }


            }
        }

    }

}
