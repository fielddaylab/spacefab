using FieldDay;
using FieldDay.Components;
using FieldDay.HID;
using FieldDay.UI;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace SpaceFab.Research {
    /// <summary>
    /// View component for the top hypothesis panel: header, pagination
    /// dots, left/right arrows, observation chips, fulfilled checkmark,
    /// and the submit button. Pure view: inspector-assigned references
    /// only, plus thin click dispatchers that route through
    /// ResearchUIInputUtility. The per-frame visual render runs in
    /// HypothesisPanelVisualSystem against Find.Components&lt;ResearchHypothesisPanel&gt;().
    /// </summary>
    public class ResearchHypothesisPanel : BatchedComponent, IRegistrationCallbacks {
        public TMP_Text HeaderLabel;

        public Image[] PaginationDots;
        public Color DotActiveColor = Color.white;
        public Color DotInactiveColor = new Color(1f, 1f, 1f, 0.3f);

        public CursorHint LeftArrow;
        public CursorHint RightArrow;

        public ResearchObservationChip[] Chips;
        public GameObject FulfilledCheckmark;
        public CursorHint SubmitButton;

        public void OnRegister() {
            if (LeftArrow != null) {
                LeftArrow.onClick.Register(HandleLeftArrow);
            }
            if (RightArrow != null) {
                RightArrow.onClick.Register(HandleRightArrow);
            }
            if (SubmitButton != null) {
                SubmitButton.onClick.Register(HandleSubmit);
            }
        }

        public void OnDeregister() {
            if (LeftArrow != null) {
                LeftArrow.onClick.Deregister(HandleLeftArrow);
            }
            if (RightArrow != null) {
                RightArrow.onClick.Deregister(HandleRightArrow);
            }
            if (SubmitButton != null) {
                SubmitButton.onClick.Deregister(HandleSubmit);
            }
        }

        private void HandleLeftArrow() {
            ResearchUIInputUtility.RequestHypothesisCycle(Find.State<ResearchUIInputState>(), -1);
        }

        private void HandleRightArrow() {
            ResearchUIInputUtility.RequestHypothesisCycle(Find.State<ResearchUIInputState>(), +1);
        }

        private void HandleSubmit() {
            ResearchUIInputUtility.RequestSubmit(Find.State<ResearchUIInputState>());
        }
    }
}
