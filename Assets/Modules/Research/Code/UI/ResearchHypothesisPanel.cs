using FieldDay;
using FieldDay.Components;
using FieldDay.HID;
using FieldDay.UI;
using TMPro;
using UnityEngine;

namespace SpaceFab.Research {
    /// <summary>
    /// View component for the top hypothesis panel: header, left/right
    /// arrows, observation chips, and the moving current-hypothesis
    /// indicator. Pure view: inspector-assigned references only, plus
    /// thin click dispatchers that route through ResearchUIInputUtility.
    /// The per-frame visual render runs in HypothesisPanelVisualSystem
    /// against Find.Components&lt;ResearchHypothesisPanel&gt;(). Per-page
    /// fulfilled state is shown via the dots' ConfirmedOverlay, not a
    /// separate header checkmark.
    ///
    /// Pagination dots live in ResearchPools.PaginationDotPool — the
    /// panel does not own them, but it owns PaginationDotContainer (the
    /// RectTransform alloced dots get reparented under) and
    /// CurrentHypothesisIndicator (a single transform the visual util
    /// repositions over the active dot each frame). Chip slots are a
    /// fixed pool with hide-unused: max count is bounded by the largest
    /// hypothesis decomposition we expect.
    /// </summary>
    public class ResearchHypothesisPanel : BatchedComponent, IRegistrationCallbacks {
        public TMP_Text HeaderLabel;

        public CursorHint LeftArrow;
        public CursorHint RightArrow;

        // Parent that alloced pagination dots are reparented under on
        // grow. Typically a RectTransform sitting
        // between the arrows so the dots flow into place automatically.
        public RectTransform PaginationDotContainer;

        // Single indicator that moves to the active dot's position
        // each frame. Renders on top of the dots (handled via Canvas
        // sibling order in the prefab).
        public RectTransform CurrentHypothesisIndicator;

        public ResearchObservationChip[] Chips;

        public void OnRegister() {
            if (LeftArrow != null) {
                LeftArrow.onClick.Register(HandleLeftArrow);
            }
            if (RightArrow != null) {
                RightArrow.onClick.Register(HandleRightArrow);
            }
        }

        public void OnDeregister() {
            if (LeftArrow != null) {
                LeftArrow.onClick.Deregister(HandleLeftArrow);
            }
            if (RightArrow != null) {
                RightArrow.onClick.Deregister(HandleRightArrow);
            }
        }

        private void HandleLeftArrow() {
            ResearchUIInputUtility.RequestHypothesisCycle(Find.State<ResearchUIInputState>(), -1);
        }

        private void HandleRightArrow() {
            ResearchUIInputUtility.RequestHypothesisCycle(Find.State<ResearchUIInputState>(), +1);
        }
    }
}
