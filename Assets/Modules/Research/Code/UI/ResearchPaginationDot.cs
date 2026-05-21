using UnityEngine;
using UnityEngine.UI;

namespace SpaceFab.Research {
    /// <summary>
    /// One dot in the hypothesis paginator.
    ///   - ConfirmedOverlay sits on top of the base and is toggled by
    ///     HypothesisPanelVisualUtility when the hypothesis at this
    ///     dot's page is fulfilled by any known material.
    /// The "currently selected" highlight is a separate indicator owned
    /// by ResearchHypothesisPanel (CurrentHypothesisIndicator) that
    /// moves to the active dot's position each frame; it is not
    /// authored per-dot.
    /// </summary>
    public class ResearchPaginationDot : MonoBehaviour {
        public Image ConfirmedOverlay;
    }
}
