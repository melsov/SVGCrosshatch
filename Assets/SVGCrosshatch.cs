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
using System.Text.RegularExpressions;
using System.Runtime.InteropServices;
using SCPointGenerator;

public class SVGCrosshatch : MonoBehaviour
{

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

    MachineConfig machineConfig;
    [SerializeField]
    GeneratorConfig _generatorConfig;

    public GeneratorConfig generatorConfig {
        get {
            return _generatorConfig;
        }
    }

    BMapPainter painter;

    GCodeWriter gCodeWriter;

    Color testColor = Color.cyan;

    [SerializeField]
    Text progressText;

    [SerializeField]
    Text timeScaleCommentText;

    [SerializeField]
    MeshRenderer pixelTreeDebugDisplay;



    void updateTimeScaleComment()
    {
        if (generator != null)
        {
            timeScaleCommentText.text = generator.GetProcessIntensityComments();
        }
    }

    [SerializeField]
    private bool progressUpdates;

    private string DataFolder { get { return string.Format("{0}/Data/", Application.dataPath); } }

    bool finished;

    DbugSettings dbugSettings;

    enum GeneratorType
    {
        Crosshatch, TSPCrosshatch, Triangles
    }
    [SerializeField]
    GeneratorType generatorType;

    [SerializeField, Header("Get points from an image, instead of an SVG file")]
    bool useBitMapPointGenerator;
    
    [SerializeField]
    BitMapPointGenerator bitMapPointGenerator;

    public bool isInBitmapMode {
        get {
            return useBitMapPointGenerator;
        }
        set {
            useBitMapPointGenerator = value;
        }
    }

    public int MaxCities {
        get {
            return bitMapPointGenerator.tspProbMaxCities;
        }
        set {
            bitMapPointGenerator.tspProbMaxCities = value;
        }
    }

    public string inputFilePath {
        get {
            return useBitMapPointGenerator ? bitmapFullPath : svgFullPath;
        }
        set {
            if (useBitMapPointGenerator)
            {
                bitmapFullPath = value;
            } else
            {
                svgFullPath = value;
            }
        }
    }

    string bitmapFullPath {
        get {
            return bitMapPointGenerator.bmapName;
        }
        set {
            if (FileUtil.isValidFileName(value))
            {
                var tryFile = FileUtil.AppendExtensionIfMissing(value, "png");

                string fPath;
                if(FileUtil.validateSomewhere(tryFile, DataFolder + "bitmap/", out fPath))
                {
                    bitMapPointGenerator.bmapName = fPath;
                }
            }
        }
    }

    string svgFullPath {
        get {
            if (!File.Exists(svgFile))
            {
                svgFile = FileUtil.FormatApplicationDataPathIfNE(svgFile, "svg");
            }
            return svgFile;
        }
        set {
            if (FileUtil.isValidFileName(value))
            {
                string tryFileName = FileUtil.AppendSVGExtension(value);

                string fPath;
                if(FileUtil.validateSomewhere(tryFileName, DataFolder, out fPath ))
                {
                    svgFile = fPath;
                }
            } 
        }
    }

    SCBasePenPathGenerator generator;


    public string svgFileNameNoExtension {
        get {
            return Path.GetFileNameWithoutExtension(svgFullPath);
        }
    }

    void Start () {
        StartCoroutine(lateStart());
	}

    private IEnumerator lateStart() {
        yield return new WaitForSeconds(.2f);
        // Redraw();
    }

    public string saveFullPath {
        get { return FileUtil.OutGCodeFileOnDesktopWithFileName(svgFileNameNoExtension); }
    }

    [SerializeField]
    bool ShouldMeshPixelTree;

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


    void Setup()
    {
        finished = false;
        canvasClone = Instantiate(canvas);
        mat.mainTexture = canvasClone;
        machineConfig = FindObjectOfType<MachineConfig>();
        gCodeWriter = FindObjectOfType<GCodeWriter>();
        dbugSettings = FindObjectOfType<DbugSettings>();
        painter = new BMapPainter(canvasClone, machineConfig.paper);
    }

    private void clear()
    {
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
        CreateGenerator();
        updateTimeScaleComment();
        _Main();
    }

    void _Main()
    {
        try
        {
            setupPainter();

            if (progressUpdates)
            {
                StartCoroutine(getProgressiveCrosshatches(generator));
            }
            else
            {
                getCrosshatches(generator);
            }
        } 
        catch(Exception e)
        {
            Debug.Log("exception in main: " + e.ToString());
        }
    }

    void CreateGenerator()
    {
        SVGViewBox viewBox = SCCSSParser.ParseViewBox(svgFullPath);
        CSSLookup lookup = SCCSSParser.Parse(svgFullPath);

        SvgParser.SvgPath plist = getSvgPath(svgFullPath);

        for (var it = plist; it != null; it = it.next)
        {
            lookup.updatePath(it);
        }

        SCSvgFileData _svgFileData = new SCSvgFileData() { isYAxisInverted = yAxisInverted };
        StripeFieldConfig stripeFieldConfig = FindObjectOfType<StripeFieldConfig>();
        StripedPathSet stripedPathSet = new StripedPathSet(plist, stripeFieldConfig, _svgFileData);

        // 
        // Configure generator
        //
        if (generatorType == GeneratorType.Crosshatch)
        {
            var crosshatchGenerator = new SCCrosshatchGenerator(machineConfig, new HatchConfig(), _svgFileData, viewBox, _generatorConfig);
            crosshatchGenerator.UsePathSet(stripedPathSet);
            generator = crosshatchGenerator;
        }
        else if(generatorType == GeneratorType.Triangles)
        {

            var pixelTree = bitMapPointGenerator.getPixelTriTree();
            //if (ShouldMeshPixelTree)
            //    pixelTreeDebugDisplay.GetComponent<MeshFilter>().mesh = pixelTree.getMesh();

            var pointSetGenerator = new SCTriangleCrosshatchGenerator(
                machineConfig,
                _generatorConfig,
                pixelTree,
                bitMapPointGenerator);

            generator = pointSetGenerator;
        }
        else
        {
            SCTSPCrosshatchGenerator tspGenerator;
            if (useBitMapPointGenerator)
            {
                tspGenerator = new SCTSPCrosshatchGenerator(machineConfig, bitMapPointGenerator.inputImageBox, _generatorConfig);
                tspGenerator.SetTSPPointSets(bitMapPointGenerator.getPoints());

                tspGenerator.BaseFileName = Path.GetFileNameWithoutExtension(bitMapPointGenerator.bmapName);
            }
            else
            {
                List<Vector2f> points;
                tspGenerator = new SCTSPCrosshatchGenerator(machineConfig, viewBox, _generatorConfig);
                points = stripedPathSet.AllPathsToPoints();
                tspGenerator.AddPoints(points);
            }

            generator = tspGenerator;
        }

    }

    void setupPainter()
    {
        painter.SetLineWidthWithPenDiamMM((float)machineConfig.toolDiameterMM);
        painter.color = new Color(0f, 1f, .9f);
        painter.DrawBox(machineConfig.paper);
        painter.DrawBox(machineConfig.paper.expandUniform(-3f));
        painter.color = Color.black;
        painter.flipY = !yAxisInverted;
        painter.applyTexture();
    }

 

    void addPenSubscribers(SCPen pen)
    {
        if (dbugSettings.paintFromPenMoves)
        {
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

    void paintFromGCode()
    {
        if (!dbugSettings.paintFromGCode) { return; }
        if(dbugSettings.paintFromGCode && dbugSettings.paintFromPenMoves) {
            Debug.LogWarning("Paint from GCode and Pen Moves both enabled. drawing crosshatches twice. ");
        }
        GCodeCursor gCodeCursor = new GCodeCursor();
        gCodeCursor.subscribe((CursorUpdate cu) =>
        {
            painter.DrawCursorUpdate(cu);
        });

        //TODO: paint from gcode should only draw travel moves when asked to do so

        foreach(string line in gCodeWriter.getLines()) {
            gCodeCursor.moveTo(line);
        }

        painter.applyTexture();
    }

    void beFinished() {
        print("finished");
        finished = true;
    }

    void DebugPenPathToLineMesh(PenDrawingPath penPath)
    {
        GameObject go = new GameObject("penPathMeshDisplay" + UnityEngine.Random.Range(0f, 999999999f));
        go.transform.position = pixelTreeDebugDisplay.transform.position + Vector3.back * 5f;
        MeshRenderer meshRenderer = go.AddComponent<MeshRenderer>();
        MeshFilter filter = go.AddComponent<MeshFilter>();
        meshRenderer.material = pixelTreeDebugDisplay.GetComponent<MeshRenderer>().sharedMaterial; // use pixel tree mesh material
        go.GetComponent<MeshFilter>().mesh = penPath.ToMesh((float)machineConfig.toolDiameterMM, dbugSettings.drawTravelMoves);
    }

    IEnumerator getProgressiveCrosshatches(SCBasePenPathGenerator generator)
    {
        SCPen pen = FindObjectOfType<SCPen>();
        addPenSubscribers(pen);

        int count = 0;
        foreach (PenDrawingPath penPath in generator.generate())
        {
            DebugPenPathToLineMesh(penPath);

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


    void getCrosshatches(SCBasePenPathGenerator generator)
    {
        var penIterator = generator.generate();
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
