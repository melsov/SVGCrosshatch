using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;


public static class FileUtil
{
    public static bool isValidFileName(string fileName) {
        return !string.IsNullOrEmpty(fileName) &&
              fileName.IndexOfAny(Path.GetInvalidFileNameChars()) < 0; 
    }

    public static bool fullPathExists(string fileName, string folderPath, out string combined) {
        combined = Path.Combine(folderPath, fileName);
        return File.Exists(combined);
    }

    public static bool hasSVGExtension(string fileName) {
        if(Path.HasExtension(fileName)) {
            return Path.GetExtension(fileName).ToLower().Equals("svg");
        }
        return false;
    }

    public static string OutGCodeFileOnDesktopWithFileName(string fileName) {
        string outF = Path.GetFileNameWithoutExtension(fileName);
        outF = Path.Combine(UserDesktopPath, outF);
        return string.Format("{0}.gcode", outF);
    }


    public static string SourceFolder(string fileName) {
        if(string.IsNullOrEmpty(fileName)) { return ""; }
        string[] elems;
        string glue = "/";
        if (fileName.Contains(@"\")) {
            elems = fileName.Split((@"\").ToCharArray());
            glue = @"\";
        } else {
            elems = fileName.Split('/');
        }

        if(elems.Length == 1) {
            return "";
        }

        return string.Join(glue, elems.Where((s, idx) => idx < elems.Length - 1).ToArray());
    }

    public static string AppendSVGExtension(string fileName) {
        if (hasSVGExtension(fileName)) { return fileName; }
        string noExt = fileName;
        if(Path.HasExtension(fileName)) {
            noExt = Path.GetFileNameWithoutExtension(fileName);
            noExt = Path.Combine(SourceFolder(fileName), noExt);
        }
        return string.Format("{0}.svg", noExt);
    }

    public static string UserDocumentsPath {
        get { return System.Environment.GetFolderPath(System.Environment.SpecialFolder.MyDocuments); }
    }

    public static string UserDesktopPath {
        get { return System.Environment.GetFolderPath(System.Environment.SpecialFolder.Desktop); }
    }

    public static bool validateSomewhere(string inFileNameOrPath, string tryFolder, out string validFullPath) {
        if(validate(inFileNameOrPath, tryFolder, out validFullPath)) {
            return true;
        }
        if(validate(inFileNameOrPath, UserDesktopPath, out validFullPath)) {
            return true;
        }
        if(validate(inFileNameOrPath, UserDocumentsPath, out validFullPath)) {
            return true;
        }
        return false;
    }

    public static bool validate(string inFileNameOrPath, string tryFolder, out string validFullPath) {
        if(!isValidFileName(inFileNameOrPath)) {
            validFullPath = ""; return false;
        }

        if(File.Exists(inFileNameOrPath)) {
            validFullPath = inFileNameOrPath;
            return true;
        }
        
        if(!inFileNameOrPath.Contains("/")) {
            string tryPath = Path.Combine(tryFolder, inFileNameOrPath);
            if(File.Exists(tryPath)) {
                validFullPath = tryPath;
                return true;
            }
        }

        validFullPath = "";
        return false;
    }

}
