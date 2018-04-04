using UnityEngine;
using g3;
using SCParse;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NanoSvg;

namespace SCGenerator
{

    public struct StripedPath
    {
        public StripeField stripeField;
        public SvgParser.SvgPath path;
    }

    public class StripedPathSet
    {

        private StripeFieldConfig stripeFieldConfig;
        private SCSvgFileData _svgFileData;

        private List<SvgParser.SvgPath> paths;
        //private StripeField[] sfields;

        private DiamondCalculator diamondCalculator;
        private DbugSettings dbugSettings;

        public StripedPathSet(SvgParser.SvgPath path, StripeFieldConfig stripeFieldConfig, SCSvgFileData _svgFileData) {
            this.stripeFieldConfig = stripeFieldConfig;
            this._svgFileData = _svgFileData;
            diamondCalculator = new DiamondCalculator(stripeFieldConfig);
            dbugSettings = UnityEngine.Object.FindObjectOfType<DbugSettings>();
            setup(path);
        }

        private void setup(SvgParser.SvgPath path) {
            //setupFields(path);
            paths = new List<SvgParser.SvgPath>();

            for(var it = path; it != null; it = it.next) {
                //rotated copies
                if(dbugSettings.makeVariations) {
                    addRotatedCopies(it);
                    if(dbugSettings.reverseClockwiseOrderCopies) {
                        addRotatedCopies(it.ReversedPointsClone(), 1);
                    }
                    break; // only use the first path
                }

                paths.Add(it);
            }

        }

#region test-data-generation

        private void addRotatedCopies(SvgParser.SvgPath it, int copyOffset = 0) {
            PathData pdata = new PathData(it, _svgFileData.isYAxisInverted);

            int iters = dbugSettings.numVariations;
            float ang = Mathf.PI / 2f / iters;
            Vector2f cOffset = new Vector2f(0f, iters / dbugSettings.variationColumns * copyOffset);
            for(int i=0; i<iters; ++i) {
                paths.Add(rotateAndShift(pdata, ang * i, cOffset + new Vector2f(i % (dbugSettings.variationColumns), i / (dbugSettings.variationColumns))));
            }
        }

        private SvgParser.SvgPath rotateAndShift(PathData pdata, float radians, Vector2f placementCoord) {

            Matrix2f rot = Matrix2f.Identity;
            if (dbugSettings.variationType == VariationType.RotatePath) { rot.SetToRotationRad(radians); }
            float[] pts = new float[pdata.path.pts.Length];
            Vector2f v;
            float length = pdata.bounds.size.Length;
            float shift = 0f;
            while(shift < length) { shift += dbugSettings.dbugStripeInterval; }

            for(int i=0; i<pdata.path.npts; ++i) {
                v = (rot * (pdata.path[i] - pdata.average)) + pdata.average + placementCoord * shift;
                pts[i * 2] = v.x;
                pts[i * 2 + 1] = v.y;
            }
            
            return pdata.path.CloneWithPoints(pts);
        }

        private StripeField debugStripeField(int i) {
            return new StripeField()
            {
                //direction = dbugSettings.variationType == VariationType.RotateStripeDirection ?
                //new Matrix2f((dbugSettings.reverseClockwiseOrderCopies ? (i * 2) % paths.Count : i) / (float)paths.Count * Mathf.PI) * dbugSettings.baseStripeDirection :
                //dbugSettings.baseStripeDirection,
                angleRadians = dbugSettings.variationType == VariationType.RotateStripeDirection ?
                    (dbugSettings.reverseClockwiseOrderCopies ? (i * 2) % paths.Count : i) / (float)paths.Count * Mathf.PI 
                    + dbugSettings.baseStripeAngleRadians :
                dbugSettings.baseStripeAngleRadians,
                interval = dbugSettings.dbugStripeInterval
            };
        }

#endregion

        //private void setupFields(SvgParser.SvgPath path) {

        //    //List<StripeField> sfs = new Something??? List<StripeField>();
        //    int count = 0;
        //    for (var it = path; it != null; it = it.next) {
        //        // TODO: design a thing that can tell us what interval , direction to assign to each sfield
        //        count++;
        //    }

        //    sfields = new StripeField[count];

        //    //TODO: something more interesting
        //    for (int i = 0; i < sfields.Length; ++i) {
        //        sfields[i] = new StripeField();
        //        sfields[i].direction = new Vector2f(1f, 0f);
        //        sfields[i].interval = 8f;
        //    }
        //}

        public StripedPath this[int i] {
            get {
                if(dbugSettings.makeVariations) {
                    return new StripedPath()
                    {
                        stripeField = debugStripeField(i),
                        path = paths[i]
                    };
                }
                StripeField sfield = null;
                if (diamondCalculator.getStripeFieldIterator(new PathData(paths[i]), out sfield)) {
                    return new StripedPath()
                    {
                        stripeField = sfield,
                        path = paths[i],
                    };
                }
                return new StripedPath();
            }
        }

        public int Count { get { return paths.Count; } }

        public IEnumerable<StripedPath> iter() {
            for(int i=0; i < Count; ++i) {
                yield return this[i];
            }
        }
    }
}
