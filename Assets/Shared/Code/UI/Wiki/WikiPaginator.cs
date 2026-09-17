using BeauUtil.UI;
using FieldDay.UI;
using FieldDay.UI.Widgets;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace SpaceFab.UI {
    public class WikiPaginator : GuiWidget {
        [Header("Controls")]
        public GuiButton PageScrollLeft;
        public GuiButton PageScrollRight;

        [Header("Page")]
        public GuiButton[] PageIcons;
    }
}