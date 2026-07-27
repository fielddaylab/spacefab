using System.Collections.Generic;
using BeauUtil;
using FieldDay.Scenes;
using FieldDay.UI;
using UnityEngine;
using UnityEngine.UI;

namespace SpaceFab.UI {
    public sealed class PopupPrompt : SharedPanel, IParameterizedGuiPanel<PopupRequestContent>, IPopupPanel, IScenePreload {
        public Canvas Canvas;

        public CanvasGroup Fader;
        public LayoutOffset GroupOffset;
        public PopupLayout Contents;

        public void Populate(in PopupRequestContent parms) {
            PopupUtility.PopulateContents(Contents, parms);
        }

        public override void Show() {
            base.Show();
            Input.TryPushPriority();
            PopupUtility.PushState();
        }

        public override void Hide() {
            Input.TryPopPriority();
            PopupUtility.PopState();
            base.Hide();
        }

        public IEnumerator<WorkSlicer.Result?> Preload() {
            Canvas.enabled = false;
            Input.SetInputOverride(false);
            Fader.alpha = 0;
            return null;
        }
    }
}