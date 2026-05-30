using BeauUtil.Debugger;
using FieldDay;
using FieldDay.Scripting;
using FieldDay.Systems;
using Leaf.Runtime;
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
                new SysUpdate(GameLoopPhaseMask.Update, -10, UpdateMasks.PreAttemptMask),
                new SysPermissions()
                    .ReadWriteShared<LayoutState>()
                    .ReadWriteShared<ModeState>()
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

            layoutState.NeedsReshuffling = true;

            // setup pre-attempt
            if (layoutState.NeedsReshuffling) {
                Log.Msg("[PreAttemptSetupSystem] shuffling stations");

                LayoutUtility.ShuffleStations(layoutState);
                layoutState.NeedsReshuffling = false;
            }

            // trigger leaf dialogue on load
            ScriptUtility.Trigger(FabricationScriptTriggers.OnPreAttempt);
        }
    }
}
