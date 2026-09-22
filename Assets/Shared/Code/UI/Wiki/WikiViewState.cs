using System;
using System.Collections;
using BeauRoutine;
using BeauUtil;
using BeauUtil.Debugger;
using FieldDay;
using FieldDay.Animation;
using FieldDay.Scripting;
using FieldDay.SharedState;
using FieldDay.Systems;
using FieldDay.UI;
using Leaf.Runtime;
using SpaceFab.Materials;
using UnityEngine;

namespace SpaceFab.UI {
    public class WikiViewState : SharedStateComponent, IRegistrationCallbacks {
        [NonSerialized] public bool Expanded;

        [NonSerialized] public int CurrentTabId = -1;
        [NonSerialized] public int CurrentPageId = -1;
        [NonSerialized] public int CurrentPageScroll = 0;

        [NonSerialized] public WikiTabMemoryRecord[] TabMemory = new WikiTabMemoryRecord[WikiContentUtility.MaxTabs];

        [NonSerialized] public int QueuedTabId = -1;
        [NonSerialized] public int QueuedPageId = -1;
        [NonSerialized] public int QueuedPageScrollDirection = 0;
        [NonSerialized] public int QueuedPageScrollRestore = -1;
        [NonSerialized] public WikiContentUpdateResult QueuedContentUpdated;
        [NonSerialized] public bool ContentListsDirty;

        [NonSerialized] public AnimHandle ExpandAnim;

        public void OnRegister() {
            WikiUtility.WipeTabMemory(this);
            Game.Scenes.OnMainSceneLateEnable.Register(OnSceneLateEnable);
        }

        public void OnDeregister() {
            Game.Scenes.OnMainSceneLateEnable.Deregister(OnSceneLateEnable);
        }

        public void OnSceneLateEnable() {
            WikiUtility.WipeTabMemory(this);
            ContentListsDirty = true;
        }
    }

    public struct WikiTabMemoryRecord {
        public sbyte Scroll;
        public sbyte PageId;
    }

    static public partial class WikiUtility {
        static public void WipeTabMemory(WikiViewState wikiState) {
            for (int i = 0; i < wikiState.TabMemory.Length; i++) {
                wikiState.TabMemory[i] = new WikiTabMemoryRecord() {
                    PageId = -1,
                    Scroll = 0
                };
            }
        }

        static public unsafe void FlushContentChanges(WikiViewState state, WikiContent content, PlayerProgressState playerProgress) {
            if (state.ContentListsDirty) {
                WikiContentUpdateResult result = WikiContentUtility.UpdateAvailableContent(content, playerProgress);
                state.QueuedContentUpdated.AvailableTabsUpdated |= result.AvailableTabsUpdated;
                state.QueuedContentUpdated.PageListsUpdated |= result.PageListsUpdated;
                state.ContentListsDirty = false;

                if (state.CurrentTabId < 0 && content.AvailableTabs.Count > 0) {
                    state.QueuedTabId = content.AvailableTabs.Indices[0];
                }
            }
        }
    }
}