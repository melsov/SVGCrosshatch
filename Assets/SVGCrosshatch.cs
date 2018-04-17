﻿using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using NanoSvg;
using SCDisplay;
using SCParse;
using SCGenerator;
using System;
using System.Linq;
using g3;
using UnityEngine.UI;
using SCGCode;
using System.Text.RegularExpressions;

public class SVGCrosshatch : MonoBehaviour {

    [SerializeField]
    string svgFile = @"star.svg";

    [SerializeField]
    bool yAxisInverted = true;

    [SerializeField]
    Texture2D canvas;
    Texture2D canvasClone;

    [SerializeField]
    Material mat;


    bool showWithLineRenderers {
        get { return dbugSettings.showWithLineRenderers;  }
    }
    int lineRendererLimit = 600;
    [SerializeField]
    SCPathDisplay pathDisplayPrefab;

    BMapPainter painter;
    MachineConfig machineConfig;
    GCodeWriter gCodeWriter;

    Color testColor = Color.cyan;

    [SerializeField]
    Text progressText;

    [SerializeField]
    private bool progressUpdates;

    private string DataFolder { get { return string.Format("{0}/Data/", Application.dataPath); } }

    bool finished;

    DbugSettings dbugSettings;

    public string svgFullPath {
        get {
            if(File.Exists(svgFile)) {
                return svgFile;
            }
            string svgFName = svgFile; 
            if(!svgFName.StartsWith("Data/")) {
                svgFName = string.Format("Data/{0}", svgFName);
            }
            if(!svgFName.EndsWith(".svg")) {
                svgFName = string.Format("{0}.svg", svgFName);
            }
            svgFile = Application.dataPath + "/" + svgFName;
            return svgFile;
        }
        set {
            if (value != null && value.Length > 0 && FileUtil.isValidFileName(value)) {
                print(value);
                
                string tryFileName = FileUtil.AppendSVGExtension(value);

                string fPath;
                if(FileUtil.validateSomewhere(tryFileName, DataFolder, out fPath )) {

                    svgFile = fPath;
                }
            } 
        }
    }



    public string svgFileNameNoExtension {
        get {
            return Path.GetFileNameWithoutExtension(svgFullPath);
        }
    }

    void Start () {
        StartCoroutine(lateStart());
	}


    float testFMods(float minY, float interval) {
        if (minY < 0f) {
            return minY - (minY % interval);
        }
        return minY + (interval - (minY % interval));
    }

    private IEnumerator lateStart() {
        yield return new WaitForSeconds(.2f);
        Redraw();
    }

    public string saveFullPath {
        get { return FileUtil.OutGCodeFileOnDesktopWithFileName(svgFileNameNoExtension); }
    }

    public bool save() {
        if(!finished) {
            Debug.Log("not finished");
            return false;
        }
        return gCodeWriter.saveLinesTo(saveFullPath);
    }

    private SvgParser.SvgPath getSvgPath(string filePath) {
        return SvgParser.SvgParse(File.ReadAllText(filePath));
    }

    void Setup() {
        finished = false;
        canvasClone = Instantiate(canvas);
        mat.mainTexture = canvasClone;
        machineConfig = FindObjectOfType<MachineConfig>();
        gCodeWriter = FindObjectOfType<GCodeWriter>();
        dbugSettings = FindObjectOfType<DbugSettings>();
        painter = new BMapPainter(canvasClone, machineConfig.paper);
      
    }

    private void clear() {
        Destroy(canvasClone);
        if (gCodeWriter != null)
            gCodeWriter.Reset();
        foreach (var pd in GetComponentsInChildren<SCPathDisplay>()) {
            Destroy(pd.gameObject);
        }
    }


    public void Redraw() {
        clear();
        Setup();
        _Main();
    }

    void _Main() {
        CSSLookup lookup = SCCSSParser.Parse(svgFullPath);
        SVGViewBox viewBox = SCCSSParser.ParseViewBox(svgFullPath);

        SvgParser.SvgPath plist = getSvgPath(svgFullPath);
        for(var it = plist; it != null; it = it.next) {
            lookup.updatePath(it);
        }

        HatchConfig hatchConfig = new HatchConfig();
        SCSvgFileData _svgFileData = new SCSvgFileData() { isYAxisInverted = yAxisInverted };
        StripeFieldConfig stripeFieldConfig = FindObjectOfType<StripeFieldConfig>();
        //stripeFieldConfig.setViewBox(viewBox);

        StripedPathSet stripedPathSet = new StripedPathSet(plist, stripeFieldConfig, _svgFileData);
        SCCrosshatchGenerator generator = new SCCrosshatchGenerator(machineConfig, hatchConfig, _svgFileData, viewBox);

        painter.lineWidth = 5f;
        painter.color = new Color(0f, 1f, .9f);
        painter.DrawBox(machineConfig.paper);
        painter.DrawBox(machineConfig.paper.expandUniform(-3f));
        painter.lineWidth = 2f;
        painter.color = Color.black;
        painter.applyTexture();
       

        if(progressUpdates) {
            StartCoroutine(getProgressiveCrosshatches(generator, stripedPathSet));
        } else {
            getCrosshatches(generator, stripedPathSet);
        }
    }

    void addPenSubscribers(SCPen pen) {
        if (dbugSettings.paintFromPenMoves) {
            pen.subscribe((PenUpdate pu) =>
            {
                painter.DrawPenUpdate(pu);
            });
        }
        pen.subscribe((PenUpdate pu) =>
        {
            gCodeWriter.addMoves(pu);
        });
    }

    void paintFromGCode() {
        if (!dbugSettings.paintFromGCode) { return; }
        if(dbugSettings.paintFromGCode && dbugSettings.paintFromPenMoves) {
            Debug.Log("Warning: drawing crosshatches twice ");
        }
        GCodeCursor gCodeCursor = new GCodeCursor();
        gCodeCursor.subscribe((CursorUpdate cu) =>
        {
            painter.DrawCursorUpdate(cu);
        });

        foreach(string line in gCodeWriter.getLines()) {
            gCodeCursor.moveTo(line);
        }

        painter.applyTexture();
    }

    void beFinished() {
        print("finished");
        finished = true;
    }

    IEnumerator getProgressiveCrosshatches(SCCrosshatchGenerator generator, StripedPathSet stripedPathSet) {
        SCPen pen = FindObjectOfType<SCPen>();
        addPenSubscribers(pen);

        int count = 0;
        foreach (PenDrawingPath penPath in generator.generate(stripedPathSet)) {
            pen.makeMoves(penPath);
            if(showWithLineRenderers) { lineRenderPenPath(penPath); }

            if (++count % 500 == 0) {
                yield return null;
                progressText.text = string.Format("{0}", count);
            }

        }
        progressText.text = string.Format("{0}", count);
        paintFromGCode();
        beFinished();
        painter.applyTexture();
    }


    void getCrosshatches(SCCrosshatchGenerator generator, StripedPathSet stripedPathSet) {
        var penIterator = generator.generate(stripedPathSet);
        SCPen pen = FindObjectOfType<SCPen>();
        addPenSubscribers(pen);

        foreach (PenDrawingPath penPath in penIterator) {
            pen.makeMoves(penPath);
        }

        if (showWithLineRenderers) {
            foreach (PenDrawingPath penPath in penIterator) {
                lineRenderPenPath(penPath);
            }
        }

        paintFromGCode();
        beFinished();
        painter.applyTexture();
    }


    private void testRotationsWith(List<PenDrawingPath> penIterator, SCPen pen, Matrix2f m) {
        foreach (PenDrawingPath penPath in penIterator) {
            penPath.Rotate(m);
        }
        foreach (PenDrawingPath penPath in penIterator) {
            if (showWithLineRenderers) {
                lineRenderPenPath(penPath);
            }
            pen.makeMoves(penPath);
        }
    }

    private SvgParser.SvgPath getSvgPath(object filePath) {
        throw new NotImplementedException();
    }

    private void lineRenderPath(SvgParser.SvgPath path) {
        SCPathDisplay display = Instantiate(pathDisplayPrefab); // go.AddComponent<SCPathDisplay>();
        display.color = testColor;
        display.transform.parent = transform;
        display.display(path);
    }

    private void lineRenderPenPath(PenDrawingPath path, GameObject go = null) {
        if(lineRendererLimit-- <= 0) { return; }
        SCPathDisplay display = Instantiate(pathDisplayPrefab); // go.AddComponent<SCPathDisplay>();
        display.color = testColor;
        display.transform.parent = go? go.transform : transform;
        display.display(path);
    }

    // Update is called once per frame
    void Update () {
		
	}
}
