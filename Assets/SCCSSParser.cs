using g3;
using NanoSvg;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Xml;
using UnityEngine;
using VctorExtensions;

namespace SCParse
{
    public static class SCCSSParser
    {
        private static XmlDocument FromPath(string svgFilePath) {
            XmlDocument xml = new XmlDocument();
            string sxml = File.ReadAllText(svgFilePath);
            xml.LoadXml(sxml);
            return xml;
        }

        public static CSSLookup Parse(string svgFilePath) {
            CSSLookup lookup = new CSSLookup();
            XmlDocument xml = FromPath(svgFilePath);

            var els = xml.GetElementsByTagName("style");
            foreach (XmlNode node in els) {
                foreach (var nameDef in GetClassNamesAndDefinitions(node.InnerText)) {
                    var cssC = CSSClass.FromString(nameDef[0], nameDef[1]);
                    lookup.add(cssC);
                }
            }

            return lookup;
        }

        public static SVGViewBox ParseViewBox(string svgFilePath) {
            var els = FromPath(svgFilePath).GetElementsByTagName("svg");
            XmlNode svgNode = els[0];
            var vbstr = svgNode.Attributes["viewBox"].Value;
            return SVGViewBox.FromAttributeString(vbstr);
        }

        private static IEnumerable<string[]> GetClassNamesAndDefinitions(string innerText) {
            int cursor = 0, openBracketIndex = 0, closedBracketIndex = 0;

            while (true) {
                cursor = innerText.IndexOf('.');
                if (cursor == -1) {
                    break;
                }
                innerText = innerText.Substring(cursor, innerText.Length - cursor);
                cursor = 0;
                openBracketIndex = innerText.IndexOf('{');
                closedBracketIndex = innerText.IndexOf('}');
                if (openBracketIndex == -1 || closedBracketIndex == -1) { Debug.LogWarning("we are in the woods here"); break; }
                string name = innerText.Substring(1, openBracketIndex - 1);
                string cdef = innerText.Substring(openBracketIndex + 1, closedBracketIndex - openBracketIndex - 1);
                yield return new string[] { name, cdef };
                innerText = innerText.Substring(closedBracketIndex, innerText.Length - closedBracketIndex);
            }

        }
    }

    [System.Serializable]
    public class Box2
    {
        [SerializeField]
        public Vector2 min, max;

        public Vector2 size { get { return max - min; } }

        public float getFitScale(Vector2 reference) {
            Vector2 div = reference.divide(size);
            return Mathf.Min(div.x, div.y);
        }

        public override string ToString() {
            return string.Format("ViewBox: min: {0} max: {1}", min.ToString(), max.ToString());
        }

        public Vector2 corner(int i) {
            switch(i) {
                case 0:
                    return min;
                case 1:
                    return new Vector2(max.x, min.y);
                case 2:
                    return max;
                case 3:
                default:
                    return new Vector2(min.x, max.y);
            }
        }

        public IEnumerable<Vector2> corners() {
            for(int i = 0; i<4; ++i) { yield return corner(i); }
        }

        public IEnumerable<Edge2f> getSides() {
            for(int i=0; i<4;++i) {
                yield return new Edge2f(corner(i), corner((i + 1) % 4));
            }
        }

        public Box2 scaled(float scale, bool fromMinCorner = false) {
            if (fromMinCorner) {
                return new Box2() { min = min, max = min + max * scale };
            }
            Vector2 sz = size * (scale - 1f);
            return new Box2() { min = min - sz / 2f, max = max + sz / 2f };
        }

        public Box2 expandUniform(float expand, bool fromMinCorner = false) {
            if(fromMinCorner) {
                return new Box2() {
                    min = min,
                    max = new Vector2(max.x + expand, max.y + expand) };
            }
            return new Box2() {
                min = new Vector2(min.x - expand / 2f, min.y - expand / 2f),
                max = new Vector2(max.x + expand / 2f, max.y + expand / 2f) };
        }
    }

    public class SVGViewBox : Box2
    {
        
        public static SVGViewBox FromAttributeString(string s) {
            List<string> ns = new List<string>(s.Split(' '));
            ns.RemoveAll((string ss) => { return string.IsNullOrEmpty(ss); });
            if(ns.Count != 4) {
                throw new Exception("expecting 4 numbers in view box definition. got " + ns.Count);
            }

            var dims = new List<float>(4);
            foreach(string d in ns) {
                dims.Add(float.Parse(d));
            }
            Vector2 min = new Vector2(dims[0], dims[1]);
            Vector2 max = new Vector2(dims[2], dims[3]);

            return new SVGViewBox() { min = min, max = max };
        }


    }

    public class CSSLookup
    {
        Dictionary<string, CSSClass> storage = new Dictionary<string, CSSClass>();

        public CSSClass this[string name] {
            get {
                if (storage.ContainsKey(name)) { return storage[name]; }
                return null;
            }
        }

        public void add(CSSClass ccs) {
            storage.Add(ccs.name, ccs);
        }

        public void updatePath(SvgParser.SvgPath path)
        {
            if(path._class != null && storage.ContainsKey(path._class))
            {
                path.updateWith(storage[path._class]);
            }
            else {
                Debug.Log("no class: " + path._class);
            }
        }
    }

    public class CSSClass
    {
        public string name;
        public string fill { get { return getVal("fill"); } }
        public bool hasFill { get { return fill != null; } }
        public string stroke { get { return getVal("stroke"); } }
        public bool hasStroke { get { return stroke != null; } }
        public float strokeWidth {
            get {
                string s = getVal("stroke-width");
                if(s == null) {
                    return 2f;
                }
                float ret = float.Parse(s);
                if(hasStroke) {
                    ret = Mathf.Max(.1f, ret);
                }
                return ret;
            }
        }

        private CSSClass() { }

        private string getVal(string k) {
            if (defs.ContainsKey(k)) { return defs[k]; }
            return null;
        }

        private Dictionary<string, string> defs = new Dictionary<string, string>();

        public static CSSClass FromString(string className, string definition) {
            CSSClass result = new CSSClass();
            result.name = className;
            string[] defs = definition.Split(';');
            foreach(string def in defs) {
                string[] keyVal = def.Split(':');
                if(keyVal.Length != 2) { continue; }
                string key = keyVal[0].Trim();
                string val = keyVal[1].Trim();
                result.defs.Add(key, val);
            }
            return result;
        }

        public override string ToString() {
            return string.Format("CSSClass: {0}. fill {1} . stroke {2} . stroke-width: {3}", name, fill, stroke, strokeWidth);
        }
    }

}
