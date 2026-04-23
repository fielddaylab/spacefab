using FieldDay.Components;
using FieldDay.UI;
using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace SpaceFab.UI {
    public sealed class AutoSizedButton : MonoBehaviour {
        public Button Button;
        public LayoutSizeGroup Layout;
        public TMP_Text TextContent;

        [NonSerialized] private bool m_Clicked;

        private void Awake() {
            Button.onClick.AddListener(() => m_Clicked = true);
        }

        public bool ConsumeClick() {
            bool wasClicked;
            if ((wasClicked = m_Clicked)) {
                m_Clicked = false;
            }
            return wasClicked;
        }
    }
}