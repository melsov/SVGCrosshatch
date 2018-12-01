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

        Vector2f cursor;
        bool isCursorUp;

        void Start() {
            startFileLines = new List<string>();
            lines = new List<string>(1000);
            machineConfig = FindObjectOfType<MachineConfig>();
            Reset();
            floatFormatter = "0.";
            for(int i = 0; i < machineConfig.moveCoordFloatPrecision; ++i) { floatFormatter = string.Format("{0}0", floatFormatter); }
        }

        public void Reset() {
            Debug.Log("RESET GCodeWriter***");
            lines.Clear();
        }

        public void addMoves(PenUpdate pu) {

            List<PenMove> moves = pu.drawPath.GetMoves().ToList();
            if(moves.Count == 0) { return; }
            if(moves.Count == 1) { Debug.LogWarning("why?"); return; }
            PenMove first = moves[0];

            if(!machineConfig.isSameXY(cursor, first.destination)) {
                if (!isCursorUp)
                    lines.Add(penUp());
                lines.Add(move(first.destination, 1));
            }

            if(isCursorUp) {
                lines.Add(penDown());
            }

            PenMove next = moves[1];
            lines.Add(move(next.destination, -1));

            for(int i = 2; i < moves.Count; ++i) {
                next = moves[i];
                lines.Add(move(next.destination, 0));
            }


        }


        private string penUp() { return penUpDown(true); }
        private string penDown() { return penUpDown(false); }

        private string penUpDown(bool up) {
            isCursorUp = up;
            if(!up) {
                return string.Format("G1 Z{0} F{1}", machineConfig.maxPenetrationDepthMM, machineConfig.penetrationRateMMS);
            } else {
                return string.Format("G0 Z{0}", machineConfig.travelHeightMM);
            }
        }

        /// <summary>
        /// Returns gcode to move to destination
        /// </summary>
        /// <param name="destination"></param>
        /// <param name="updown">1 is up</param>
        /// <returns></returns>
        private string move(Vector2f destination, int updown) {
            cursor = destination;
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
                    return string.Format("F{0}", machineConfig.feedRateMMS);
            }
        }

        private IEnumerable<string> moveHome() {
            yield return penUpDown(true);
            yield return move(machineConfig.homePositionOffsetMM.toVector2f(), 1);
            //yield return penUpDown(false);
        }

        private IEnumerable<string> returnHome(bool penDown = false) {
            yield return "(return home)";
            yield return penUpDown(true);
            yield return move(machineConfig.homePositionOffsetMM.toVector2f(), 1);
            if(penDown)
                yield return penUpDown(false); 
            yield return "(end return home)";
        }

        private IEnumerable<string> tourPaperBounnds()
        {
            yield return "(tour paper bounds)";
            yield return penUpDown(true);
            var home = machineConfig.homePositionOffsetMM.toVector2f();

            var corners = new List<Vector2>(machineConfig.paper.corners());

            for(int i = 0; i <= corners.Count; ++i)
            {
                yield return move(home + (Vector2f)corners[i % corners.Count], 1);
            }
            yield return "(end tour paper bounds)";
        }

        public IEnumerable<string> getLines() {
            foreach(string line in lines) { yield return line; }
        }

        public bool saveLinesTo(string fullPath) {
            Debug.Log("saving to: " + fullPath);
            try {
                using (StreamWriter outputFile = new StreamWriter(fullPath)) {
                    outputFile.Write(machineConfig.header.ToCharArray());
                    outputFile.WriteLine();

                    foreach(string line in tourPaperBounnds())
                    {
                        outputFile.WriteLine(line);
                    }

                    outputFile.WriteLine(@"(Move home)");
                    foreach(string line in moveHome()) {
                        outputFile.WriteLine(line);
                    }
                    outputFile.WriteLine(@"(End move home)");

                    foreach (string line in lines) {
                        outputFile.WriteLine(line);
                    }

                    foreach(string line in returnHome()) {
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
