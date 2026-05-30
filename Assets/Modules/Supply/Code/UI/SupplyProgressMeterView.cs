using UnityEngine;

namespace SpaceFab.Supply {
    /// <summary>
    /// Aggregate "Result" panel view for the Supply mini progress meter. Pure ref-holder:
    /// authored cell arrays for the three sections, filled by SupplyProgressMeterUtility.
    /// The number of cells per section is fixed by what's authored on the prefab; the utility
    /// fills a prefix of each and clears the rest. Reuses ProgressMeterCell (base + overlay).
    /// </summary>
    public class SupplyProgressMeterView : MonoBehaviour {
        // Summed risk across ships, rendered as filled diamond cells.
        public ProgressMeterCell[] RiskCells;

        // Contract funds, rendered as remaining (yellow) then spent (red) bars.
        public ProgressMeterCell[] CostCells;

        // Max time across ships, rendered as filled dot cells.
        public ProgressMeterCell[] TimeCells;
    }
}
