using System;
using UnityEngine;

namespace SpaceFab.Research {
    public class ResearchContractRequirementRow : MonoBehaviour {
        public ResearchObservationChip Chip;

        // Wiki-open handler bound by ContractRequirementsVisualUtility while this row shows a
        // goal whose property page exists and is unlocked. Null when the row is hidden, the
        // scene ships no wiki, or the page is still locked.
        [NonSerialized] public Action WikiClickHandler;
    }
}
