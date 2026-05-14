using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace SpaceFab.UI {
    /// <summary>
    /// Pure-data MonoBehaviour on the wiki prefab's PageArea. Holds inspector references to
    /// the widgets that display the currently-selected WikiPageData — title, illustration,
    /// body. Consumed by WikiVisualsUpdateSystem, which pushes page fields into these widgets
    /// each frame while the panel is expanded.
    ///
    /// No logic here intentionally — this is the display-side equivalent of a pure-data
    /// component. The write path lives in WikiVisualsUpdateSystem (stubbed today).
    /// </summary>
    public class WikiPageContentWidgets : MonoBehaviour {
        public TextMeshProUGUI TitleText;
        public Image IllustrationImage;
        public TextMeshProUGUI BodyText;
    }
}
