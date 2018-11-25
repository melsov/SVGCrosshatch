using UnityEngine;
using UnityEditor;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Diagnostics;

namespace SCTest
{
    public class TestScratchPad : MonoBehaviour
    {
        [MenuItem("SC/Test RW To Hidden Folder")]
        static void TestRWToHidden()
        {
            string hidden = "aTestHidden~/";
            string f = "aFile.txt";
            string full = string.Format("{0}/{1}/{2}", Application.dataPath, hidden, f);

            string[] content = { "this is a test", "another line" };
            File.WriteAllLines(full, content);

            var sreader = new System.IO.StreamReader(full);

            string line;
            while ((line = sreader.ReadLine()) != null)
            {
                print(line);
            }


        }

        [MenuItem("SC/Test Create Folder")]
        static void TestCreateFolder()
        {

            string guid = AssetDatabase.CreateFolder("Assets", "_TestSquig2~");
            string path = AssetDatabase.GUIDToAssetPath(guid);
            print(path);
        }

        [MenuItem("SC/Test Run Command")]
        static void TestRunCommand()
        {
            //string curDir = Environment.CurrentDirectory;
            //print("cur dir" + curDir);

            //string script = "_testLS.py";
            //ProcessStartInfo startInfo = new ProcessStartInfo(Application.dataPath + "/LKHProbs~", "python");
            //startInfo.WindowStyle = ProcessWindowStyle.Normal;
            //startInfo.Arguments = "_testLS.py bomb";
            //Process p = Process.Start(startInfo);

            //startcamera();
            //doCMD();
            //altCMD();
            doCMDA();
        }

        static int caminterrupt;

        static void doCMD()
        {
            string strCmdText;
            strCmdText = "/C dir"; // copy /b Image1.jpg + Archive.rar Image2.jpg";
            System.Diagnostics.Process.Start("CMD.exe", strCmdText);
        }

        static void doCMDA()
        {
            string wdir = Application.dataPath + "/LKHProbs~";

            System.Diagnostics.Process process = new System.Diagnostics.Process();
            System.Diagnostics.ProcessStartInfo startInfo = new System.Diagnostics.ProcessStartInfo();
            startInfo.WindowStyle = System.Diagnostics.ProcessWindowStyle.Normal;
            startInfo.FileName = "cmd.exe";
            startInfo.WorkingDirectory = wdir;

            startInfo.Arguments = "/C python _testLS.py";
            process.StartInfo = startInfo;



            process.Start();
            process.WaitForExit();
            print("done");
        }

        static void altCMD()
        {
            string wdir = Application.dataPath + "/LKHProbs~";

            Process cmd = new Process();
            cmd.StartInfo.FileName = "cmd.exe";
            cmd.StartInfo.RedirectStandardInput = true;
            cmd.StartInfo.RedirectStandardOutput = true;
            cmd.StartInfo.CreateNoWindow = true;
            cmd.StartInfo.UseShellExecute = false;

            cmd.StartInfo.WorkingDirectory = wdir;

            cmd.Start();

            cmd.StandardInput.WriteLine("/C touch _hello.txt"); // echo Oscar");
            cmd.StandardInput.Flush();
            cmd.StandardInput.Close();
            cmd.WaitForExit();
            print(cmd.StandardOutput.ReadToEnd());
        }

        public static void startcamera()
        {

            //string strOutput;

            //Starting Information for process like its path, use system shell i.e. 
            //control process by system etc.

            ProcessStartInfo psi = 
                new ProcessStartInfo(@"C:\Windows\System32\WindowsPowerShell\v1.0\powershell.exe"); // C:\ConsoleExample\bin\Debug\ConsoleExample.exe");
            
            // its states that system shell will not be used to control the process 
            // instead program will handle the process
            psi.UseShellExecute = false;
            psi.ErrorDialog = false;
            // Do not show command prompt window separately
            //psi.CreateNoWindow = true;
            //psi.WindowStyle = ProcessWindowStyle.Hidden;
            //redirect all standard inout to program
            psi.RedirectStandardError = true;
            psi.RedirectStandardInput = true;
            psi.RedirectStandardOutput = true;

            psi.Arguments = "dir ";
            //create the process with above infor and start it
            Process plinkProcess = new Process();
            plinkProcess.StartInfo = psi;
            plinkProcess.Start();
            //link the streams to standard inout of process
            StreamWriter myStream = new StreamWriter(plinkProcess.StandardInput.BaseStream, Encoding.ASCII);

            //send command to cmd prompt and wait for command to execute with thread 
            ///sleep


            refire:
            if (caminterrupt == 1)
            {

                myStream.WriteLine("y");
                //yield return new WaitForSeconds(1);
                goto refire;

            }
            if (caminterrupt == 0)
            {
                myStream.WriteLine("e");

            }


            myStream.WriteLine("e");

            // flush the input stream before sending exit command to end process for 
            // any unwanted characters

            myStream.Close();

            plinkProcess.Close();
            caminterrupt = 0;

        }


    }
}
