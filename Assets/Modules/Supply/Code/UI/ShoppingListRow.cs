using SpaceFab.Materials;
using SpaceFab.Research;
using UnityEngine;
using UnityEngine.UI;

namespace SpaceFab.Supply {
    /// <summary>
    /// One row in the shopping list: a property chip describing a contract
    /// requirement, paired with a slot that shows the icon of a gathered
    /// material satisfying it (or an empty placeholder when nothing does).
    /// Pure view — pooled and driven by ShoppingListLoadUtility; holds no
    /// state of its own beyond the inspector-wired widgets.
    /// </summary>
    public class ShoppingListRow : MonoBehaviour {
        // Reused Research chip view: renders the requirement's icon + label.
        public ResearchObservationChip PropertyChip;

        // Slot icon, shown over the background only when satisfied.
        public Image SlotIcon;

        // Renders the property chip for this requirement. Always filled —
        // the chip shows the requirement itself, not a discovery state.
        public void SetProperty(string name, ObservationType type) {
            if (PropertyChip != null) {
                PropertyChip.SetState(name, ChipFillState.Filled, false, type);
            }
        }

        // Sets the slot to a gathered material's icon, or clears it. The
        // background stays visible either way; only the overlaid icon toggles.
        public void SetSlot(Sprite icon) {
            if (SlotIcon != null) {
                SlotIcon.sprite = icon;
                SlotIcon.enabled = icon != null;
            }
        }
    }
}
