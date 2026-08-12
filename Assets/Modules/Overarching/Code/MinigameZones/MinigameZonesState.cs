using BeauUtil;
using FieldDay;
using FieldDay.Scenes;
using FieldDay.SharedState;
using FieldDay.Systems;
using SpaceFab.Save;
using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

namespace SpaceFab.Overarching
{
    [PreloadOrder(1000)]
    public class MinigameZonesState : SharedStateComponent, IScenePreload
    {
        [NonSerialized] public MinigameZoneStatus[] ZoneStatus = new MinigameZoneStatus[(int) MinigameId.COUNT];
        
        [NonSerialized] public MinigameZone HoverZone;
        [NonSerialized] public MinigameZone QueuedZone;

        [NonSerialized] public bool StatusDirty;

        IEnumerator<WorkSlicer.Result?> IScenePreload.Preload() {
            Find.State(out ContractState contracts, out MinigameSaveStates saves);
            Find.GlobalAsset(out MinigameDependencyGraph depGraph);
            if (!contracts.ContractId.IsEmpty) {
                MinigameZonesUtility.UpdateStatusFromSave(this, saves, depGraph);
            } else {
                MinigameZonesUtility.DisableAllZones(this);
            }
            return null;
        }
    }

    public static partial class MinigameZonesUtility {

        static public MinigameZoneStatus GetStatus(MinigameZonesState zones, MinigameId mg) {
            return zones.ZoneStatus[(int) mg];
        }

        static public void DisableAllZones(MinigameZonesState zones) {
            for (int i = 0; i < (int) MinigameId.COUNT; i++) {
                zones.ZoneStatus[i] = MinigameZoneStatus.Disabled;
            }
            zones.StatusDirty = true;
        }

        static public unsafe void UpdateStatusFromSave(MinigameZonesState zones, MinigameSaveStates saveStates, MinigameDependencyGraph dependencyGraph) {
            MinigameZoneStatus* statusBuffer = stackalloc MinigameZoneStatus[(int) MinigameId.COUNT];

            for (MinigameId id = 0; id < MinigameId.COUNT; id++) {
                MinigameSaveStateBase save = MinigameSaveUtility.GetState(saveStates, id);
                statusBuffer[(int) id] = save.FoundValidSolution ? MinigameZoneStatus.Completed : (save.Started ? MinigameZoneStatus.InProgress : MinigameZoneStatus.InProgress);
            }

            for (int i = 0; i < dependencyGraph.UnlockRules.Length; i++) {
                MinigameUnlockRule rule = dependencyGraph.UnlockRules[i];
                if (rule.Prerequisites == null || rule.Prerequisites.Length <= 0) {
                    continue;
                }

                bool locked = false;
                for (int p = 0; p < rule.Prerequisites.Length; p++) {
                    if (!MinigameSaveUtility.GetState(saveStates, rule.Prerequisites[p]).FoundValidSolution) {
                        locked = true;
                        break;
                    }
                }
                if (locked) {
                    statusBuffer[(int) rule.Minigame] = MinigameZoneStatus.Locked;
                }
            }

            for (int i = 0; i < (int) MinigameId.COUNT; i++) {
                zones.ZoneStatus[i] = statusBuffer[i];
            }

            zones.StatusDirty = true;
        }

        static public void UpdateAllZoneAppearances(MinigameZonesState zones) {
            foreach (var zone in Find.Components<MinigameZone>()) {
                MinigameZonesUtility.UpdateZoneStatus(zone, zones.ZoneStatus[(int) zone.Minigame]);
                MinigameZonesUtility.SetHoverState(zone, zone == zones.HoverZone);
            }
        }

        static public void AttemptStartGame(MinigameZone zone) {
            SpacefabGame.Events.Dispatch(GameEvents.StartMinigame, EvtArgs.Create(zone.Minigame));
            OverarchingTransitions.ToMinigame(zone);
        }
    }
}