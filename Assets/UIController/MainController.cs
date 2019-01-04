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

        [SerializeField]
        Toggle bitMapModeToggle;

        [SerializeField]
        InputField maxCitiesField;

        private void Start() {
            svgCrosshatch = FindObjectOfType<SVGCrosshatch>();
            setup();
        }

        private void setup()
        {
            setupCrosshatchConfig();
            setInputFileField();
        }

        void setupCrosshatchConfig()
        {
            bitMapModeToggle.onValueChanged.AddListener(
                delegate 
                {
                    svgCrosshatch.isInBitmapMode = bitMapModeToggle.isOn;
                    setInputFileField();
                });

            bitMapModeToggle.isOn = svgCrosshatch.isInBitmapMode;
            maxCitiesField.text = "" + svgCrosshatch.MaxCities;

            maxCitiesField.onEndEdit.AddListener(delegate
            {
                OnEditMaxCitiesField(maxCitiesField);
            });

        }

        private void OnEditMaxCitiesField(InputField _maxCitiesField)
        {
            try
            {
                int m = int.Parse(_maxCitiesField.text);
                svgCrosshatch.MaxCities = m;
            } catch
            {
            }
            _maxCitiesField.text = "" + svgCrosshatch.MaxCities;
        }

        private void setInputFileField()
        {
            inFileField.text = svgCrosshatch.inputFilePath;
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
            svgCrosshatch.inputFilePath = inFileInputField;
            setInputFileField();
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
