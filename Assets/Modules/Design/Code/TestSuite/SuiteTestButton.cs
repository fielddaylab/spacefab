using SpaceFab.UI;
using UnityEngine.UI;

namespace SpaceFab.Design
{
    /// <summary>
    /// Single "Test" button used by the toggle-input mode of the SimTable. Replaces the per-row
    /// Run buttons and the suite-level SuiteRunButton when DesignMinigameState.UseToggleInputMode
    /// is true. Click handler is wired by SimulateUIUtility.AssignSuiteListeners and reads the
    /// matched test row from InputToggleState.LastMatchedRowIndex.
    /// SuiteTestButtonRefreshSystem owns interactable + sprite state and hides the button when
    /// UseToggleInputMode is false.
    /// </summary>
    public class SuiteTestButton : DynamicButton
    {
        public Image Icon;
    }
}
