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
using SCDisplay;
using System.IO;
using SCCollection;

namespace SCPointGenerator 
{
    [System.Serializable]
    public class BitMapPointGenerator
    {
        Texture2D _texture;
        Texture2D texture {
            get {
                if (!_texture)
                {
                    _texture = LoadPNG(bmapName);
                }
                return _texture;
            }
        }

        [SerializeField]
        string _bmapName = "bitmap/bomb.png";

        [SerializeField, Header("If splitting point sets. Max cities per point set")]
        public int tspProbMaxCities = 100;

        [SerializeField]
        bool splitPointSets;

        public string bmapName {
            get {
                return FileUtil.FormatApplicationDataPathIfNE(_bmapName, Path.GetExtension(_bmapName));
            }
            set {
                if(!String.IsNullOrEmpty(_bmapName) && !_bmapName.Equals(value))
                {
                    _texture = null;
                    _bmapName = value;
                }
            }
        }

        public string GetBaseName() { return Path.GetFileNameWithoutExtension(_bmapName); }

        public Box2 imageBox {
            get {
                var result = new Box2();
                result.min = Vector2f.Zero;
                result.max = new Vector2f(texture.width, texture.height);
                return result;
            }
        }

        public TSPPointSets getPoints(float scale = 1f)
        {
            TSPPointSets psets = new TSPPointSets();

            Box2f imageBox = new Box2f
            {
                min = new Vector2f(0f, 0f),
                max = new Vector2f(texture.width, texture.height) * scale
            };
            var points = SierpinskiVectorTree.TreeToContainBox(imageBox, tspProbMaxCities);
            //var points = new List<Vector2f>();

            var tex = texture; 
            if(tex == null)
            {
                Debug.Log("null. bmap name: " + bmapName);
                return null;
            }

            Color pix;
            Vector2f v;
            for (int x = 0; x < tex.width; ++x)
            {
                for(int y = 0; y < tex.height; ++y)
                {
                    pix = tex.GetPixel(x, y);
                    if(pix.grayscale < .2f)
                    {
                        v = new Vector2f(x * scale, y * scale);
                        points.Add(v);
                    }
                }
            }

            //Debug.Log("this many dark enough points: " + points.Count + " out of: " + (tex.width * tex.height));
            var lists = points.GetAll().ToList();

            if(lists.Count == 0) //early return if no point lists
            {
                return psets;
            }

            var aggregateShortLists = lists[0];
            for(int i = 1; i < lists.Count; ++i)
            {
                var pointList = lists[i];
                if (splitPointSets && pointList.Count > 3)
                {
                    psets.Add(pointList);
                }
                else
                {
                    foreach(var p in pointList)
                    {
                        aggregateShortLists.Add(p);
                    }
                }
            }
            psets.Add(aggregateShortLists);
            //psets.Add(points);
            return psets;
        }

        public static Texture2D LoadPNG(string filePath)
        {

            Texture2D tex = null;
            byte[] fileData;

            if (File.Exists(filePath))
            {
                fileData = File.ReadAllBytes(filePath);
                tex = new Texture2D(2, 2);
                tex.LoadImage(fileData); //..this will auto-resize the texture dimensions.
            }
            return tex;
        }

    }
}
