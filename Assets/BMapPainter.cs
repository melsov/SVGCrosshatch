﻿using System;
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
        public float BitMapToPaperScale { get; private set; }
        public bool flipX = true;
        public bool flipY = false;
        DbugSettings dbugSettings;
        MachineConfig machineConfig;

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

        public BMapPainter(Texture2D tex, Box2 viewBox)
        {
            map = new SCBitmap(tex);
            this.viewBox = viewBox;
            BitMapToPaperScale = viewBox.getFitScale(map.size);
            dbugSettings = UnityEngine.Object.FindObjectOfType<DbugSettings>();
            machineConfig = UnityEngine.Object.FindObjectOfType<MachineConfig>();
        }

        public void SetLineWidthWithPenDiamMM(float diamMM)
        {
            lineWidth = diamMM * BitMapToPaperScale;
        }

        public void DrawBox(Box2 box) {
            foreach (var edge in box.getSides()) {
                DrawLine(edge.a, edge.b);
            }
        }

        public void DrawLine(Vector2 from, Vector2 to) {
            map.DrawLine(flip(from * BitMapToPaperScale), flip(to * BitMapToPaperScale), dbugSettings.pathDirIndicators);
        }

        public void DrawCursorUpdate(CursorUpdate cu) {
            //DEBUG
            if(cu.to.z > 0f)
            {
                Debug.Log("z height of to: " + cu.to.z);
            }
            if (machineConfig.isAtTravelHeight(cu.to)) {
                if (dbugSettings.drawTravelMoves)
                {
                    DrawTravelMove(cu.from.xy, cu.to.xy);
                }
            } else
            {
                DrawLine(cu.from.xy, cu.to.xy);
            }
        }

        private void DrawPath(PenDrawingPath path) {
            List<PenMove> moves = path.GetMoves().ToList();
            if(moves.Count == 0) { return; }
            PenMove last = moves[0];
            PenMove next;
            for(int i = 1; i < moves.Count; ++i) {
                next = moves[i];
                if (!next.up || dbugSettings.drawTravelMoves)
                    DrawLine(last.destination, next.destination);
                last = next;
            }
        }

        public void DrawPenUpdate(PenUpdate pu) {
            DrawPath(pu.drawPath);

        }

        private void DrawTravelMove(Vector2f from, Vector2f to) {
            lineWidth = .9f;
            color = new Color(1f, .1f, .1f);
            DrawLine(from, to);
            color = Color.black;
            lineWidth = 2f;
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

    public void DrawLine(Vector2 from, Vector2 to, bool shouldUseDirIndicator = false)
    {
        LineBox(from, to, lineWidth, shouldUseDirIndicator);
    }

    private void LineBox(Vector2 from, Vector2 to, float width = 2f, bool shoudlUseDirIndicator = false)
    {
        Vector2 dir = to - from;
        float mag = dir.magnitude;
        if(mag < Mathf.Epsilon) { return; }
        dir /= mag;

        Vector2 perp = dir.perp();
        Vector2 start = from - perp * (width / 2f);

        float incr = .7071f; // 1 / root2

        Vector2 cursor = Vector2.zero;

        while (cursor.x < mag)
        {
            cursor.y = 0f;
            while (cursor.y < width)
            {
                SetPixel(start + perp * cursor.y + dir * cursor.x);
                cursor.y += incr;
            }

            cursor.x += incr;
        }


        //Vector2i starti = from - perp * (width / 2f);

        //float ang = Mathf.Atan2(dir.y, dir.x);
        //Matrix2f mat = new Matrix2f(ang);
        //AxisAlignedBox2i aaBox = new AxisAlignedBox2i(0, -Mathf.RoundToInt(width / 2f), Mathf.RoundToInt(mag), Mathf.RoundToInt(width / 2));
        //foreach (var vi in aaBox.IndicesInclusive())
        //{
        //    SetPixel(starti + mat * vi);
        //}
        ////TODO: optimize this 
        //if (shoudlUseDirIndicator)
        //{
        //    Vector2 arrow = to;
        //    foreach (int i in Enumerable.Range(0, 16))
        //    {
        //        SetPixel(arrow, Color.red);
        //        arrow -= dir;
        //    }
        //}
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

    public void SetPixel(Vector2 v, Color c) {
        SetPixel((int)v.x, (int)v.y, c);
    }

    public void SetPixel(Vector2Int v) {
        SetPixel(v.x, v.y);
    }

    public void SetPixel(Vector2i v) {
        SetPixel(v.x, v.y);
    }

    public void SetPixel(int x, int y) {
        SetPixel(x, y, color);
    }

    public void SetPixel(int x, int y, Color c) {
        x = Mathf.Clamp(x, 0, (int)size.x - 1);
        y = Mathf.Clamp(y, 0, (int)size.y - 1);
        tex.SetPixel(x, y, c);
    }

}

