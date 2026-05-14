using UnityEngine;
using UnityEngine.UI;

namespace SpaceFab.Design
{
    /// <summary>
    /// Per-output verdict mark on a suite-row Output column. Lives on the same GameObject as
    /// SuiteCol (hooked up in the prefab editor). Pure data — VerdictVisualizerRefreshSystem
    /// reads SimulateUIState.CellVerdicts and writes Icon.sprite / Icon.enabled. SuiteRow.Verdicts
    /// caches the per-row refs at BuildTable time so the refresh system avoids GetComponent.
    /// </summary>
    public class VerdictVisualizer : MonoBehaviour
    {
        public Image Icon;
    }
}
