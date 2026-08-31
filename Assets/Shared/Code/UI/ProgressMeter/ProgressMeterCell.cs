using FieldDay.Components;
using UnityEngine;
using UnityEngine.UI;

namespace SpaceFab {
    /// <summary>
    /// View component for one cell of the ProgressMeter (Cycles or Funds row). Holds the
    /// always-visible base Image and the state-driven overlay Image. ProgressMeterUtility
    /// writes to OverlayImage's sprite/enabled to communicate Pending / Filled / per-funds
    /// states; EMPTY simply disables OverlayImage. No logic lives here.
    /// </summary>
    public class ProgressMeterCell : BatchedComponent {
        public RectTransform Rect;
        public Image BaseImage;
        public Image OverlayImage;
        public Image xMarkImage;
    }
}
