using System;
using System.Collections.Generic;
using BeauUtil;
using BeauUtil.UI;
using FieldDay;
using FieldDay.Animation;
using FieldDay.Scenes;
using FieldDay.UI;
using FieldDay.UI.Widgets;
using UnityEngine;
using UnityEngine.UI;

namespace SpaceFab.UI {
    public sealed class PopupPrompt : SharedPanel, IParameterizedGuiPanel<PopupRequestContent>, IPopupPanel, IScenePreload {
        public Canvas Canvas;

        public CanvasGroup Fader;
        public LayoutOffset GroupOffset;
        public PopupLayout Contents;

        [NonSerialized] public PopupResponseDelegate Callback;

        public void Populate(in PopupRequestContent parms) {
            PopupUtility.PopulateContents(Contents, parms);
            Callback = parms.Callback;
        }

        public override void Show() {
            base.Show();
            if (Input.TryPushPriority()) {
                Input.ClearInputOverride();
                PopupUtility.PushState();
            }
            Canvas.enabled = true;
            Fader.alpha = 1;
        }

        public override void Hide() {
            Canvas.enabled = false;
            Fader.alpha = 0;
            if (Input.TryPopPriority()) {
                Input.SetInputOverride(false);
                PopupUtility.PopState();
            }
            base.Hide();
        }

        public IEnumerator<WorkSlicer.Result?> Preload() {
            Canvas.enabled = false;
            Input.SetInputOverride(false);
            Fader.alpha = 0;
            gameObject.SetActive(false);

            Contents.ButtonA.OnClick.Register(OnButtonClick);
            Contents.ButtonB.OnClick.Register(OnButtonClick);
            Contents.CloseButton.OnClick.Register(OnButtonClick);

            return null;
        }

        private void OnButtonClick(PointerListener.EventData evt) {
            StringHash32 id = evt.Source.GetComponentInParent<GuiWidget>().Id;
            PopupResponseDelegate callback = Callback;
            Callback = null;
            Hide();

            if (callback != null) {
                callback(id);
            }
        }
    }

    static public partial class PopupUtility {
        static public void DisplayGenericPopup(string title, string text, PopupResponseDelegate callback = null) {
            PopupPrompt prompt = Find.Panel<PopupPrompt>();
            PopupRequestContent content = default;
            content.Header = title;
            content.Text = text;
            content.Flags = PopupRequestFlags.DisplayClose;
            prompt.Populate(content);
            prompt.Show();
        }
    }
}