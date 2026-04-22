using FieldDay;
using FieldDay.Systems;
using SpaceFab.Fabrication.Layout;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace SpaceFab.Fabrication.Movement {
    /// <summary>
    /// Manages world (non-microgame) interactions and inputs during a fabrication attempt.
    /// Runs on any Update phase at order 1 under AttemptMask; gated by WorldInteractState.WorldInteractEnabled.
    /// </summary>
    public class WorldInteractSystem : SystemComponent {
        #region Input Mappings

        private const KeyCode Up0 = KeyCode.W;
        private const KeyCode Up1 = KeyCode.UpArrow;

        private const KeyCode Down0 = KeyCode.S;
        private const KeyCode Down1 = KeyCode.DownArrow;

        private const KeyCode Activate = KeyCode.Space;

        #endregion // Input Mappings

        public override unsafe void RegisterSystems(ref SystemRegistrationTable ecs) {
            ecs.Register(&ProcessWork,
                new SysUpdate(GameLoopPhaseMask.Update, 1, UpdateMasks.AttemptMask),
                new SysPermissions()
                    .ReadShared<WorldInteractState>()
                    .ReadWriteShared<LayoutState>()
            );
        }

        // Reads the world-interact gate and routes keyboard input to activate/cancel handlers.
        static private void ProcessWork(float deltaTime) {
            WorldInteractState interactState = Find.State<WorldInteractState>();

            if (!interactState.WorldInteractEnabled) { return; }

            ProcessInputs();
        }

        // Dispatches up/down/activate keypresses. Placeholder — action branches are still stubs.
        static private void ProcessInputs() {
            if (Input.GetKeyDown(Up0) || Input.GetKeyDown(Up1) || Input.GetKeyDown(Activate)) {
                // activate
            }
            else if (Input.GetKeyDown(Down0) || Input.GetKeyDown(Down1)) {
                // cancel / close results
            }
        }
    }
}
