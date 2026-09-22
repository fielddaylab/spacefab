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
    public class WikiStateV2 : SharedStateComponent, IRegistrationCallbacks {
        [NonSerialized] public bool Expanded;

        [NonSerialized] public int CurrentTabId = -1;
        [NonSerialized] public int CurrentPageId = -1;
        [NonSerialized] public int CurrentPageScroll = 0;

        [NonSerialized] public WikiTabMemoryRecord[] TabMemory = new WikiTabMemoryRecord[WikiUtility.MaxTabs];

        [NonSerialized] public int QueuedTabId = -1;
        [NonSerialized] public int QueuedPageId = -1;
        [NonSerialized] public int QueuedPageScroll = 0;
        [NonSerialized] public WikiContentUpdateResult QueuedContentUpdated;
        [NonSerialized] public WikiVisualDirty DirtyFlags;
        [NonSerialized] public bool ContentDirty;

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
        }
    }

    public struct WikiTabMemoryRecord {
        public sbyte Scroll;
        public sbyte PageId;
    }

    static public partial class WikiUtility {
        static public void WipeTabMemory(WikiStateV2 wikiState) {
            for (int i = 0; i < wikiState.TabMemory.Length; i++) {
                wikiState.TabMemory[i] = new WikiTabMemoryRecord() {
                    PageId = -1,
                    Scroll = 0
                };
            }
        }
    }
}