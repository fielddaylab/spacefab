using SpaceFab.UI;
using UnityEngine.UI;

namespace SpaceFab.Design
{
    /// <summary>
    /// Suite-level run button on the SimTable. Single Play / Pause / Resume trigger for the
    /// whole suite: SimulateUIUtility.HandleSuiteRunButtonClick decides which request to fire
    /// based on current phase, and SuiteRunButtonRefreshSystem swaps Icon based on Phase.
    /// Shares the SuiteRunButtonState sprite-lookup with the per-row SuiteRunRowButton.
    /// </summary>
    public class SuiteRunButton : DynamicButton
    {
        public Image Icon;
    }
}
