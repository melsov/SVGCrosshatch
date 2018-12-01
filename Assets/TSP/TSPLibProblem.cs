using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using g3;
using System.Runtime.InteropServices;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

using System.IO;
using SCPointGenerator;
using SCUtil;

namespace SCGenerator
{

    public class TSPLibProblem
    {
        //public string parameterFileStr;
        public string problemFileStr;
        public int length;
        public float multiplier = 100f;

        public bool UseOutputFileIfAvailable = true; //want true for release 
        public bool OnlyUseOutputFilesDontRunSolver = true;

        bool DBUGOnlyWriteProbAndParam = false;

#if UNITY_EDITOR
        string OutputFolder = "LKHProbs~";
#else
        string OutputFolder = "LKHProbsRuntime";
#endif
        public string IndicesInOutFileBaseName;

        public float TimeLimitPerRunSeconds = 6f;
        public int Runs = 5;

        public static string _DefaultParam = @"
PROBLEM_FILE = {0}.tsp
OPTIMUM = 5
MOVE_TYPE = 5
PATCHING_C = 3
PATCHING_A = 2
RUNS = {1}
TIME_LIMIT = {2}
OUTPUT_TOUR_FILE={0}.out.txt
"; //NOTE: setting OPTIMUM = MINUS_INFINITY crashes LKH (and Unity along with it)

        public string GetParamString {
            get {
                return string.Format(_DefaultParam,
                    IndicesInOutFileBaseName,
                    Runs, 
                    TimeLimitPerRunSeconds);
            }
        }

        public static string ProblemFormat = @"NAME : GeneratedProblem
COMMENT : SVGCrosshatch
TYPE : TSP
DIMENSION : {0}
EDGE_WEIGHT_TYPE : EUC_2D
NODE_COORD_SECTION
{1}EOF
";

        public static string OutputFormat = @"NAME : {0}.tour
COMMENT : Length = 479929
COMMENT : Found by LKH [Keld Helsgaun] At some point in time
TYPE : TOUR
DIMENSION : TODO add valid length
TOUR_SECTION
{1}-1
EOF
";

        int[] indices;
        [DllImport("lkh", EntryPoint = "SolveTSP")]
        private static extern int SolveTSP(string paramFileStr, string probFileStr, int[] indices, int length);

        public void WriteParamAndProbWithBaseName(string name)
        {
            WriteParamAndProb(name, name);
        }

        void WriteParamAndProb(string paramFileName = "z-param", string probFileName = "z-prob") {

            if(!paramFileName.EndsWith(".par"))
                paramFileName += ".par";

            if (!probFileName.EndsWith(".tsp"))
                probFileName += ".tsp";

            string folder = OutputFolder; // "LKHProbs";

            System.IO.File.WriteAllText(string.Format("{0}/{1}/{2}", Application.dataPath, folder, paramFileName), GetParamString);
            System.IO.File.WriteAllText(string.Format("{0}/{1}/{2}", Application.dataPath, folder, probFileName), problemFileStr);
        }

        string FullOutputFolder {
            get {
                return Application.dataPath + "/" + OutputFolder;
            }
        }

        string OutPath {
            get {
                return  FullOutputFolder + "/" + IndicesInOutFileBaseName + ".out.txt";
            }
        }

        string ParFileName {
            get {
                return IndicesInOutFileBaseName + ".par";
            }
        }

        void WriteIndicesToOutputFile()
        {
            StringBuilder s = new StringBuilder();
            for(int i=0; i < indices.Length; ++i)
            {
                s.AppendLine(string.Format("{0}", indices[i]));
            }

            string outf = string.Format(OutputFormat, IndicesInOutFileBaseName, s.ToString());
            System.IO.File.WriteAllText(OutPath, outf);
        }

        static int[] indicesFromOutputFile(string path)
        {
            var lkhOutReader = new LKHOutputReader(path);
            return lkhOutReader.getIndices().ToArray();
        }

        public ProcessSettings GetTSPCommand()
        {
            return new ProcessSettings
            {
                args = ".\\LKHBinary~\\lkh.exe " + ParFileName,
                workingDirectory = Application.dataPath + "/LKHProbs~"
            };
        }

        public bool OutputFileExists() { return System.IO.File.Exists(OutPath); }

        public bool setIndicesFromOutputFile()
        {
            if (System.IO.File.Exists(OutPath))
            {
                indices = indicesFromOutputFile(OutPath);
                Debug.Log("got indices from file^^^^");
                return indices.Length == length;
            }
            return false;
        }

        /*
        public bool runSolver()
        {
            throw new Exception("please don't use this");
            dbugParamProbFiles();
            if (DBUGOnlyWriteProbAndParam)
            {
                return false;
            }

            if(UseOutputFileIfAvailable)
            {
                if (!System.IO.File.Exists(OutPath))
                {
                    FileUtil.RunShellCMD(
                        ".\\LKHBinary~\\lkh.exe " + ParFileName, // "python runMultiLKH.py " + ParFileName,
                        OutputFolder,
                        true);
                }
                if (setIndicesFromOutputFile()) { return true; }
            }

            if (OnlyUseOutputFilesDontRunSolver)
            {
                Debug.Log("didnt get output files. but won't run solver");
                return false;
            }

            indices = new int[length];
            int result = 0;
            if (length > 3)
            {
                Debug.Log("ind length: " + indices.Length);

                //DBUGWriteToFilesWithName(IndicesInOutFileBaseName);

                result = SolveTSP(GetParamString, problemFileStr, indices, length);
                Debug.Log("*****TSP result: " + result + "@@@@****************");
            }
            else //not enough points for a meaningful problem
            {
                for (int i = 0; i < length; ++i) indices[i] = i;
            }
            WriteIndicesToOutputFile();
            return result == 0;
        }
        */

        private void dbugParamProbFiles() {
            Debug.Log(GetParamString);
            Debug.Log(problemFileStr);
        }

        public int indexAt(int i) {
            return indices[i] - 1;
        }

        public TSPLibProblem(string BaseFileName) {
            IndicesInOutFileBaseName = BaseFileName;
        }

        public static TSPLibProblem FromPoints(IEnumerator<Vector2f> points, string IndicesInOutFileName, float multiplier = 100f)
        {
            TSPLibProblem tsp = new TSPLibProblem(IndicesInOutFileName);
            tsp.multiplier = multiplier;

            Debug.Log("will write to: " + IndicesInOutFileName);
            StringBuilder eucPoints = new StringBuilder(); 
            while(points.MoveNext())
            {
                Vector2f scaled = points.Current * tsp.multiplier;
                eucPoints.Append(string.Format("{0} {1} {2}\n", ++tsp.length, (int)scaled.x, (int)scaled.y));
            }
            tsp.problemFileStr = string.Format(ProblemFormat, tsp.length, eucPoints.ToString());
            tsp.WriteParamAndProbWithBaseName(tsp.IndicesInOutFileBaseName);
            return tsp;
        }
    }

    public static class TSPDemoProblem
    {
        public static int Length = 131;

        public static string ParamString = @"
PROBLEM_FILE = tempo_prob_file.tsp
OPTIMUM = 378032
MOVE_TYPE = 5
PATCHING_C = 3
PATCHING_A = 2
RUNS = 10
OUTPUT_TOUR_FILE=yay_output.txt
";

        public static string ProbString = @"
NAME : xqf131
COMMENT : Bonn VLSI data set with 131 points
COMMENT : Uni Bonn, Research Institute for Discrete Math
COMMENT : Contributed by Andre Rohe
TYPE : TSP
DIMENSION : 131
EDGE_WEIGHT_TYPE : EUC_2D
NODE_COORD_SECTION
1 0 13
2 0 26
3 0 27
4 0 39
5 2 0
6 5 13
7 5 19
8 5 25
9 5 31
10 5 37
11 5 43
12 5 8
13 8 0
14 9 10
15 10 10
16 11 10
17 12 10
18 12 5
19 15 13
20 15 19
21 15 25
22 15 31
23 15 37
24 15 43
25 15 8
26 18 11
27 18 13
28 18 15
29 18 17
30 18 19
31 18 21
32 18 23
33 18 25
34 18 27
35 18 29
36 18 31
37 18 33
38 18 35
39 18 37
40 18 39
41 18 41
42 18 42
43 18 44
44 18 45
45 25 11
46 25 15
47 25 22
48 25 23
49 25 24
50 25 26
51 25 28
52 25 29
53 25 9
54 28 16
55 28 20
56 28 28
57 28 30
58 28 34
59 28 40
60 28 43
61 28 47
62 32 26
63 32 31
64 33 15
65 33 26
66 33 29
67 33 31
68 34 15
69 34 26
70 34 29
71 34 31
72 34 38
73 34 41
74 34 5
75 35 17
76 35 31
77 38 16
78 38 20
79 38 30
80 38 34
81 40 22
82 41 23
83 41 32
84 41 34
85 41 35
86 41 36
87 48 22
88 48 27
89 48 6
90 51 45
91 51 47
92 56 25
93 57 12
94 57 25
95 57 44
96 61 45
97 61 47
98 63 6
99 64 22
100 71 11
101 71 13
102 71 16
103 71 45
104 71 47
105 74 12
106 74 16
107 74 20
108 74 24
109 74 29
110 74 35
111 74 39
112 74 6
113 77 21
114 78 10
115 78 32
116 78 35
117 78 39
118 79 10
119 79 33
120 79 37
121 80 10
122 80 41
123 80 5
124 81 17
125 84 20
126 84 24
127 84 29
128 84 34
129 84 38
130 84 6
131 107 27
EOF
";
    }

   
}
