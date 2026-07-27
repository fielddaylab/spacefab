using System;
using BeauUtil;
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
    }

    public struct PopupRequestButton {
        public StringHash32 ResponseId;
        public string Label;
        public ColorPalette2 Tint;
    }

    [Flags]
    public enum PopupRequestFlags : ushort {
        DisplayClose = 0x01,
    }

    static public partial class PopupUtility {
        static public void PopulateContents(PopupLayout contents, in PopupRequestContent request) {
            contents.Header.SetTextAndActive(request.Header);
            contents.Text.SetTextAndActive(request.Text);

            contents.CloseButton.gameObject.SetActive((request.Flags & PopupRequestFlags.DisplayClose) != 0);
            contents.CloseButton.Id = request.CloseResponseId;

            bool hasButtonA = PopulateButton(contents.ButtonA, request.ButtonA);
            bool hasButtonB = PopulateButton(contents.ButtonB, request.ButtonB);
            bool hasBothButtons = hasButtonA & hasButtonB;

            contents.ButtonGroup.gameObject.SetActive(hasButtonA | hasButtonB);

            if (hasBothButtons) {
                Positioning.SetAnchorX(contents.ButtonA.Rect, -contents.DefaultButtonSpacing);
                Positioning.SetAnchorX(contents.ButtonB.Rect, contents.DefaultButtonSpacing);
            } else {
                Positioning.SetAnchorX(contents.ButtonA.Rect, 0);
                Positioning.SetAnchorX(contents.ButtonB.Rect, 0);
            }
        }

        static public bool PopulateButton(GuiButton popupButton, in PopupRequestButton request) {
            if (request.ResponseId.IsEmpty) {
                popupButton.gameObject.SetActive(false);
                return false;
            }

            popupButton.gameObject.SetActive(true);
            popupButton.TextGraphic.SetText(request.Label);
            popupButton.ColorTinter.SetTint((ColorPalette2F) request.Tint);
            popupButton.Id = request.ResponseId;
            return true;
        }
    }
}