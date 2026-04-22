using BeauUtil.Debugger;
using FieldDay;
using FieldDay.Systems;
using SpaceFab.Design.Visuals;
using SpaceFab.Save;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace SpaceFab.Design {
    /// <summary>
    /// Drives the Design minigame's grid-setup sequence during the SetupMask phase.
    /// Walks DesignTransitionState.Phase through SetupBaseLevel → ApplySave → FinalizeLevel → SetupComplete,
    /// initializing the visual grid and flagging the visuals for refresh. Suspends SetupMask when done.
    /// </summary>
    public class GridStackLoadSystem : SystemComponent {
        public override unsafe void RegisterSystems(ref SystemRegistrationTable ecs) {
            ecs.Register(&ProcessWork,
                new SysUpdate(GameLoopPhase.PreUpdate, 0, UpdateMasks.SetupMask),
                new SysPermissions()
                    .ReadWriteShared<DesignTransitionState>()
                    .ReadShared<GridStackState>()
                    .ReadWriteShared<VisualGridStackState>()
            );
        }

        // Advances the setup phase state machine one step per tick until setup is complete.
        static private void ProcessWork(float deltaTime) {
            Find.State(
                out DesignTransitionState transitionState,
                out GridStackState gridStackState,
                out VisualGridStackState visualState
                );

            switch (transitionState.Phase) {
                case DesignTransitionPhase.SetupBaseLevel:
                    Debug.Log("[GridStackLoadSystem] Setting up base level...");
                    // TODO: load base level
                    // Build the visual grid to match the logical grid's dimensions
                    VisualGridStackUtility.Init(ref visualState.VisualGridStack, gridStackState.GridStack.LayerDims.X, gridStackState.GridStack.LayerDims.Y, visualState.CellVisualsPrefab, visualState.CellVisualsContainer);
                    VisualGridStackUtility.RefreshGridSize(visualState.GridRenderer, gridStackState.GridStack.LayerDims.X, gridStackState.GridStack.LayerDims.Y);
                    transitionState.Phase = DesignTransitionPhase.ApplySave;
                    break;
                case DesignTransitionPhase.ApplySave:
                    Debug.Log("[GridStackLoadSystem] Applying save to level...");
                    // TODO: apply save
                    transitionState.Phase = DesignTransitionPhase.FinalizeLevel;
                    break;
                case DesignTransitionPhase.FinalizeLevel:
                    Debug.Log("[GridStackLoadSystem] Finalizing level...");
                    // TODO: finalize level (enforce eraseable)
                    // Queue a visuals refresh so the newly-populated grid renders on the next frame
                    visualState.VisualsNeedRefreshing = true;
                    transitionState.Phase = DesignTransitionPhase.SetupComplete;
                    break;
                case DesignTransitionPhase.SetupComplete:
                    Debug.Log("[GridStackLoadSystem] Load Complete!");
                    // Setup is finished; stop running this system until the next minigame load
                    GameLoop.SuspendUpdates(UpdateMasks.SetupMask);
                    break;
                default:
                    break;
            }
        }
    }
}
