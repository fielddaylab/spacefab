using BeauUtil;
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
    }
}
