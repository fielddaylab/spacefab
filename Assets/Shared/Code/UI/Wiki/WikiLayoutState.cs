using System.Collections.ObjectModel;
using BeauRoutine;
using BeauUtil.Debugger;
using FieldDay;
using FieldDay.Scenes;
using FieldDay.SharedState;
using FieldDay.UI;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace SpaceFab.UI {
    /// <summary>
    /// Scene-authored layout references for the shared wiki UI: the panel root, the paginator
    /// strip and its scroll geometry, the selection highlight, and the widget set page content
    /// binds into.
    ///
    /// Authored once on the wiki prefab root alongside WikiContent and WikiPools, and written by
    /// WikiVisualsUtility when wiki state changes rather than on a per-frame poll.
    /// </summary>
    public class WikiLayoutState : SharedStateComponent, IRegistrationCallbacks, ISceneLateInitialize {
        public Canvas RootCanvas;
        public CanvasGroup ExpandedRoot;
        public CanvasInputLayer InputLayer;

        public TextMeshProUGUI Header;

        public WikiTabinator Tabinator;
        public WikiPaginator Paginator;
        public WikiPageLayout PageLayout;

        public WikiContent WikiContent;

        public void OnRegister()
        {
        }

        public void OnDeregister() {
        }

        // Snaps the authored prefab to whatever steady state WikiState starts in, then queues the
        // first strip rebuild.
        public void LateInitialize()
        {
            Find.State(out WikiState wikiState);
            WikiLayoutUtility.ApplyExpandedSteadyState(this, wikiState.Expanded);
            wikiState.NeedsRebuild = true;
        }
    }

    /// <summary>
    /// Helpers for WikiLayoutState: the steady-state visibility snap, the tab strip's vertical
    /// arrangement, the paginator scroll math, and the selection highlight's placement.
    ///
    /// Every layout reference these touch is required authoring on the wiki prefab, so a missing
    /// one asserts rather than silently skipping the work.
    /// </summary>
    public static partial class WikiLayoutUtility {
        // Snap the panel root to the visibility that matches `expanded`.
        public static void ApplyExpandedSteadyState(WikiLayoutState layoutState, bool expanded) {
            layoutState.InputLayer.SetInputOverride(expanded ? null : false);
            layoutState.RootCanvas.enabled = expanded;
        }
    }
}
