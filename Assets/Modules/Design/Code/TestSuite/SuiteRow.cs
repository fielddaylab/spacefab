using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace SpaceFab.Design
{
    public class SuiteRow : MonoBehaviour
    {
        public HorizontalLayoutGroup HorizontalLayout;
        public Image RowBGBar;
        [HideInInspector] public SuiteCol[] Cols;
        [HideInInspector] public SuiteCol ArrowCol;

        // Parallel to Cols. Output columns hold the VerdictVisualizer attached to their prefab;
        // non-output columns hold null. Cached at BuildTable time so VerdictVisualizerRefreshSystem
        // reads visualizers without per-frame GetComponent calls.
        [HideInInspector] public VerdictVisualizer[] Verdicts;
    }
}
