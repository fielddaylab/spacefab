using BeauUtil.Debugger;
using FieldDay;
using FieldDay.Systems;
using SpaceFab.Fabrication.Layout;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace SpaceFab.Fabrication {
    /// <summary>
    /// Sets up data for a new fabrication attempt. Reshuffles stations when the layout requests it.
    /// Runs in PreUpdate at order 0 under SetupMask.
    /// </summary>
    public class PreAttemptSetupSystem : SystemComponent {
        public override unsafe void RegisterSystems(ref SystemRegistrationTable ecs) {
            ecs.Register(&ProcessWork,
                new SysUpdate(GameLoopPhaseMask.PreUpdate, 0, UpdateMasks.PreAttemptMask),
                new SysPermissions()
                    .ReadWriteShared<WaferState>()
                    .ReadWriteShared<LayoutState>()
            );
        }

        // When the layout is flagged as needing a reshuffle, shuffle stations and clear the flag.
        static private void ProcessWork(float deltaTime) {
            LayoutState layoutState = Find.State<LayoutState>();
            ModeState modeState = Find.State<ModeState>();

            if (!modeState.ChangedModeThisFrame || modeState.CurrMode != LevelMode.PreAttempt)
            {
                return;
            }

            // setup pre-attempt

            if (layoutState.NeedsReshuffling) {
                Log.Msg("[PreAttemptSetupSystem] shuffling stations");

                LayoutUtility.ShuffleStations(layoutState);
                layoutState.NeedsReshuffling = false;
            }
        }
    }
}
