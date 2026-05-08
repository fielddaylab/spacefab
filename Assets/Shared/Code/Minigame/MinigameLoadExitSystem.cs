using BeauUtil;
using FieldDay;
using FieldDay.Systems;
using SpaceFab.Design;
using SpaceFab.Save;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace SpaceFab {
    /// <summary>
    /// Drives the enter/exit flow for a minigame: imports state on load, flips update masks
    /// to the incoming minigame's default mask, and on exit exports state, saves, and loads
    /// the return scene. Runs in Update under MinigameTransitionMask.
    /// </summary>
    public class MinigameLoadExitSystem : SystemComponent {
        public override unsafe void RegisterSystems(ref SystemRegistrationTable ecs) {
            ecs.Register(&ProcessWork,
                new SysUpdate(GameLoopPhase.Update, 1, UpdateMasks.MinigameTransitionMask),
                new SysPermissions()
                    .ReadWriteShared<MinigameLoadExitState>()
                    .ReadShared<MinigameStateInterfacer>()
                    .ReadWriteShared<MinigameSaveStates>()
                    .ReadShared<ReturnMenuState>()
                    .ReadShared<SaveLoadState>()
            );
        }

        // Steps the minigame load/exit phase machine forward one tick.
        static private void ProcessWork(float deltaTime) {
            Find.State(
                out MinigameLoadExitState loadExitState,
                out MinigameStateInterfacer interfacer,
                out MinigameSaveStates saveStates,
                out ReturnMenuState returnState
                );
            Find.State(out SaveLoadState saveOpState);

            switch (loadExitState.Phase) {
                case MinigameLoadExitPhase.Loading:
                    Debug.Log("[MinigameLoadExitSystem] Importing state...");
                    interfacer.MinigameState.ImportState(saveStates);
                    loadExitState.Phase = MinigameLoadExitPhase.Loaded;
                    break;
                case MinigameLoadExitPhase.Loaded:
                    Debug.Log("[MinigameLoadExitSystem] Imported!");
                    // Suspend everything, then resume only the incoming minigame's own update mask
                    GameLoop.SuspendUpdates(Bits.All32);
                    GameLoop.ResumeUpdates(interfacer.MinigameState.DefaultUpdateMask);
                    loadExitState.Phase = MinigameLoadExitPhase.None;
                    break;
                case MinigameLoadExitPhase.Exiting:
                    Debug.Log("[MinigameLoadExitSystem] Exporting state...");
                    interfacer.MinigameState.ExportState(ref saveStates);
                    SaveUtility.Save(SaveSlot.Main);
                    loadExitState.Phase = MinigameLoadExitPhase.SavingOnExit;
                    break;
                case MinigameLoadExitPhase.SavingOnExit:
                    if (!saveOpState.Operation) {
                        // saving completed
                        loadExitState.Phase = MinigameLoadExitPhase.Exited;
                    }
                    break;
                case MinigameLoadExitPhase.Exited:
                    Debug.Log("[MinigameLoadExitSystem] Exported!");
                    Game.Events.Dispatch(GameEvents.OnMinigameExit);
                    Game.Scenes.LoadMainScene(returnState.ReturnScene);
                    loadExitState.Phase = MinigameLoadExitPhase.None;
                    break;
                default:
                    break;
            }
        }
    }
}
