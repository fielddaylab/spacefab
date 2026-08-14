using BeauUtil;
using BeauUtil.Debugger;
using FieldDay;
using FieldDay.Music;
using FieldDay.Scripting;
using Leaf.Runtime;

namespace SpaceFab.Narrative {
    /// <summary>
    /// Leaf-callable entry points for dialogue boxes. Routes id'd actions through
    /// ScriptUtility's printer registry — the printer's own m_Id is the addressing key.
    /// </summary>
    public static class DialogueScripting {
        // Dismisses the minigame dialogue box with the given printer id. No-op if no printer
        // is registered under that id, or if the registered printer isn't a MinigameDialogueBox
        // (overarching DialogueBox dismisses via its Next button, not via Leaf).
        [LeafMember("MinigameDismissDialogue")]
        public static void Leaf_DismissMinigameDialogue(StringHash32 printerId) {
            IDialoguePrinter printer = ScriptUtility.GetDialoguePrinter(printerId);
            if (printer is MinigameDialogueBox box) {
                box.Dismiss();
            }
        }

        // Arms the minigame dialogue box's primary button for the NEXT line it prints, with the
        // given action's label and behavior. The button clears as soon as a later line prints, or
        // when the box is dismissed. actionId is a DialogueButtonAction enum name (e.g.
        // "OpenDatabase"), case-insensitive; unknown names warn and no-op. printerId defaults to
        // the scene's default printer id.
        //
        // Addressed through the printer registry rather than the calling thread: at $call time the
        // thread hasn't yet taken ownership of the printer for the upcoming line.
        [LeafMember("ShowPrimaryPALButton")]
        public static void Leaf_ShowPrimaryPALButton(string actionId, StringHash32 printerId = default) {
            if (!DialogueButtonActionUtility.TryResolve(actionId, out DialogueButtonAction action)) {
                Log.Warn("[DialogueScripting] Unknown primary button action '{0}'", actionId);
                return;
            }

            IDialoguePrinter printer = ScriptUtility.GetDialoguePrinter(printerId);
            if (printer is MinigameDialogueBox box) {
                box.ArmPrimaryButton(action);
            }
        }
    }
}
