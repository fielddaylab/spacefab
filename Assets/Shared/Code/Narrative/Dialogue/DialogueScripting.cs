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

        [LeafMember("MusicStop")]
        public static void Leaf_StopMusic() {
            MusicPlayer.Stop();
        }

        [LeafMember("MusicStopNow")]
        public static void Leaf_StopMusicNow() {
            MusicPlayer.Stop(0);
        }

        [LeafMember("MusicPlay")]
        public static void Leaf_PlayMusic(StringHash32 music) {
            MusicPlayer.SetLoopingTrack(music);
        }

        [LeafMember("MusicPreload")]
        public static void Leaf_PreloadMusic(StringHash32 music) {
            Game.Audio.QueuePreload(music);
        }

        [LeafMember("MusicUnload")]
        public static void Leaf_UnloadMusic(StringHash32 music) {
            Game.Audio.QueueUnload(music);
        }
    }
}
