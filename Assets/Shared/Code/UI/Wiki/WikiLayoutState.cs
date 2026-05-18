using FieldDay;
using FieldDay.SharedState;
using FieldDay.Systems;
using FieldDay.Scenes;
using UnityEngine;
using UnityEngine.UI;

namespace SpaceFab.UI {
    /// <summary>
    /// Scene-authored layout references for the shared wiki UI. Holds the two sibling
    /// CanvasGroups whose alpha distinguishes expanded vs. collapsed steady state, the
    /// paginator strip's content RectTransform and per-icon stride used to slide the strip
    /// under its mask, and a pointer to the WikiPageContentWidgets that the visuals system
    /// pushes page fields into.
    ///
    /// Authored once on the wiki prefab root alongside WikiContent and WikiPools. Mutated by
    /// the visuals system each frame; the transition routines (WikiUtility.ExpandRoutine /
    /// CollapseRoutine) tween the CanvasGroup alphas while Transitioning is true.
    /// </summary>
    public class WikiLayoutState : SharedStateComponent, IRegistrationCallbacks, ISceneLateInitialize {
        // Full-panel root. Fully visible (alpha == 1, blocksRaycasts == true) in the expanded
        // steady state; faded out and non-interactive when collapsed.
        public CanvasGroup ExpandedRoot;

        // Small icon-button root. Mirror of ExpandedRoot: visible when collapsed, faded out
        // when expanded.
        public CanvasGroup CollapsedRoot;

        // Page-text + illustration widgets the visuals system writes into each frame while
        // the panel is expanded.
        public WikiPageContentWidgets PageContentWidgets;

        // Paginator scroll surface. Slid horizontally by anchoredPosition.x = -StartIndex *
        // PageThumbStride so off-window icons clip against the strip's UI Mask.
        public RectTransform PaginatorContent;

        public Button PrevPage;
        public Button NextPage;

        // Horizontal distance (in PaginatorContent's local space) between adjacent page-thumb
        // slots. Authored to match the prefab's thumb layout group spacing + cell width.
        public float PageThumbStride;

        public void OnRegister()
        {
        }

        public void OnDeregister() {
        }

        public void LateInitialize()
        {
            // Enforce the initial steady state based on the default WikiState.Expanded value.
            Find.State(out WikiState wikiState);
            WikiLayoutUtility.ApplyExpandedSteadyState(this, wikiState.Expanded);
            Debug.Log($"ScrollPaginator called with start={wikiState.PageWindowStartIndex}");
            WikiLayoutUtility.ScrollPaginator(this, 0);
            wikiState.NeedsRebuild = true;
        }
    }

    /// <summary>
    /// Helpers for WikiLayoutState. Holds the steady-state CanvasGroup assertion used by
    /// WikiVisualsUpdateSystem when no transition is in flight, and the paginator-strip
    /// scroll math.
    /// </summary>
    public static class WikiLayoutUtility {
        // Snap the two roots to the steady-state alpha that matches `expanded`. Called by the
        // visuals system while WikiState.Transitioning is false; the transition routines own
        // the in-between alpha values.
        public static void ApplyExpandedSteadyState(WikiLayoutState layoutState, bool expanded) {
            SetGroupVisible(layoutState.ExpandedRoot, expanded);
            SetGroupVisible(layoutState.CollapsedRoot, !expanded);
        }

        // Slide the paginator strip so the (startIndex)th slot sits at the strip's left edge.
        // No-op if PaginatorContent or PageThumbStride aren't authored.
        public static void ScrollPaginator(WikiLayoutState layoutState, int startIndex) {
            if (layoutState.PaginatorContent == null) { return; }

            Vector2 anchoredPos = layoutState.PaginatorContent.anchoredPosition;
            anchoredPos.x = -startIndex * layoutState.PageThumbStride;
            layoutState.PaginatorContent.anchoredPosition = anchoredPos;
        }

        // Drive a CanvasGroup to a fully-visible or fully-hidden steady state. Used by
        // ApplyExpandedSteadyState; pulled out so the alpha + blocksRaycasts + interactable
        // toggle stays consistent across both roots.
        private static void SetGroupVisible(CanvasGroup group, bool visible) {
            if (group == null) { return; }
            group.alpha = visible ? 1f : 0f;
            group.blocksRaycasts = visible;
            group.interactable = visible;
        }
    }
}
