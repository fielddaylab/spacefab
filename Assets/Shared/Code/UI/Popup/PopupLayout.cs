using System;
using BeauRoutine;
using BeauUtil;
using BeauUtil.Debugger;
using FieldDay;
using FieldDay.UI;
using FieldDay.UI.Widgets;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace SpaceFab.UI {
    public sealed class PopupLayout : MonoBehaviour {
        public TMP_Text Header;
        public TMP_Text Text;

        [Header("Buttons")]
        public RectTransform ButtonGroup;
        public GuiButton ButtonA;
        public GuiButton ButtonB;
        public float DefaultButtonSpacing;
        public GuiButton CloseButton;

        [Header("Layout")]
        public LayoutSizeGroup LayoutGroup;
        public LayoutOptions LayoutOptions;
    }

    public struct PopupRequestContent {
        public string Header;
        public string Text;
        public PopupRequestButton ButtonA;
        public PopupRequestButton ButtonB;
        public StringHash32 CloseResponseId;
        public PopupRequestFlags Flags;
        public PopupResponseDelegate Callback;
    }

    public struct PopupRequestButton {
        public StringHash32 ResponseId;
        public ColorPalette2 Tint;
        public string Label;
    }

    public delegate void PopupResponseDelegate(StringHash32 buttonId);

    [Flags]
    public enum PopupRequestFlags : ushort {
        DisplayClose = 0x01,
    }

    static public partial class PopupUtility {
        static public void PopulateContents(PopupLayout contents, in PopupRequestContent request) {
            contents.Header.SetTextAndActive(request.Header);
            contents.Text.SetTextAndActive(request.Text);

            bool hasButtonA = PopulateButton(contents.ButtonA, request.ButtonA);
            bool hasButtonB = PopulateButton(contents.ButtonB, request.ButtonB);
            bool hasBothButtons = hasButtonA & hasButtonB;

            contents.ButtonGroup.gameObject.SetActive(hasButtonA | hasButtonB);

            bool showClose = (request.Flags & PopupRequestFlags.DisplayClose) != 0 || !request.CloseResponseId.IsEmpty;
            if (!showClose && !hasButtonA && !hasButtonB) {
                showClose = true;
                Log.Warn("[PopupUtility] Popup had no valid buttons or close button - forcing close on!");
            }

            contents.CloseButton.gameObject.SetActive(showClose);
            contents.CloseButton.Id = request.CloseResponseId;

            Positioning.ResizeToPreferred(contents.Header);
            Positioning.ResizeToPreferred(contents.Text);

            if (hasBothButtons) {
                Positioning.SetOffsetX(contents.ButtonA.Rect, -contents.DefaultButtonSpacing);
                Positioning.SetOffsetX(contents.ButtonB.Rect, contents.DefaultButtonSpacing);
            } else {
                Positioning.SetOffsetX(contents.ButtonA.Rect, 0);
                Positioning.SetOffsetX(contents.ButtonB.Rect, 0);
            }

            contents.LayoutGroup.VerticalLayout(contents.LayoutOptions);
        }

        static public bool PopulateButton(GuiButton popupButton, in PopupRequestButton buttonConfig) {
            if (buttonConfig.ResponseId.IsEmpty && string.IsNullOrEmpty(buttonConfig.Label)) {
                popupButton.gameObject.SetActive(false);
                return false;
            }

            popupButton.gameObject.SetActive(true);
            popupButton.TextGraphic.SetText(buttonConfig.Label);
            popupButton.ColorTinter.SetTint((ColorPalette2F) buttonConfig.Tint);
            popupButton.Id = buttonConfig.ResponseId;
            return true;
        }
    }
}