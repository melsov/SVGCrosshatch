using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using g3;
using SCGenerator;
using System.Text.RegularExpressions;

namespace SCGCode
{

    public struct VelocityV3f
    {
        public Vector3f position;
        public float velocity;
        public bool validPosition;

        public void apply(GMoveComponent gmc) {
            switch(char.ToUpper(gmc.command)) {
                case 'F':
                    velocity = gmc.scalar;
                    break;
                case 'X':
                    position.x = gmc.scalar;
                    validPosition = true;
                    break;
                case 'Y':
                    position.y = gmc.scalar;
                    validPosition = true;
                    break;
                case 'Z':
                    position.z = gmc.scalar;
                    validPosition = true;
                    break;
                default:
                    break;
            }
        }

        public static implicit operator Vector3f(VelocityV3f vv) { return vv.position; }
    }

    public struct GMoveComponent
    {
        public char command;
        public float scalar;

        public static char[] ValidMoveCommands = new char[] { 'G', 'g', 'F', 'f', 'X', 'x', 'Y' ,'y', 'Z', 'z' };
        public static bool isValidCommand(char c) { return ValidMoveCommands.Contains(c); }
        public static char InvalidChar = '^'; 

        public static GMoveComponent Invalid() {
            return new GMoveComponent() { command = InvalidChar };
        }

        public bool isValid {
            get { return command != InvalidChar && ValidMoveCommands.Contains(command); }
        }

    }

    public static class GCodeToVec3
    {

        private static int next(string[] compos, int startIndex, out GMoveComponent gComponent) {

            string subject;
            string scalarS = "";
            char command = GMoveComponent.InvalidChar;

            while(startIndex < compos.Length) {
                subject = compos[startIndex++].Trim();
                if(string.IsNullOrEmpty(subject)) { continue; }
                if(!GMoveComponent.isValidCommand(subject[0])) { continue; }

                command = subject[0];
                if(subject.Length == 1) {
                    if(++startIndex < compos.Length) {
                        scalarS = compos[startIndex];
                    }
                    break;
                } 

                scalarS = subject.Substring(1);
                break;
            }

            try {
                gComponent = new GMoveComponent() { command = command, scalar = float.Parse(scalarS) };
            } catch (Exception e) {
                gComponent = GMoveComponent.Invalid();
            }
            return startIndex;
        }

        public static IEnumerable<GMoveComponent> FromLine(string gcode) {
            string[] compos = Regex.Split(gcode, @"\s+");
            int i = 0;
            GMoveComponent gComponent;
            while(i < compos.Length) {
                i = next(compos, i, out gComponent);
                yield return gComponent;
            }
        }

        //public static VelocityV3f FromGCode(string gcode) {

        //    string[] compos = Regex.Split(gcode, @"\s+");
        //    int i = 0;
        //    GMoveComponent gComponent;
        //    VelocityV3f vv = default(VelocityV3f);
        //    while(i < compos.Length) {
        //        i = next(compos, i, out gComponent);
        //        vv.apply(gComponent);
        //    }
        //    return vv;
        //}
    }
}
