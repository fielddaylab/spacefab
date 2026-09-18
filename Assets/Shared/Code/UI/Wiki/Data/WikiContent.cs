using System;
using System.Collections.Generic;
using BeauUtil;
using BeauUtil.Debugger;
using FieldDay;
using FieldDay.Components;
using FieldDay.Scenes;
using SpaceFab.Materials;

namespace SpaceFab.UI {
    /// <summary>
    /// Root BatchedComponent of the shared wiki prefab. Carries the ordered list of tabs the
    /// active minigame exposes. One instance per scene. Consumed by WikiSelectSystem (to
    /// resolve TabId/PageId lookups for OpenTo calls) and WikiVisualsUtility (to render
    /// tab/page content).
    /// </summary>
    public class WikiContent : BatchedComponent, IRegistrationCallbacks {
        // Runtime-only: the tab set is authored per-minigame on GlobalUISceneConfig.WikiTabs and
        // pushed here by QuickToolbar on scene late enable. Empty until that happens, so a scene
        // without a config simply shows no tabs rather than tripping the strip asserts.
        [NonSerialized] public WikiTabData[] Tabs = Array.Empty<WikiTabData>();

        // Number of page thumbnails visible at once in the paginator strip. Scrolling moves the
        // strip one slot at a time, keeping the selected page inside the window.
        public int PageWindowSize = 5;

        [NonSerialized] public IWikiContentFilter ContentFilter;

        [NonSerialized] public WikiContentList AvailableTabs;
        [NonSerialized] public WikiContentList[] TabPages = new WikiContentList[WikiUtility.MaxTabs];

        public void OnDeregister() {
            ContentFilter = null;
            Tabs = null;
        }

        public void OnRegister() {
            ContentFilter = BaseWikiContentFilter.Instance;
        }
    }

    public struct WikiContentList {
        public BitSet32 Availability;
        public int Count;
        public unsafe fixed sbyte Indices[WikiUtility.MaxPages];
    }

    public interface IWikiContentFilter {
        PageAvailabilityOverride GetPageAvailability(PlayerProgressState playerProgress, WikiPageData pageData);
        MaterialPropertyRecord GetMaterialProgress(PlayerProgressState playerProgress, StringHash32 materialId);
    }

    public enum PageAvailabilityOverride {
        Default,
        AlwaysShow,
        AlwaysHide
    }

    public sealed class BaseWikiContentFilter : IWikiContentFilter {
        static public readonly BaseWikiContentFilter Instance = new BaseWikiContentFilter();

        public MaterialPropertyRecord GetMaterialProgress(PlayerProgressState playerProgress, StringHash32 materialId) {
            return GetDefaultMaterialProgress(playerProgress, materialId);
        }

        public PageAvailabilityOverride GetPageAvailability(PlayerProgressState playerProgress, WikiPageData pageData) {
            return PageAvailabilityOverride.Default;
        }

        static public MaterialPropertyRecord GetDefaultMaterialProgress(PlayerProgressState playerProgress, StringHash32 materialId) {
            playerProgress.MaterialProperties.TryGetValue(materialId, out MaterialPropertyRecord record);
            return record;
        }
    }

    static public partial class WikiUtility {
        public const int MaxTabs = 4;
        public const int MaxPages = 20;

        #region List Updates

        /// <summary>
        /// Updates the list of available pages for the given tab.
        /// </summary>
        static public unsafe WikiListUpdateResult UpdatePageList(ref WikiContentList pageList, WikiTabData tabData, PlayerProgressState progressState, IWikiContentFilter contentFilter) {
            Assert.True(tabData.Pages.Length <= MaxPages, "Too many pages in tab '{0}'!", tabData.AssetId);

            BitSet32 originalBits = pageList.Availability;

            pageList.Availability.Clear();
            pageList.Count = 0;
            for(int pageIndex = 0; pageIndex < tabData.Pages.Length; pageIndex++) {
                if (IsPageAvailable(progressState, tabData.Pages[pageIndex], contentFilter)) {
                    pageList.Availability.Set(pageIndex);
                    pageList.Indices[pageList.Count++] = (sbyte) pageIndex;
                }
            }

            if (originalBits != pageList.Availability) {
                return WikiListUpdateResult.Changed;
            }
            return WikiListUpdateResult.NoChange;
        }

        static private bool IsPageAvailable(PlayerProgressState progressState, WikiPageData pageData, IWikiContentFilter contentFilter) {
            Assert.NotNullOrDestroyed(contentFilter);
            switch(contentFilter.GetPageAvailability(progressState, pageData)) {
                case PageAvailabilityOverride.AlwaysHide: {
                    return false;
                }
                case PageAvailabilityOverride.AlwaysShow: {
                    return true;
                }
            }
            return pageData.IsUnlockedByDefault || progressState.UnlockedWikiPages.Contains(pageData.AssetId);
        }

        /// <summary>
        /// Updates the list of available tabs.
        /// </summary>
        static public unsafe BitSet32 UpdateTabList(ref WikiContentList tabList, WikiContentList[] tabPageData, int tabCount) {
            Assert.True(tabCount <= tabPageData.Length && tabCount < MaxTabs, "Too many tabs!");

            BitSet32 originalBits = tabList.Availability;

            tabList.Availability.Clear();
            tabList.Count = 0;
            for(int tabIndex = 0; tabIndex < tabCount; tabIndex++) {
                if (tabPageData[tabIndex].Count > 0) {
                    tabList.Availability.Set(tabIndex);
                    tabList.Indices[tabList.Count++] = (sbyte)tabIndex;
                }
            }

            return originalBits ^ tabList.Availability;
        }
    
        /// <summary>
        /// Updates the lists of available tabs and pages.
        /// </summary>
        static public WikiContentUpdateResult UpdateAvailableContent(WikiContent content, PlayerProgressState progressState) {
            WikiContentUpdateResult result = default;

            for(int tabIndex = 0; tabIndex < content.Tabs.Length; tabIndex++) {
                if (UpdatePageList(ref content.TabPages[tabIndex], content.Tabs[tabIndex], progressState, content.ContentFilter) == WikiListUpdateResult.Changed) {
                    result.PageListsUpdated.Set(tabIndex);
                }
            }

            result.AvailableTabsUpdated = UpdateTabList(ref content.AvailableTabs, content.TabPages, content.Tabs.Length);
            return result;
        }

        #endregion // List Updates

        #region Indices



        #endregion // Indices
    }

    public enum WikiListUpdateResult {
        NoChange,
        Changed
    }

    public struct WikiContentUpdateResult {
        public BitSet32 AvailableTabsUpdated;
        public BitSet32 PageListsUpdated;
    }
}
