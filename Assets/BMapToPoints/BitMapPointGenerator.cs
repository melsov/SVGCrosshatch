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
        public Texture2D texture {
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

        //[SerializeField, Header("For colored/grayscale tri tree"), Range(0.001f, .99f)]
        //public double deviationThreshold = .05d;

        [Header("In: tri grayscale mean, Out: deviation threshold spread, scaled 01")]
        public AnimationCurve grayscaleToDeviationCurve01;

        public float minDeviationThreshold = .1f;
        public float deviationSpread = .4f;

        private bool TriShouldSplit(IsoTriangle<PixelTriData> tri)
        {
            return 
                minDeviationThreshold + deviationSpread * grayscaleToDeviationCurve01.Evaluate((float)tri.onlineNormalEstimator.mean()) < 
                tri.onlineNormalEstimator.standardDeviationUnbiased();
        }

        [SerializeField, Header("Max grayscale")]
        public float MaxGrayScale = .85f;

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

        public Box2 inputImageBox {
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
            var sierTree = SierpinskiVectorTree.TreeToContainBox(imageBox, tspProbMaxCities);

            var tex = texture;
            if (tex == null)
            {
                Debug.Log("null. bmap name: " + bmapName);
                return null;
            }

            Color pix;
            Vector2f v;
            for (int x = 0; x < tex.width; ++x)
            {
                for (int y = 0; y < tex.height; ++y)
                {
                    pix = tex.GetPixel(x, y);
                   
                    if (pix.grayscale < .2f)
                    {
                        v = new Vector2f(x * scale, y * scale);
                        sierTree.Add(v);
                    }
                }
            }

            //Debug.Log("this many dark enough points: " + points.Count + " out of: " + (tex.width * tex.height));
            List<List<Vector2f>> lists;

            lists = sierTree.GetAll().ToList();
         

            // early out
            if (lists.Count == 0) return psets;

            var aggregateShortLists = lists[0];
            for (int i = 1; i < lists.Count; ++i)
            {
                var pointList = lists[i];
                if (splitPointSets && pointList.Count > 3)
                {
                    psets.Add(pointList);
                }
                else
                {
                    foreach (var p in pointList)
                    {
                        aggregateShortLists.Add(p);
                    }
                }
            }
            psets.Add(aggregateShortLists);

            return psets;
        }

        public TriTree<PixelTriData> getPixelTriTree(float scale = 1f)
        {
            //TSPPointSets psets = new TSPPointSets();

            Box2f imageBox = new Box2f
            {
                min = new Vector2f(0f, 0f),
                max = new Vector2f(texture.width, texture.height) * scale
            };
            var triTree = TriTree<PixelTriData>.TreeToContainBox<PixelTriData>(imageBox, tspProbMaxCities);
            //triTree.deviationThreshold = deviationThreshold;

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
                    //
                    // TODO: handle grey scale images
                    // Tricky: define regions based on blobs of pixels of similar grey-ness
                    // Would help if: triangle regions (or square regions) could join themselves into sets (of similar grey levels)?
                    // Try: divide picture into 'a lot' of triangles
                    // At some point, each triangle should check it's members for tonal homogeneity.
                    // If not enough homogeneity, divide again.
                    // This would include white pixels!
                    // With this set of homogeneous tone triangles
                    // cull the points in each set based on median tone.
                    // lighter tone, more points get culled
                    // cull by again dividing the triangle, fewer times for lighter
                    // each sub triangle gives only one point. (perhaps the average)
                    // Sufficiently dark tonal regions can skip the second divide: we just want all of their points.
                    // Sufficiently filled triangles can skip the averaging, just provide the center point.
                    //
                    //if(pix.grayscale < MaxGrayScale)
                    //{
                    v = new Vector2f(x * scale, y * scale);
                    var tri = triTree.root.Add(new PixelTriData(new PixelV {
                        pos = v,
                        color = pix
                    }));

                    if(tri != null)
                    {
                        tri.DivideR((IsoTriangle<PixelTriData> _tri) =>
                        {
                            return TriShouldSplit(_tri);
                        });
                    }

                    //}
                }
            }

            return triTree;
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
