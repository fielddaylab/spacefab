using BeauUtil;
using FieldDay;
using FieldDay.Scripting;
using FieldDay.Systems;
using SpaceFab.Save;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace SpaceFab.Overarching {
    static public partial class OverarchingTransitions {
        static public void AdvanceContract() {
            GameLoop.SuspendUpdates(UpdateMasks.EntireGame);
            GameLoop.ResumeUpdates(ScriptUtility.RuntimeUpdateMask);
            Find.State(out ChapterState chapterState, out PlayerProgressState progressState, out ContractState contractState, out MinigameSaveStates saveStates);
            ChapterUtility.LoadNextChapter(chapterState, progressState, contractState, saveStates);
        }
        
        static public void ToMinigame() {
            Find.State(out MinigameZonesState zonesState);
            GameLoop.SuspendUpdates(UpdateMasks.EntireGame);
            GameLoop.ResumeUpdates(ScriptUtility.RuntimeUpdateMask);
            Game.Scenes.LoadMainScene(zonesState.Zones[zonesState.CurrSelectedIndex].MinigameScene);
            Game.Events.Dispatch(GameEvents.OnMinigameLoad);
        }
    }
}
