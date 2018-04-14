using g3;
using SCParse;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace SCGenerator
{
    public class StripeFieldConfig : MonoBehaviour
    {
        [SerializeField]
        AnimationCurve fillToStripeInterval;

        [SerializeField]
        float _lineWidth = 1f;

        [SerializeField, Range(0f, 1f)]
        float _twoLinePatternCuttoff = .6f;

        [SerializeField, Range(.65f, 1f)]
        float _whiteValueCuttoff = .9f;

        [SerializeField, Range(-.9f, 2f)]
        float referenceAreaGain = 1.5f;

        [SerializeField]
        float _maxStripeIntervalOffWhite = 20f;

        public float lineWidth { get { return _lineWidth; } }

        public float referenceArea(float whiteAreaScalar) {
            whiteAreaScalar += referenceAreaGain;
            return (_maxStripeIntervalOffWhite * _maxStripeIntervalOffWhite * whiteAreaScalar * whiteAreaScalar);
        }

        public float targetWidthForValue(float val) {
            /*
             * See crosshatches as a grid of 'L' shapes (assume orthagonal, 90 degree crosshatches)
             * Each 'L' is two sides of a square with dimensions = w^2
             * lineWidth = s, dark area = 2ws - ss
             * value (h) = dark area/ww = (2ws - ss) / ww
             * solve for w: -h w^2 + 2s w + -s^2 = 0
             * (relative angle doesn't matter since the resulting parallelogram will have the same area) 
             */

            float a = -val;
            float b = 2 * _lineWidth;
            float c = -_lineWidth * _lineWidth;
            double minW, maxW;
            MathUtil.SolveQuadratic(a, b, c, out minW, out maxW);
            return (float)maxW;
        }

        public int lineCountForLightRatio(float wp) {
            if(wp < _twoLinePatternCuttoff) { return 2; }
            if(wp < _whiteValueCuttoff) { return 1; }
            return 0;
        }

        public float lightAreaRatio(float v) {
            return fillToStripeInterval.Evaluate(v);
        }
    }
}
