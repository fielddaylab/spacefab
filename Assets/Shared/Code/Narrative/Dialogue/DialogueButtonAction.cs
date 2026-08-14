using BeauUtil;
using FieldDay;
using SpaceFab.UI;

namespace SpaceFab.Narrative {
    /// <summary>
    /// Predefined actions a dialogue box's primary button can perform.
    ///
    /// Every entry needs an arm in both DialogueButtonActionUtility.GetLabel (what the button reads)
    /// and DialogueButtonActionUtility.Invoke (what clicking it does).
    /// </summary>
    public enum DialogueButtonAction {
        None,
        OpenProperties,
        OpenObservations
    }

    /// <summary>
    /// Label and behavior lookup for DialogueButtonAction, plus enum-name resolution for Leaf
    /// callers. Paired with MinigameDialogueBox, which stores the armed action and calls Invoke
    /// when the button is clicked.
    /// </summary>
    public static class DialogueButtonActionUtility {
        // Asset name of the wiki tab OpenProperties targets.
        private static readonly StringHash32 MaterialPropertiesTabId = "Material Properties_Tab";

        // Asset name of the wiki tab OpenObservations targets.
        private static readonly StringHash32 MaterialObservationsTabId = "Observations_Tab";

        // Resolves a DialogueButtonAction enum name (e.g. "OpenProperties") to its value,
        // case-insensitively. Returns false with action left as None for an empty or unrecognized
        // name, so script authors get a no-op rather than a wrong action. None itself is not
        // resolvable — arming "nothing" is expressed by simply not calling ShowPrimaryPALButton.
        public static bool TryResolve(string name, out DialogueButtonAction action) {
            action = DialogueButtonAction.None;
            if (string.IsNullOrEmpty(name)) { return false; }

            switch (name.Trim().ToUpperInvariant()) {
                case "OPENPROPERTIES":
                    action = DialogueButtonAction.OpenProperties;
                    return true;
                case "OPENOBSERVATIONS":
                    action = DialogueButtonAction.OpenObservations;
                    return true;
            }

            return false;
        }

        /// <summary>
        /// Button text for the given action. Returns null for None and for any action without
        /// authored copy — callers should treat a null label as "don't show the button".
        /// </summary>
        public static string GetLabel(DialogueButtonAction action) {
            switch (action) {
                case DialogueButtonAction.OpenProperties: return "Open Database";
                case DialogueButtonAction.OpenObservations: return "Open Database";
                default: return null;
            }
        }

        /// <summary>
        /// Performs the action. Each arm is a thin call into the owning module's utility, and
        /// no-ops when that module isn't present in the current scene.
        /// </summary>
        public static void Invoke(DialogueButtonAction action) {
            switch (action) {
                case DialogueButtonAction.OpenProperties:
                    // WikiUtility.OpenTo resolves WikiState without a presence guard of its own, so
                    // check here first (same contract as WikiScripting's Leaf members).
                    if (!Game.SharedState.Has<WikiState>()) { return; }

                    // Empty page id: open to the tab's remembered page, or its first unlocked one.
                    // In a scene whose tab set has no Material Properties tab, the id is dropped by
                    // the resolver and the wiki simply opens where it was.
                    WikiUtility.OpenTo(MaterialPropertiesTabId, default);
                    break;
                case DialogueButtonAction.OpenObservations:
                    if (!Game.SharedState.Has<WikiState>()) { return; }
                    WikiUtility.OpenTo(MaterialObservationsTabId, default);
                    break;
                default:
                    break;
            }
        }
    }
}
