using BeauUtil;
using BeauUtil.UI;
using FieldDay.Scenes;
using FieldDay.UI;
using FieldDay.UI.Widgets;
using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace SpaceFab.UI {
    public class WikiPaginator : GuiWidget, IScenePreload {
        [Header("Controls")]
        public GuiButton PageScrollLeft;
        public GuiButton PageScrollRight;

        [Header("Page")]
        public WikiPageButton[] Pages;

        IEnumerator<WorkSlicer.Result?> IScenePreload.Preload() {
            Action<PointerListener.EventData> handler = WikiUtility.HandlePageClicked;
            for (int i = 0; i < Pages.Length; i++) {
                Pages[i].Widget.OnClick.Register(handler);
            }
            return null;
        }
    }

    static public partial class WikiUtility {
        static public void HandlePageClicked(PointerListener.EventData data) {
            int queuedPageId = GuiButton.GetData(data).AsInt();
            // TODO: queue page select
        }

        static public void HandlePageScrollClicked(PointerListener.EventData data) {
            int queuedScrollDirection = GuiButton.GetData(data).AsInt();
            // TODO: queue page scroll
        }
    }
}