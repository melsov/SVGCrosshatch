using SCGenerator;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using UnityEngine;
using UnityEngine.UI;

namespace SCGCode
{
    public class GCodeWriter : MonoBehaviour
    {
        List<string> lines;

        public GCodeWriter() {
            lines = new List<string>(1000);
            Reset();
        }

        public void Reset() {
            lines.Clear();
        }

        public void addMoves(PenUpdate pu) {
            lines.Add("hi");
        }

        public bool saveLinesTo(string fullPath) {
            Debug.Log("saving to: " + fullPath);
            try {
                using (StreamWriter outputFile = new StreamWriter(fullPath)) {
                    foreach (string line in lines) {
                        outputFile.WriteLine(line);
                    }
                }
            } catch(Exception) {
                return false;
            }
            return true;
        }
    }
}
