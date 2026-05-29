using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace SpaceFab.Supply {
    /// <summary>
    /// One ship's breakdown row in the expanded progress meter: ship icon + name plus the
    /// ship's own Risk / Cost / Time, each shown as a static section icon followed by a
    /// numeric count (e.g. [risk icon] 3). Pure ref-holder; filled by
    /// SupplyProgressMeterUtility. Rows are authored on the prefab (one per ship slot) and
    /// toggled active by the utility.
    /// </summary>
    public class SupplyShipBreakdownRow : MonoBehaviour {
        // The whole row, toggled active when this ship has a route.
        public GameObject Root;

        public Image ShipIcon;
        public TMP_Text ShipName;

        // Numeric counts shown beside each section's authored icon.
        public TMP_Text RiskText;
        public TMP_Text CostText;
        public TMP_Text TimeText;
    }
}
