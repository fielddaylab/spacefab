using BeauUtil.Debugger;
using FieldDay.Assets;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace SpaceFab.Design.Visuals
{
    [CreateAssetMenu(menuName = "SpaceFab/Design/Suite Visual Config")]
    public class SuiteVisualConfig : GlobalAsset
    {
        [Header("Sprites")]
        public Sprite InputLow;
        public Sprite InputHigh;
        public Sprite OutputEmpty;
        public Sprite OutputLow;
        public Sprite OutputHigh;
        public Sprite RowPending;
        public Sprite RowSuccess;
        public Sprite RowFailure;

        [Header("Colors")]
        public Color32 OutputEmptyTextColor;
        public Color32 OutputFilledTextColor;
        public Color32 RowPendingLineLeftColor;
        public Color32 RowPendingLineRightColor;
        public Color32 RowSuccessLineColor;
        public Color32 RowFailureLineColor;

        [Header("Text")]
        public string LowLabel;
        public string HighLabel;
        public string EmptyLabel;
        public string UnstableLabel;
        public string InputALabel;
        public string InputBLabel;
        public string OutputXLabel;
        public string OutputYLabel;
    }

    /// <summary>
    /// Per-output-cell verdict UI state. Hidden = no verdict to display (no test resolved yet,
    /// or display cleared between runs). Correct/Incorrect = the most recent resolve compared
    /// the actual flow to the expected flow for this output and got that result.
    /// </summary>
    public enum CellVerdict : byte {
        Hidden,
        InProgress,
        Correct,
        Incorrect,
    }

    public struct RowVerdictSet {
        public CellVerdict OutputX;
        public CellVerdict OutputY;
    }

    public static partial class SuiteVisualsDBUtility
    {
        static public string GetLabelForFlowState(SuiteVisualConfig config, FlowState flowState) {
            switch (flowState) {
                case FlowState.Empty: {
                    return config.EmptyLabel;
                }
                case FlowState.Hi: {
                    return config.HighLabel;
                }
                case FlowState.Lo: {
                    return config.LowLabel;
                }
                case FlowState.Unstable: {
                    return config.UnstableLabel;
                }
                default: {
                    Assert.Fail("Unknown flow state '{0}'", flowState);
                    return string.Empty;
                }
            }
        }

        static public Sprite GetInputSlotIconForFlowState(SuiteVisualConfig config, FlowState flowState) {
            switch (flowState) {
                case FlowState.Hi: {
                    return config.InputHigh;
                }
                case FlowState.Lo: {
                    return config.InputLow;
                }
                default: {
                    Assert.Fail("No input icon for flow state '{0}'", flowState);
                    return null;
                }
            }
        }

        static public Sprite GetOutputSlotIconForFlowState(SuiteVisualConfig config, FlowState flowState) {
            switch (flowState) {
                case FlowState.Hi: {
                    return config.OutputHigh;
                }
                case FlowState.Lo: {
                    return config.OutputLow;
                }
                case FlowState.Empty: {
                    return config.OutputEmpty;
                }
                default: {
                    Assert.Fail("No input icon for flow state '{0}'", flowState);
                    return null;
                }
            }
        }
    }
}