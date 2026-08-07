using BeauUtil.Debugger;
using FieldDay;
using FieldDay.Scenes;
using FieldDay.SharedState;
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
        // Full-panel root. Opaque and interactive when expanded, faded out and raycast-transparent
        // when collapsed.
        public CanvasGroup ExpandedRoot;

        public TextMeshProUGUI Header;

        // Page text + illustration widgets. Written whenever the active page changes while the
        // panel is expanded.
        public WikiPageContentWidgets PageContentWidgets;

        // Paginator scroll surface. Slid horizontally so off-window thumbs clip against the
        // strip's UI Mask.
        public RectTransform PaginatorContent;

        // Single highlight overlay, reparented onto the selected thumbnail on each refresh. Hidden
        // when no page is selected.
        public RectTransform PageHighlight;

        public Button PrevPage;
        public Button NextPage;

        public Sprite TabActiveSprite;
        public Sprite TabInactiveSprite;
        public Sprite PageThumbActiveSprite;
        public Sprite PageThumbInactiveSprite;

        // Horizontal distance between adjacent thumb slots, in PaginatorContent's local space.
        // Authored to match the prefab layout group's spacing plus cell width.
        public float PageThumbStride;

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
            WikiLayoutUtility.ScrollPaginator(this, 0);
            wikiState.NeedsRebuild = true;
        }
    }

    /// <summary>
    /// Helpers for WikiLayoutState: the steady-state visibility snap, the paginator scroll math,
    /// and the selection highlight's placement.
    ///
    /// Every layout reference these touch is required authoring on the wiki prefab, so a missing
    /// one asserts rather than silently skipping the work.
    /// </summary>
    public static class WikiLayoutUtility {
        private static Vector2 THUMB_HIGHLIGHT_MARGIN = new Vector2(-8, -8);

        // Snap the panel root to the visibility that matches `expanded`.
        public static void ApplyExpandedSteadyState(WikiLayoutState layoutState, bool expanded) {
            SetGroupVisible(layoutState.ExpandedRoot, expanded);
            // SetGroupVisible(layoutState.CollapsedRoot, !expanded);
        }

        // Slide the paginator strip so the (startIndex)th slot sits at the strip's left edge.
        public static void ScrollPaginator(WikiLayoutState layoutState, int startIndex) {
            Assert.NotNullOrDestroyed(layoutState.PaginatorContent, "WikiLayoutState.PaginatorContent not authored");

            Vector2 anchoredPos = layoutState.PaginatorContent.anchoredPosition;
            anchoredPos.x = -startIndex * layoutState.PageThumbStride;
            layoutState.PaginatorContent.anchoredPosition = anchoredPos;
        }

        // Reparent the highlight overlay onto the selected thumbnail and stretch it to that
        // thumb's rect. Parenting rather than copying coordinates means the highlight tracks the
        // thumb as the strip slides and clips against the same UI Mask. SetAsFirstSibling pushes
        // it behind the thumb's own content so the icon still renders on top.
        public static void PositionPageHighlight(WikiLayoutState layoutState, RectTransform targetThumb) {
            Assert.NotNullOrDestroyed(layoutState.PageHighlight, "WikiLayoutState.PageHighlight not authored");

            // No selected thumb is a real state — the active page's thumbnail may be locked or
            // belong to another tab.
            if (targetThumb == null) {
                HidePageHighlight(layoutState);
                return;
            }

            RectTransform highlight = layoutState.PageHighlight;
            if (highlight.parent != targetThumb) {
                highlight.SetParent(targetThumb, false);
                highlight.SetAsFirstSibling();
            }

            // Stretch to fill the thumb's rect exactly.
            highlight.anchorMin = Vector2.zero;
            highlight.anchorMax = Vector2.one;
            highlight.offsetMin = THUMB_HIGHLIGHT_MARGIN;
            highlight.offsetMax = -THUMB_HIGHLIGHT_MARGIN;

            highlight.gameObject.SetActive(true);
        }

        // Hide the highlight overlay, for when no thumbnail in the active tab is selected.
        public static void HidePageHighlight(WikiLayoutState layoutState) {
            layoutState.PageHighlight.gameObject.SetActive(false);
        }

        // Drive a CanvasGroup to fully visible or fully hidden. Pulled out so the alpha +
        // blocksRaycasts + interactable toggle stays in one place as more roots are added.
        private static void SetGroupVisible(CanvasGroup group, bool visible) {
            group.alpha = visible ? 1f : 0f;
            group.blocksRaycasts = visible;
            group.interactable = visible;
        }

    }
}
