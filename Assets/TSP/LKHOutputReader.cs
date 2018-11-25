using UnityEngine;
using UnityEditor;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SCPointGenerator
{
    [System.Serializable]
    public class LKHOutputReader 
    {

        [SerializeField]
        public string outFileName;

        public LKHOutputReader()
        {

        }

        public LKHOutputReader(string outFileName)
        {
            this.outFileName = outFileName;
        }


        public List<int> getIndices()
        {
            List<int> result = new List<int>();

            var path = outFileName;
            var sreader = new System.IO.StreamReader(path);

            string line;
            bool indexLines = false;
            while((line = sreader.ReadLine()) != null)
            {
                if (!indexLines)
                {
                    indexLines = line.StartsWith("TOUR_SECTION");
                    continue;
                }

                try
                {
                    int i = int.Parse(line);
                    if (i == -1) break;
                    result.Add(i);
                }
                catch
                {
                    Debug.Log("parse excepton");
                }
            }
            return result;
        }

        
    }
}
