using FieldDay.Components;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace SpaceFab.Fabrication.Sequence
{
    /// <summary>
    /// View component for one of the five recap cards shown when a sequence step completes.
    /// Data-only; CompletionRecapUtility populates sprites/text and animates positions/alpha in
    /// response to a recap-request flag. Cards are pre-authored on the Sequence Completion Recap
    /// prefab (no instantiate/destroy).
    /// </summary>
    public class CompletionRecapCard : BatchedComponent
    {
        public CanvasGroup Group;

        // RectTransform used for the slide-in animation. Same transform as the GameObject; exposed
        // here so the utility doesn't repeatedly cast the component.
        public RectTransform Root;

        // Sourced from WaferStepUILookup via SequenceStepEntry.ConvertToA for completed steps, or
        // CompletionRecapState.UnknownStepSprite for future steps.
        public Image Wafer;

        // The card frame that "stamps" in around the wafer on the center card. Disabled by default
        // on the prefab; the wafer-appear stage enables it once the wafer fade-in finishes.
        public Image CardBackground;

        // Step label (e.g. "ADD PHOTORESIST"). Sourced from SequenceStepEntry.InstructionLabel.
        public TMP_Text Label;
    }
}
