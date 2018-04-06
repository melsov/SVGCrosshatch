using UnityEngine;
using UnityEngine.UI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using SCGenerator;
using SCParse;

public class UIMachineConfig : MonoBehaviour
{
    MachineConfig config;

    [SerializeField]
    InputField paperWidth;
    [SerializeField]
    InputField paperHeight;

    [SerializeField]
    InputField penWidth;

    [SerializeField]
    InputField drawZ;
    [SerializeField]
    InputField travelZ;

    private void Start() {
        config = FindObjectOfType<MachineConfig>();
        readValues();

    }

    private void readValues() {
        paperWidth.text = config.paper.size.x.ToString();
        paperHeight.text = config.paper.size.y.ToString();
        penWidth.text = config.toolDiameterMM.ToString();
        drawZ.text = config.maxPenetrationDepthMM.ToString();
        travelZ.text = config.travelHeightMM.ToString();
    }

    public void onUpdatePressed() {
        updateValues();
    }

    float parse(InputField inf) {
        return float.Parse(inf.text);
    }

    private void updateValues() {
        try {
            config.paper = new Box2() { min = config.paper.min, max = config.paper.min + new Vector2(parse(paperWidth), parse(paperHeight)) };
            config.toolDiameterMM = parse(penWidth);
            config.maxPenetrationDepthMM = parse(drawZ);
            config.travelHeightMM = parse(travelZ);
        } catch(Exception e) {
            Debug.Log(e.ToString());
        }

        readValues();
    }
}
