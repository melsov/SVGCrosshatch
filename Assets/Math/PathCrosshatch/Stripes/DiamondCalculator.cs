using g3;
using NanoSvg;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace SCGenerator
{
    public class DiamondCalculator
    {
        StripeFieldConfig stripeFieldConfig;
        DbugSettings dbugSettings;

        public DiamondCalculator(StripeFieldConfig stripeFieldConfig) {
            this.stripeFieldConfig = stripeFieldConfig;
            dbugSettings = GameObject.FindObjectOfType<DbugSettings>();
        }

        //CONSIDER: analyze all of the paths and gen StripeFields based on some global data

        public bool getStripeFieldIterator(PathData pdata, out StripeField sfield) {

            float value = ColorUtil.valueFromUIntColor(pdata.path.fillColor);

            float lightRatio = stripeFieldConfig.lightAreaRatio(value);
            int lineCount = stripeFieldConfig.lineCountForLightRatio(lightRatio);
            float darkRatio = 1f - lightRatio;

            if (lineCount == 0) { sfield = new StripeField(); return false; }

            Vector2f primary = pdata.bounds.tallerThanWide ? pdata.highest - pdata.lowest : pdata.rightMost - pdata.leftMost;

            float priAngle = Mathf.PI * 2f / 3f; //  Mathf.Atan2(primary.y, primary.x);
            priAngle = Mathf.Abs(priAngle);

            Vector2f sz = pdata.bounds.size;
            float diamondAngle = Mathf.PI / 6f; // priAngle + pdata.bounds.smallerOverLargerDimension * Mathf.PI / 2.5f; //TODO: determine angle somehow


            //throw new Exception("makes almost all lines go away on sphere!");

            // make sure diamond is sub 90 degrees?

            Vector2f secondary = new Vector2f(Mathf.Cos(diamondAngle), Mathf.Sin(diamondAngle));

            float targetWidth = stripeFieldConfig.targetWidthForValue(darkRatio);
            float targetArea = targetWidth * targetWidth; // stripeFieldConfig.referenceArea(lightRatio); 

            if(targetArea < Mathf.Epsilon) {
                sfield = new StripeField()
                {
                    angleRadians = priAngle,
                    //direction = primary.Normalized,
                    interval = stripeFieldConfig.lineWidth
                };
                return true;
            }

            float mult = Mathf.Sqrt(targetArea /Mathf.Max(Mathf.Abs(secondary.y), 0.0001f));
            primary.Normalize();

            float interval = secondary.y * mult;

            secondary = new Matrix2f(priAngle) * secondary;

            StripeField _next = new StripeField()
            {
                angleRadians = diamondAngle,
                //direction = secondary,
                interval = interval
            };

            if(dbugSettings.hatchCount == 1) {
                sfield = _next;
                return true;
            }

            sfield = new StripeField()
            {
                angleRadians = priAngle,
                //direction = primary,
                interval = interval,
                next = _next
            };

            return true;


        }



    }
}
