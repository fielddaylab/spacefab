using FieldDay.Assets;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace SpaceFab.Design.Visuals
{
    [CreateAssetMenu(menuName = "SpaceFab/Design/SuiteVisuals DB")]
    public class SuiteVisualsDB : GlobalAsset
    {
        [Header("Suite Prefabs")]
        public GameObject RowPrefab;
        public GameObject HeaderPrefab;
        public GameObject InputColPrefab;
        public GameObject OutputColPrefab;
        public GameObject ArrowColPrefab;

        [Header("SuiteCols")]
        public Sprite SuiteFlowHi;
        public Sprite SuiteFlowLo;
        public Sprite SuiteFlowEmpty;
        public Sprite SuiteFlowUnstable;
        public Sprite SuiteFlowOutput;
    }

    public static class SuiteVisualsDBUtility
    {
        public static Sprite LookupSuiteColSprite(SuiteVisualsDB suiteDB, bool isOutput, FlowState state)
        {
            if (isOutput)
            {
                return suiteDB.SuiteFlowOutput;
            }

            switch (state)
            {
                case FlowState.Empty:
                    return suiteDB.SuiteFlowEmpty;
                case FlowState.Hi:
                    return suiteDB.SuiteFlowHi;
                case FlowState.Lo:
                    return suiteDB.SuiteFlowLo;
                case FlowState.Unstable:
                    return suiteDB.SuiteFlowUnstable;
                default:
                    return null;
            }
        }
    }
}