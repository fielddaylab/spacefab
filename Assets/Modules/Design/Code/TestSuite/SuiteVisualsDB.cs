using FieldDay.Assets;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace SpaceFab.Design.Visuals
{
    /// <summary>
    /// Visual state of a SuiteRunRowButton's icon. Drives which sprite is shown:
    /// Play (idle / inactive), Pause (this row is propagating), Resume (this row is paused).
    /// </summary>
    public enum SuiteRunButtonState
    {
        Play,
        Pause,
        Resume,
    }

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
        public Sprite SuiteArrow;

        [Header("Run Button Icons")]
        public Sprite PlayIcon;
        public Sprite PauseIcon;
        public Sprite ResumeIcon;
    }

    public static class SuiteVisualsDBUtility
    {
        public static Sprite LookupSuiteColSprite(SuiteVisualsDB suiteDB, FlowState state, bool isOutput = false, bool isArrow = false)
        {
            if (isOutput) { return suiteDB.SuiteFlowOutput; }
            if (isArrow) { return suiteDB.SuiteArrow; }

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

        // Maps a SuiteRunRowButton state to its icon sprite from the visuals DB.
        public static Sprite LookupRunButtonSprite(SuiteVisualsDB suiteDB, SuiteRunButtonState state)
        {
            switch (state)
            {
                case SuiteRunButtonState.Play:
                    return suiteDB.PlayIcon;
                case SuiteRunButtonState.Pause:
                    return suiteDB.PauseIcon;
                case SuiteRunButtonState.Resume:
                    return suiteDB.ResumeIcon;
                default:
                    return null;
            }
        }
    }
}