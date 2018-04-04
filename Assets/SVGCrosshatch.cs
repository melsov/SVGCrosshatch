using System.Collections;
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

public class SVGCrosshatch : MonoBehaviour {

    [SerializeField]
    string svgFile = @"Data/star.svg";

    [SerializeField]
    bool yAxisInverted = true;

    [SerializeField]
    Texture2D canvas;

    [SerializeField]
    Material mat;

    [SerializeField]
    bool showWithLineRenderers;
    [SerializeField]
    SCPathDisplay pathDisplayPrefab;

    BMapPainter painter;
    MachineConfig machineConfig;
    GCodeWriter gCodeWriter;

    Color testColor = Color.cyan;

    [SerializeField]
    private bool drawPathsOnTexture;

    [SerializeField]
    Text progressText;

    [SerializeField]
    private bool progressUpdates;

    private string DataFolder { get { return string.Format("{0}/Data/", Application.dataPath); } }

    bool finished;

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

    void testFMs() {
        float interval = 1f;
        for(float f = -3f; f < 3f; f += .2f) {
            Debug.Log(string.Format("next above: {0} is {1}", f, testFMods(f, interval)));
        }
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
        Texture2D clone = Instantiate(canvas);
        mat.mainTexture = clone;
        machineConfig = FindObjectOfType<MachineConfig>();
        gCodeWriter = FindObjectOfType<GCodeWriter>();
        painter = new BMapPainter(clone, machineConfig.paper);
      
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

        painter.DrawBox(machineConfig.paper);
        painter.applyTexture();
       

        if(progressUpdates) {
            StartCoroutine(getProgressiveCrosshatches(generator, stripedPathSet));
        } else {
            getCrosshatches(generator, stripedPathSet);
        }
    }

    void addPenSubscribers(SCPen pen) {
        if (drawPathsOnTexture) {
            pen.subscribe((PenUpdate pu) =>
            {
                painter.DrawPenMove(pu);
            });
        }
        pen.subscribe((PenUpdate pu) =>
        {
            gCodeWriter.addMoves(pu);
        });
    }

    void beFinished() {
        print("finished");
        finished = true;
    }

    IEnumerator getProgressiveCrosshatches(SCCrosshatchGenerator generator, StripedPathSet stripedPathSet) {
        SCPen pen = FindObjectOfType<SCPen>();
        addPenSubscribers(pen);

        int count = 0;
        foreach (PenPath penPath in generator.generate(stripedPathSet)) {
            pen.makeMoves(penPath);
            if(showWithLineRenderers) { lineRenderPenPath(penPath); }

            if (++count % 500 == 0) {
                yield return null;
                progressText.text = string.Format("{0}", count);
            }

        }
        progressText.text = string.Format("{0}", count);
        beFinished();
        painter.applyTexture();
    }


    void getCrosshatches(SCCrosshatchGenerator generator, StripedPathSet stripedPathSet) {
        var penIterator = generator.generate(stripedPathSet);
        SCPen pen = FindObjectOfType<SCPen>();
        addPenSubscribers(pen);

        foreach (PenPath penPath in penIterator) {
            pen.makeMoves(penPath);
        }

        if (showWithLineRenderers) {
            foreach (PenPath penPath in penIterator) {
                lineRenderPenPath(penPath);
            }
        }


        beFinished();
        painter.applyTexture();
    }


    private void testRotationsWith(List<PenPath> penIterator, SCPen pen, Matrix2f m) {
        foreach (PenPath penPath in penIterator) {
            penPath.Rotate(m);
        }
        foreach (PenPath penPath in penIterator) {
            if (showWithLineRenderers) {
                lineRenderPenPath(penPath);
            }
            pen.makeMoves(penPath);
        }
    }

    private SvgParser.SvgPath getSvgPath(object filePath) {
        throw new NotImplementedException();
    }

    private void clear() {
        if(gCodeWriter != null)
            gCodeWriter.Reset();
        foreach(var pd in GetComponentsInChildren<SCPathDisplay>()) {
            Destroy(pd.gameObject);
        }
    }

    private void lineRenderPath(SvgParser.SvgPath path) {
        SCPathDisplay display = Instantiate(pathDisplayPrefab); // go.AddComponent<SCPathDisplay>();
        display.color = testColor;
        display.transform.parent = transform;
        display.display(path);
    }

    private void lineRenderPenPath(PenPath path, GameObject go = null) {
        SCPathDisplay display = Instantiate(pathDisplayPrefab); // go.AddComponent<SCPathDisplay>();
        display.color = testColor;
        display.transform.parent = go? go.transform : transform;
        display.display(path);
    }

    // Update is called once per frame
    void Update () {
		
	}
}
