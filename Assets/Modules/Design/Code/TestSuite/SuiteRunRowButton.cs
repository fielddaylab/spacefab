using SpaceFab.UI;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace SpaceFab.Design
{
    /// <summary>
    /// Per-row run button on the suite table. Acts as the single Play / Pause / Resume trigger
    /// for its row: SimulateUIUtility.HandleRunButtonClick reads RowIndex to know which row to
    /// dispatch the request for, and SuiteRunRowButtonRefreshSystem swaps Icon based on the
    /// active SimulatePhase + SimulateRunState.CurrentRow.
    /// </summary>
    public class SuiteRunRowButton : DynamicButton
    {
        public Image Icon;

        // Stamped by SimulateUIUtility.CreateRowsAndCols when the table is built.
        // Identifies which test row this button drives.
        [HideInInspector] public int RowIndex;
    }
}
