using UnityEngine;
using UnityEngine.UI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;

namespace UIController
{
    public class MainController : MonoBehaviour
    {
        SVGCrosshatch svgCrosshatch;
        [SerializeField]
        InputField inFileField;

        [SerializeField]
        Text fileNameDisplay;

        [SerializeField]
        InputField savedToField;

        private void Start() {
            svgCrosshatch = FindObjectOfType<SVGCrosshatch>();
            setInFileField();
        }

        private void setInFileField() {
            inFileField.text = svgCrosshatch.svgFullPath;
            if(File.Exists(inFileField.text)) {
                inFileField.GetComponent<Image>().color = Color.green;
                fileNameDisplay.text = Path.GetFileName(inFileField.text);
            } else {
                inFileField.GetComponent<Image>().color = Color.red;
                fileNameDisplay.text = "...";
            }
        }

        public void pressedRedraw() {
            svgCrosshatch.Redraw();
        }

        public void onEndEditInFileName(string inFileInputField) {
            svgCrosshatch.svgFullPath = inFileInputField;
            setInFileField();
        }


        public void save() {
            if(svgCrosshatch.save()) {
                savedToField.GetComponent<Image>().color = new Color(.3f, 1f, .8f);
                savedToField.text = svgCrosshatch.saveFullPath;
            } else {
                savedToField.GetComponent<Image>().color = Color.red;
                savedToField.text = "Not saved";
            }
        }
    }
}
