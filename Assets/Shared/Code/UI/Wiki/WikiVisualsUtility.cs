using System.Collections;
using System.Collections.ObjectModel;
using BeauRoutine;
using BeauUtil.Debugger;
using FieldDay;
using FieldDay.UI;
using SpaceFab.Materials;
using UnityEngine;
using UnityEngine.UI;

namespace SpaceFab.UI {
    /// <summary>
    /// Drives the wiki's presentation from WikiState + WikiContent: panel visibility, the
    /// active-tab highlight, page content binding, paginator thumbnails, and arrow enable-state.
    ///
    /// Repaints are dirty-tracked, not wholesale. Mutators call Invalidate with the domains they
    /// touched (see WikiVisualDirty); Refresh applies only those, then clears them. Stepping one
    /// page therefore rewrites the page bind and the paginator, and leaves the tab strip alone.
    ///
    /// Nothing recomputes per-frame — WikiRefreshSystem drains the mask on LateUpdate and early-outs
    /// when it's empty. A panel that looks stale means some mutation forgot to invalidate its
    /// domain, and the fix belongs at that mutation rather than here.
    ///
    /// The selected tab's pop-out is the one exception: it runs as a tween on
    /// WikiState.TabPopRoutine, which owns the offset and width of the two tabs it's animating for as
    /// long as it's in flight. Repaints that land mid-pop leave those two alone.
    /// </summary>
    public static class WikiVisualsUtility {
        #region Entry Points

        // Marks presentation domains stale. Called from the mutation that invalidated them, so the
        // dependency is recorded next to the write instead of at whatever call site refreshes later.
        public static void Invalidate(WikiState wikiState, WikiVisualDirty domains) {
            wikiState.VisualsDirty |= domains;
        }

        // Applies every dirty domain, then clears the mask. Systems pass the states they hold.
        // researchContext is absent outside the Research scene, where observation and property
        // pages render plain and inert.
        public static void Refresh(WikiState wikiState, WikiLayoutState layout, WikiContent content, WikiPools pools, WikiChipPools chipPools, PlayerProgressState progressState, in WikiResearchContext researchContext) {
            WikiVisualDirty dirty = wikiState.VisualsDirty;
            if (dirty == WikiVisualDirty.None) { return; }

            // Visibility is the only domain that means anything while the panel is collapsed.
            if ((dirty & WikiVisualDirty.Visibility) != 0) {
                WikiLayoutUtility.ApplyExpandedSteadyState(layout, wikiState.Expanded);
                wikiState.VisualsDirty &= ~WikiVisualDirty.Visibility;
            }

            // Collapsed is a real state, not missing setup. Leave the content domains dirty rather
            // than clearing them against a panel nobody can see — they apply on expand.
            if (!wikiState.Expanded) { return; }

            // Resolved once, and only when a domain that needs it is dirty. Doubles as an invariant
            // check on ActiveTabIndex.
            WikiTabData activeTab = null;
            if ((dirty & (WikiVisualDirty.PageContent | WikiVisualDirty.Paginator)) != 0) {
                activeTab = ResolveActiveTab(wikiState, content);
            }

            if ((dirty & WikiVisualDirty.TabStrip) != 0) {
                RefreshTabStrip(pools, content, layout, wikiState);
            }

            if ((dirty & WikiVisualDirty.PageContent) != 0) {
                RefreshPageContent(layout, chipPools, activeTab, progressState, wikiState.ActivePageIndex, researchContext);
            }

            if ((dirty & WikiVisualDirty.Paginator) != 0) {
                RefreshPaginator(layout, pools, content, activeTab, wikiState, progressState);
            }

            wikiState.VisualsDirty = WikiVisualDirty.None;
        }

        #endregion // Entry Points

        #region Tab Strip

        // Applies each pooled tab button's icon and selected/unselected sprite, then reconciles the
        // pop-out. Locked tabs were already hidden by WikiAvailabilityUtility, so this only styles
        // what's still active.
        //
        // Offset and width aren't written here — SyncTabPop owns both, since the pop animates them.
        private static void RefreshTabStrip(WikiPools pools, WikiContent content, WikiLayoutState layout, WikiState wikiState) {
            var tabButtons = pools.TabButtonPool.ActiveObjects;
            WikiLayoutUtility.LayoutTabStrip(layout, tabButtons);

            WikiButton selectedTab = null;

            for (int i = 0; i < tabButtons.Count; i++) {
                WikiButton tab = tabButtons[i];
                Assert.True(tab.TabIndex >= 0 && tab.TabIndex < content.Tabs.Length,
                    "Wiki tab button has out-of-range TabIndex {0}", tab.TabIndex);

                Image icon = tab.GetComponent<WikiTab>().TabIcon;
                icon.sprite = content.Tabs[tab.TabIndex].Icon;
                icon.color = Color.white;

                bool selected = tab.TabIndex == wikiState.ActiveTabIndex;
                tab.DynamicButton.image.sprite = selected ? layout.TabActiveSprite : layout.TabInactiveSprite;
                tab.DynamicButton.interactable = true;

                if (selected) { selectedTab = tab; }
            }

            SyncTabPop(wikiState, layout, tabButtons, selectedTab);
        }

        // Brings the pop in line with the selection: selected tab popped out, every other tab resting.
        // WikiState.PoppedTabIndex is what the selection is compared against, and names the tab to
        // ease back in when they disagree.
        private static void SyncTabPop(WikiState wikiState, WikiLayoutState layout, ReadOnlyCollection<WikiButton> tabButtons, WikiButton selectedTab) {
            if (wikiState.PoppedTabIndex == wikiState.ActiveTabIndex) {
                // A pop in flight owns its two tabs' offsets and widths until it ends; placing them
                // here would cut the transition short. Nothing else needs placing, since the only
                // thing that changes the button count is LoadTabs, which stops the pop.
                if (wikiState.TabPopRoutine) { return; }

                for (int i = 0; i < tabButtons.Count; i++) {
                    ApplyTabPop(layout, tabButtons[i], tabButtons[i] == selectedTab ? 1f : 0f);
                }
                return;
            }

            // Place every tab against the selection the pop is leaving, so both tweens start from a
            // known offset. This is also what puts a freshly pooled instance at the strip's width
            // rather than the prefab's, and what returns a tab stranded by an interrupted pop.
            WikiButton popInTab = null;
            for (int i = 0; i < tabButtons.Count; i++) {
                WikiButton tab = tabButtons[i];
                bool outgoing = tab.TabIndex == wikiState.PoppedTabIndex;

                ApplyTabPop(layout, tab, outgoing ? 1f : 0f);
                if (outgoing) { popInTab = tab; }
            }

            wikiState.PoppedTabIndex = wikiState.ActiveTabIndex;
            wikiState.TabPopRoutine.Replace(layout, PopTabsRoutine(layout, selectedTab, popInTab));
        }

        // Slides the newly-selected tab out of the panel while easing the one it replaced back in.
        // Runs on WikiState.TabPopRoutine, hosted on the wiki prefab's own WikiLayoutState so it dies
        // with the scene rather than outliving the buttons it writes to. Start it through SyncTabPop,
        // never directly.
        //
        // Either end can be absent: the first pop of a tab set has no tab to send back in, and a
        // selection with no button of its own has none to send out until the pending rebuild lands.
        private static IEnumerator PopTabsRoutine(WikiLayoutState layout, WikiButton popOutTab, WikiButton popInTab) {
            Tween popOut = popOutTab != null
                ? Tween.ZeroToOne((pop) => ApplyTabPop(layout, popOutTab, pop), layout.TabPopOutTween)
                : null;
            Tween popIn = popInTab != null
                ? Tween.OneToZero((pop) => ApplyTabPop(layout, popInTab, pop), layout.TabPopInTween)
                : null;

            if (popOut == null) {
                yield return popIn;
            } else if (popIn == null) {
                yield return popOut;
            } else {
                yield return Routine.Combine(popOut, popIn);
            }
        }

        // Places one tab along the pop, where 0 is resting and 1 is fully popped out. Sole writer of
        // the two properties the pop moves, so they can't drift apart: both come off the same
        // parameter, and both are unclamped since the pop-out curve overshoots past 1.
        //
        // The vertical slot LayoutTabStrip assigned is preserved — only the horizontal offset moves.
        private static void ApplyTabPop(WikiLayoutState layout, WikiButton tab, float pop) {
            RectTransform rect = (RectTransform) tab.transform;

            rect.anchoredPosition = new Vector2(Mathf.LerpUnclamped(0f, -layout.TabPopOutDistance, pop), rect.anchoredPosition.y);
            rect.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, Mathf.LerpUnclamped(layout.TabWidth, layout.TabPopWidth, pop));
        }

        #endregion // Tab Strip

        #region Page Content

        // Pushes the header title and the active page's fields into the authored widget set.
        // Default pages show the body wrapper; the other three kinds show their own group instead,
        // and material pages additionally source their illustration from the material asset.
        //
        // progressState is the unlock set a material page's characteristics column reads to pick
        // between the basic and full halves of a property pair.
        private static void RefreshPageContent(WikiLayoutState layout, WikiChipPools chipPools, WikiTabData activeTab, PlayerProgressState progressState, int activePageIndex, in WikiResearchContext researchContext) {
            // The header shows the active tab's title, so it turns over with the page bind rather
            // than with the tab strip's icons.
            layout.Header.text = activeTab.Title;

            WikiPageContentWidgets widgets = layout.PageContentWidgets;
            WikiPageData activePage = ResolveActivePage(activeTab, activePageIndex);
            bool materialPage = ResolvePageKind(widgets, activePage, out bool observationPage, out bool propertyPage);
            bool defaultPage = !materialPage && !observationPage && !propertyPage;

            // Title always renders.
            widgets.TitleText.text = activePage.Title ?? " ";

            // Body wrapper visible only on default pages. Observation and property pages carry
            // their own body widget inside their group.
            widgets.DefaultGroup.SetActive(defaultPage);
            if (defaultPage) {
                widgets.BodyText.text = activePage.Body ?? " ";
            }

            widgets.PlanetDetailsContainer.SetActive(activePage.isPlanet);

            // Chips are filled before the group is shown, so navigation never renders an empty
            // column. That used to be a cross-system phase-ordering convention; keeping the two
            // statements adjacent makes it structural. Each kind frees the other two kinds' chips
            // so none stay parked under a hidden container with live click handlers.
            if (materialPage) {
                WikiCharacteristicsLoadUtility.LoadFor(widgets, chipPools, progressState, activePage.MaterialId);
            } else {
                WikiCharacteristicsLoadUtility.FreeAllCharacteristicChips(chipPools);
            }

            if (observationPage) {
                WikiObservationLoadUtility.LoadFor(widgets, chipPools, activePage, researchContext);
            } else {
                WikiObservationLoadUtility.FreeAllObservationChips(chipPools);
            }

            if (propertyPage) {
                WikiPropertyLoadUtility.LoadFor(widgets, chipPools, activePage, researchContext);
            } else {
                WikiPropertyLoadUtility.FreeAllPropertyChips(widgets, chipPools);
            }

            widgets.MaterialCharacteristicsGroup.SetActive(materialPage);
            if (widgets.ObservationGroup != null) {
                widgets.ObservationGroup.SetActive(observationPage);
            }
            if (widgets.PropertyGroup != null) {
                widgets.PropertyGroup.SetActive(propertyPage);
                if (propertyPage)
                {
                    // clear title. Chip stands in for it.
                    widgets.TitleText.SetText(string.Empty);
                }
            }

            // Default pages cycle their authored frame sequence; material pages pull the gem sprite
            // off the referenced material asset, which is a single still frame. Either way a page
            // with no illustration is authored, not broken — the bind hides the slot for us.
            SpriteCycler illustration = widgets.Illustration;
            Assert.NotNullOrDestroyed(illustration, "WikiPageContentWidgets.Illustration not authored");

            if (materialPage) {
                MaterialAsset material = Find.NamedAsset<MaterialAsset>(activePage.MaterialId);
                Assert.NotNullOrDestroyed(material, "Wiki page '{0}' references unknown material '{1}'", activePage.name, activePage.MaterialId);
                SpriteCyclerUtility.SetSingleFrame(illustration, material.GemSprite);
            } else {
                SpriteCyclerUtility.SetFrames(illustration, activePage.IllustrationFrames, activePage.IllustrationFPS);
            }

            illustration.Target.preserveAspect = true;
        }

        // Resolves which kind the page renders as. Kinds are mutually exclusive by authoring, and
        // precedence (material > observation > property) only matters for a page that mistakenly
        // sets more than one discriminator — a content error, so it warns.
        //
        // An observation or property page whose widget group hasn't been authored yet falls back
        // to default rendering, so pages authored ahead of the prefab work don't break the wiki.
        // TODO: drop the fallback and assert on the groups once Wiki.prefab authors them.
        private static bool ResolvePageKind(WikiPageContentWidgets widgets, WikiPageData page, out bool observationPage, out bool propertyPage) {
            bool material = page.IsMaterialPage;
            observationPage = !material && page.IsObservationPage;
            propertyPage = !material && !observationPage && page.IsPropertyPage;

            int authoredKinds = (page.IsMaterialPage ? 1 : 0) + (page.IsObservationPage ? 1 : 0) + (page.IsPropertyPage ? 1 : 0);
            if (authoredKinds > 1) {
                Log.Warn("[WikiVisualsUtility] Wiki page '{0}' authors more than one page kind; rendering as material > observation > property.", page.name);
            }

            if (observationPage && widgets.ObservationGroup == null) {
                Log.Warn("[WikiVisualsUtility] Wiki page '{0}' is an observation page but WikiPageContentWidgets.ObservationGroup is not authored; rendering as a default page.", page.name);
                observationPage = false;
            }
            if (propertyPage && widgets.PropertyGroup == null) {
                Log.Warn("[WikiVisualsUtility] Wiki page '{0}' is a property page but WikiPageContentWidgets.PropertyGroup is not authored; rendering as a default page.", page.name);
                propertyPage = false;
            }

            return material;
        }

        #endregion // Page Content

        #region Paginator

        // The paginator as one unit: scroll offset, thumbnails, the highlight overlay placed over
        // the selected thumb, and the arrow enable-state. These share their inputs, and the
        // highlight needs a RectTransform only the thumbnail pass can produce, so they repaint
        // together.
        private static void RefreshPaginator(WikiLayoutState layout, WikiPools pools, WikiContent content, WikiTabData activeTab, WikiState wikiState, PlayerProgressState progressState) {
            // Slides the strip under its UI Mask so out-of-window thumbnails clip away.
            WikiLayoutUtility.ScrollPaginator(layout, wikiState.PageWindowStartIndex);

            RectTransform selectedThumbRect = RefreshPaginatorStrip(pools, activeTab, content, wikiState, progressState);

            // The highlight is positioned from the thumb's live rect, and the pass above changed
            // which thumbs the strip's layout group has to place. Settle that now — the group
            // would otherwise not run until end of frame, leaving the overlay a frame behind.
            LayoutRebuilder.ForceRebuildLayoutImmediate(layout.PaginatorContent);
            WikiLayoutUtility.PositionPageHighlight(layout, selectedThumbRect);

            RefreshPaginatorArrows(layout, wikiState, content, progressState);
        }

        // Styles the pooled thumbnails for the active tab and returns the selected, in-window
        // thumb's RectTransform so the caller can place the highlight overlay over it, or null if
        // no thumb in the active tab is both selected and visible.
        //
        // Off-window thumbs stay active — the strip's UI Mask clips them once ScrollPaginator has
        // slid the content. Only wrong-tab and locked thumbs are deactivated.
        private static RectTransform RefreshPaginatorStrip(WikiPools pools, WikiTabData activeTab, WikiContent content, WikiState wikiState, PlayerProgressState progressState) {
            RectTransform selectedThumbRect = null;
            int pageWindowSize = Mathf.Max(1, content.PageWindowSize);

            var thumbButtons = pools.PageThumbPool.ActiveObjects;
            for (int i = 0; i < thumbButtons.Count; i++) {
                WikiButton thumb = thumbButtons[i];
                Assert.NotNullOrDestroyed(thumb.DynamicButton, "Wiki page thumb '{0}' has no DynamicButton", thumb.name);

                // Belongs to another tab — a real state, not missing setup.
                if (thumb.TabIndex != wikiState.ActiveTabIndex) {
                    thumb.gameObject.SetActive(false);
                    continue;
                }

                Assert.True(thumb.PageIndex >= 0 && thumb.PageIndex < activeTab.Pages.Length,
                    "Wiki page thumb has out-of-range PageIndex {0} for tab '{1}'", thumb.PageIndex, activeTab.name);

                // A locked page is a real state too — hide its thumbnail.
                int unlockedIndex = WikiUtility.GetUnlockedIndex(activeTab, progressState, thumb.PageIndex);
                if (unlockedIndex == -1) {
                    thumb.gameObject.SetActive(false);
                    continue;
                }

                thumb.gameObject.SetActive(true);
                thumb.DynamicButton.interactable = true;

                // Same split as the page illustration: material pages take their thumbnail from the
                // material asset, default pages from the authored icon.
                WikiPageData thumbPage = activeTab.Pages[thumb.PageIndex];
                Assert.NotNullOrDestroyed(thumbPage, "Tab '{0}' has a null page at index {1}", activeTab.name, thumb.PageIndex);

                Sprite thumbSprite = thumbPage.Icon;
                if (thumbPage.IsMaterialPage) {
                    MaterialAsset material = Find.NamedAsset<MaterialAsset>(thumbPage.MaterialId);
                    Assert.NotNullOrDestroyed(material, "Wiki page '{0}' references unknown material '{1}'", thumbPage.name, thumbPage.MaterialId);
                    thumbSprite = material.GemSprite;
                }
                thumb.DynamicButton.image.sprite = thumbSprite;

                // Record the selected in-window thumb so the highlight can be placed over it.
                bool inWindow = unlockedIndex >= wikiState.PageWindowStartIndex && unlockedIndex < wikiState.PageWindowStartIndex + pageWindowSize;
                if (inWindow && thumb.PageIndex == wikiState.ActivePageIndex) {
                    selectedThumbRect = thumb.DynamicButton.image.rectTransform;
                }
            }

            return selectedThumbRect;
        }

        // Greys out each arrow when the window can't scroll further that way. They stay visible at
        // the ends so the layout doesn't shift.
        private static void RefreshPaginatorArrows(WikiLayoutState layout, WikiState wikiState, WikiContent content, PlayerProgressState progressState) {
            Assert.NotNullOrDestroyed(layout.PrevPage, "WikiLayoutState.PrevPage not authored");
            Assert.NotNullOrDestroyed(layout.NextPage, "WikiLayoutState.NextPage not authored");

            layout.PrevPage.interactable = WikiUtility.CanScrollPageWindowLeft(wikiState, content, progressState);
            layout.NextPage.interactable = WikiUtility.CanScrollPageWindowRight(wikiState, content, progressState);
        }

        #endregion // Paginator

        #region Content Resolution

        // Resolves the active tab asset. Asserts rather than returning null — an out-of-range
        // ActiveTabIndex means a selection command wrote a bad index, not that there's nothing to
        // draw.
        private static WikiTabData ResolveActiveTab(WikiState wikiState, WikiContent content) {
            Assert.True(content.Tabs != null && content.Tabs.Length > 0, "WikiContent has no authored tabs");
            Assert.True(wikiState.ActiveTabIndex >= 0 && wikiState.ActiveTabIndex < content.Tabs.Length,
                "WikiState.ActiveTabIndex {0} is out of range for {1} authored tabs", wikiState.ActiveTabIndex, content.Tabs.Length);

            WikiTabData tab = content.Tabs[wikiState.ActiveTabIndex];
            Assert.NotNullOrDestroyed(tab, "WikiContent.Tabs has a null entry at index {0}", wikiState.ActiveTabIndex);
            return tab;
        }

        // Resolves the active page within the already-resolved tab. Same contract as
        // ResolveActiveTab — never returns null.
        private static WikiPageData ResolveActivePage(WikiTabData activeTab, int activePageIndex) {
            Assert.True(activeTab.Pages != null && activeTab.Pages.Length > 0, "Wiki tab '{0}' has no authored pages", activeTab.name);
            Assert.True(activePageIndex >= 0 && activePageIndex < activeTab.Pages.Length,
                "WikiState.ActivePageIndex {0} is out of range for tab '{1}'", activePageIndex, activeTab.name);

            WikiPageData page = activeTab.Pages[activePageIndex];
            Assert.NotNullOrDestroyed(page, "Wiki tab '{0}' has a null page at index {1}", activeTab.name, activePageIndex);
            return page;
        }

        #endregion // Content Resolution
    }
}
