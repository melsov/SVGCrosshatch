using g3;
using SCGenerator;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using UnityEngine;
using UnityEngine.UI;
using VctorExtensions;

namespace SCGCode
{
    public class GCodeWriter : MonoBehaviour
    {

        List<string> startFileLines;
        List<string> lines;
        MachineConfig machineConfig;
        private string floatFormatter;

        void Start() {
            startFileLines = new List<string>();
            lines = new List<string>(1000);
            machineConfig = FindObjectOfType<MachineConfig>();
            Reset();
            floatFormatter = "0.";
            for(int i = 0; i < machineConfig.moveCoordFloatPrecision; ++i) { floatFormatter = string.Format("{0}0", floatFormatter); }
        }

        public void Reset() {
            lines.Clear();
        }

        public void addMoves(PenUpdate pu) {
            int updown = 0;
            if (pu.from == null || pu.to.up != pu.from.up ) {
                updown = pu.to.up ? 1 : -1;
            }
            if(updown != 0) {
                lines.Add(penUpDown(updown < 0));
            }
            lines.Add(move(pu.to.destination, updown));
        }

        private string penUpDown(bool down) {
            if(down) {
                return string.Format("G1 Z{0} F{1}", machineConfig.maxPenetrationDepthMM, machineConfig.penetrationRateMMS);
            } else {
                return string.Format("G0 Z{0}", machineConfig.travelHeightMM);
            }
        }


        private string move(Vector2f destination, int updown) {
            return string.Format("G{0} X{1} Y{2} {3}", 
                updown == 1 ? "0" : "1", 
                destination.x.ToString(floatFormatter), 
                destination.y.ToString(floatFormatter), 
                getFString(updown));
        }

        private string getFString(int updown) {
            switch(updown) {
                case 0:
                case 1:
                default:
                    return "";
                case -1:
                    return string.Format("F{0}", machineConfig.penetrationRateMMS);
            }
        }

        private IEnumerable<string> moveHome() {
            yield return penUpDown(false);
            yield return move(machineConfig.homePositionOffsetMM.toVector2f(), 1);
            yield return penUpDown(true);
        }

        public bool saveLinesTo(string fullPath) {
            Debug.Log("saving to: " + fullPath);
            try {
                using (StreamWriter outputFile = new StreamWriter(fullPath)) {
                    outputFile.Write(machineConfig.header.ToCharArray());
                    outputFile.WriteLine();
                    foreach(string line in moveHome()) {
                        outputFile.WriteLine(line);
                    }
                    foreach (string line in lines) {
                        outputFile.WriteLine(line);
                    }
                    outputFile.WriteLine();
                    outputFile.Write(machineConfig.footer.ToCharArray());
                }
            } catch(Exception) {
                return false;
            }
            return true;
        }
    }
}
