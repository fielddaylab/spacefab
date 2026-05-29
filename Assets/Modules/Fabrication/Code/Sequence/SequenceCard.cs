using FieldDay.Components;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace SpaceFab.Fabrication.Sequence
{
    /// <summary>
    /// View component for a single on-screen sequence step. Data-only — its sprite, text, and
    /// CanvasGroup fields are populated by SequenceVisualsUtility in response to sequence
    /// reset/advance/completion signals. Two of these are pre-authored on
    /// Sequence Panel Group.prefab and pooled by SequenceVisualsState (no instantiate/destroy).
    /// </summary>
    public class SequenceCard : BatchedComponent
    {
        public CanvasGroup Group;

        // "Convert from" wafer image. Sourced from WaferStepUILookup via SequenceStepEntry.ConvertFrom.
        public Image WaferState1;

        // "Convert to" wafer image (base). Sourced from WaferStepUILookup via ConvertToA.
        public Image WaferState2Base;

        // Optional overlay layered on top of WaferState2Base. Sourced via ConvertToB; the image
        // is disabled when no ConvertToB entry is authored on the step.
        public Image WaferState2Overlay;

        // Image for the station the player must visit. Authored per-step on SequenceStepEntry.
        public Image StationIcon;

        // Two text labels split out from the old single StepText field.
        public TMP_Text StationLabelText;
        public TMP_Text InstructionLabelText;
    }
}
