using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using NanoSvg;
using SCParse;
using SCGenerator;
using g3;
using UnityEngine.UI;

namespace SCDisplay
{

    public class SCPathDisplay : MonoBehaviour
    {

        LineRenderer lr;
        TextMesh textProto;
        [SerializeField]
        AnimationCurve widthCurve;
        [SerializeField]
        float widthMult = 2f;

        private void Awake() {
            lr = GetComponent<LineRenderer>();
            if (!lr) {
                lr = gameObject.AddComponent<LineRenderer>();
            }
            //lr.material = new Material(Shader.Find("Diffuse"));

            textProto = GetComponentInChildren<TextMesh>();
        }

        public Color color {
            get { return lr.material.color; }
            set {
                lr.material.color = value;
            }
        }

        public void display(SvgParser.SvgPath path) {
            displayPoints(PathToVector.GetPointList(path));
        }

        public void display(PenPath penPath) {
            lr.positionCount = penPath.Count;
            //lr.widthMultiplier = widthMult;
            //lr.widthCurve = widthCurve;
            for(int i=0; i < penPath.Count; ++i) {
                PenMove move = penPath[i];
                if(move.hasColor) {
                    color = move.color;
                }
                if(move.text != null) {
                    textAt(move.destination, move.text);
                }
                lr.SetPosition(i, move.destination);
            }
        }

        private void textAt(Vector2f v, string s) {
            var textMesh = Instantiate(textProto);
            textMesh.text = s;
            textMesh.transform.position = v;
            textMesh.transform.parent = transform;
        }


        private void displayPoints(List<Vector2f> points) {
            lr.positionCount = points.Count;
            for (int i = 0; i < points.Count; ++i) {
                lr.SetPosition(i, points[i]);
            }
        }

    }
}
