using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using UnityEditor;
using NanoSvg;
using SCParse;
using SCGenerator;
using VctorExtensions;
using g3;

namespace SCDisplay
{
    public class BMapPainter
    {
        SCBitmap map;
        Box2 viewBox;
        float scaleToMap;
        bool flipX = true;
        bool flipY = false;

        public Color color {
            get { return map.color; }
            set {
                map.color = value;
            }
        }

        public float lineWidth {
            get { return map.lineWidth; }
            set {
                map.lineWidth = Mathf.Max(value, .01f);
            }
        }

        public BMapPainter(Texture2D tex, Box2 viewBox) {
            map = new SCBitmap(tex);
            this.viewBox = viewBox;
            scaleToMap = viewBox.getFitScale(map.size);
        }

        public void DrawBox(Box2 box) {
            foreach (var edge in box.getSides()) {
                DrawLine(edge.a, edge.b);
            }
        }

        public void DrawLine(Vector2 from, Vector2 to) {
            map.DrawLine(flip(from * scaleToMap), flip(to * scaleToMap));
            ////make lines more visible
            //Vector2 dif = (to - from).normalized * 1.2f;
            //map.DrawLine(flip(from * scaleToMap + dif), flip(to * scaleToMap + dif));
        }

        public void DrawPenMove(PenUpdate pu) {
            if(pu.from == null) { return; }
            if(pu.from.up) { return; }// or draw moves in air 

            if(pu.from.hasColor) {
                color = pu.from.color;
            }
            DrawLine(pu.from.destination, pu.to.destination);
        }

        public Texture2D texture {
            get { return map.tex; }
        }

        public void applyTexture() {
            texture.Apply();
        }

        public Vector2 flip(Vector2 v) {
            v.x = flipX ? map.size.x - v.x : v.x;
            v.y = flipY ? map.size.y - v.y : v.y;
            return v;
        }


    }
}

/*
 * Wrapper to hide the actual Texture/Bitmap class
 */
public class SCBitmap
{
    public Texture2D tex { get; private set; }

    public Color color = new Color32(0, 122, 122, 255);
    public Vector2 size { get; private set; }

    private float count = 0f;

    DbugSettings dbugSettings;
    StripeFieldConfig stripeFieldConfig;
    MachineConfig machineConfig;

    public float lineWidth = 2f;

    public SCBitmap(Texture2D tex) {
        this.tex = tex;
        dbugSettings = UnityEngine.Object.FindObjectOfType<DbugSettings>();
        machineConfig = UnityEngine.Object.FindObjectOfType<MachineConfig>();
        stripeFieldConfig = UnityEngine.Object.FindObjectOfType<StripeFieldConfig>();
        lineWidth = stripeFieldConfig.lineWidth;
        size = new Vector2(tex.width, tex.height);
    }

    public void DrawLine(Vector2 from, Vector2 to) {
        Vector2 dir = to - from;
        float mag = dir.magnitude;
        if(mag < Mathf.Epsilon) { return; }
        dir /= mag;
        //LineOne(from, dir, mag);
        LineBox(from, to, lineWidth);
    }

    private void LineBox(Vector2 from, Vector2 to, float width = 2f) {
        Vector2 dir = to - from;
        float mag = dir.magnitude;
        if(mag < Mathf.Epsilon) { return; }
        dir /= mag;
        Vector2 perp = dir.perp();
        Vector2i start = from + perp * (width / 2f);

        float ang = Mathf.Atan2(dir.y, dir.x);
        Matrix2f mat = new Matrix2f(ang);
        AxisAlignedBox2i aaBox = new AxisAlignedBox2i(0, -Mathf.RoundToInt(width / 2f), Mathf.RoundToInt(mag), Mathf.RoundToInt(width / 2));
        foreach(var vi in aaBox.IndicesInclusive()) {
            SetPixel(start + mat * vi);
        }
        //TODO: optimize this 
    }

    private struct Pix2i
    {
        Vector2i v;
        Color c;
    }

        

    private void LineOne(Vector2 from, Vector2 dir, float mag) {
        count = 0f;
        while (count < mag) {
            SetPixel(from);
            from.x += dir.x;
            from.y += dir.y;
            count++;
        }
    }

    public void SetPixel(Vector2 v) {
        SetPixel((int)v.x, (int)v.y);
    }

    public void SetPixel(Vector2Int v) {
        SetPixel(v.x, v.y);
    }

    public void SetPixel(Vector2i v) {
        SetPixel(v.x, v.y);
    }

    public void SetPixel(int x, int y) {
        x = Mathf.Clamp(x, 0, (int)size.x - 1);
        y = Mathf.Clamp(y, 0, (int)size.y - 1);
        tex.SetPixel(x, y, color);
    }

}

