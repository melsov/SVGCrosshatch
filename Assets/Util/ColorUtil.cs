using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace SCGenerator
{
    public static class ColorUtil
    {

        public static Color pink = new Color(1f, .7f, .7f);
        public static Color fuschia = new Color(.7f, .2f, .3f);
        public static Color aqua = new Color(0f, .6f, .66f);
        public static Color brown = new Color(.5f, .2f, 0f);
        public static Color powderBlue = new Color(.6f, .6f, 1f);
        public static Color dullRobinsEgg = new Color32(103, 100, 233, 255);

        private const float RgbChannelMax = 256f;

        public static readonly Color[] roygbiv = new Color[]
        {
            Color.red, new Color(1f, .5f, 0f), Color.yellow, Color.green, Color.blue, new Color(.2f, 0f, .6f), new Color(.9f, .3f, .85f),
        };

        public static Color roygbivMod(int i) {
            if(i < 0) {
                i = roygbiv.Length - (Mathf.Abs(i) % roygbiv.Length);
            }
            return roygbiv[i % roygbiv.Length];
        }

        public static Color darken(Color c,float factor = .5f) {
            float h, s, v;
            Color.RGBToHSV(c, out h, out s, out v);
            return Color.HSVToRGB(h, v, v * factor);
        }

        public static uint RightShift(uint u, int nbits) {
            return (u >> nbits) | (u << (32 - nbits));
        }

        public static Color uintToColor(uint c) {
            byte r = (byte)(c >> 16);
            byte g = (byte)(c >> 8);
            byte b = (byte)(c >> 0);
            return new Color(r / RgbChannelMax, g / RgbChannelMax, b / RgbChannelMax);
        }

        public static float valueFromUIntColor(uint c) {
            float h, s, v;
            Color col = uintToColor(c);
            Color.RGBToHSV(uintToColor(c), out h, out s, out v);
            return v;
        }
    }
}
