﻿using UnityEngine;
using UnityEngine.UI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using g3;

namespace SCGenerator
{
    public enum VariationType
    {
        RotatePath, RotateStripeDirection
    }

    public class DbugSettings : MonoBehaviour
    {
        [Header("--Display settings--")]
        public bool paintFromGCode;
        public bool paintFromPenMoves;
        public bool drawTravelMoves;
        public bool pathOutlines;
        public bool pathDirIndicators;
        [Header("Limited to a small-ish number of lines")]
        public bool showWithLineRenderers;
        [Header("--Variations/Test settings--")]
        public bool makeVariations;
        public VariationType variationType;
        public int numVariations = 12;
        public bool reverseClockwiseOrderCopies;
        public bool dontRotateBackFinalPenPaths;
        [Range(1, 24)]
        public int variationColumns = 4;
        public bool dbugDrawing;
        public float dbugStripeInterval = 22f;
        [SerializeField]
        private Vector2 _baseStripeDirection = Vector2.right;
        public Vector2f baseStripeDirection { get { return _baseStripeDirection.normalized; } }
        public float baseStripeAngleRadians = 0f;

        public bool deriveLineWidthFromPenWidth = false;
        [SerializeField, Range(1f, 20f), Header("not used if deriving from pen width")]
        public float displayLineWidth = 3f;
        [SerializeField]
        public int hatchCount = 2;
    }
}
