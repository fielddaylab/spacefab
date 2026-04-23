using FieldDay;
using FieldDay.Systems;
using SpaceFab.Fabrication.Layout;
using SpaceFab.Fabrication.Stations;
using SpaceFab.Fabrication.StationControl;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace SpaceFab.Fabrication.Movement {
    /// <summary>
    /// Routes world-interact input (Activate, Cancel) to the station-control state machine. Up/Activate
    /// enters a microgame at the robot's current station; Down/Cancel exits a microgame mid-play.
    /// Gated by WorldInteractState.WorldInteractEnabled (outer kill switch) and the station-control
    /// machine's AllowsActivate / AllowsCancel queries.
    /// Runs on any Update phase at order 10 under AttemptMask (after StationControlSystem at order 5).
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
                new SysUpdate(GameLoopPhaseMask.Update, 10, UpdateMasks.AttemptMask),
                new SysPermissions()
                    .ReadShared<WorldInteractState>()
                    .ReadShared<MovementState>()
                    .ReadShared<LayoutState>()
                    .ReadWriteShared<StationControlState>()
            );
        }

        // Reads world-interact keys and forwards Activate/Cancel to the station-control machine when allowed.
        static private void ProcessWork(float deltaTime) {
            Find.State(
                out WorldInteractState interactState,
                out MovementState movementState,
                out LayoutState layoutState,
                out StationControlState stationState
                );

            if (!interactState.WorldInteractEnabled) { return; }

            ProcessInputs(interactState, movementState, layoutState, stationState);
        }

        // Dispatches Activate (Up / Space) and Cancel (Down) keypresses. Validity is checked via
        // WorldInteractUtility against the station-control state; the machine itself makes the final decision.
        static private void ProcessInputs(WorldInteractState interactState, MovementState movementState, LayoutState layoutState, StationControlState stationState) {
            if (Game.Input.IsKeyPressed(Up0) || Game.Input.IsKeyPressed(Up1) || Game.Input.IsKeyPressed(Activate)) {
                HandleActivate(interactState, movementState, layoutState, stationState);
            }
            else if (Game.Input.IsKeyPressed(Down0) || Game.Input.IsKeyPressed(Down1)) {
                HandleCancel(interactState, stationState);
            }
        }

        // Looks up the interfacer at the current slot and asks the station-control machine to activate it.
        // No-op if the gate fails, the slot is invalid, or no interfacer is assigned.
        static private void HandleActivate(WorldInteractState interactState, MovementState movementState, LayoutState layoutState, StationControlState stationState) {
            if (!WorldInteractUtility.CanActivate(interactState, stationState)) { return; }

            int slotIndex = movementState.CurrSlotPosition;
            if (slotIndex < 0 || slotIndex >= layoutState.StationSlots.Length) { return; }

            MicrogameStationInterfacer interfacer = layoutState.StationSlots[slotIndex].AssignedStationInterfacer;
            StationControlUtility.RequestActivate(stationState, interfacer);
        }

        // Forwards a Cancel request to the station-control machine. Honored only during InMicrogame.
        static private void HandleCancel(WorldInteractState interactState, StationControlState stationState) {
            if (!WorldInteractUtility.CanCancel(interactState, stationState)) { return; }

            StationControlUtility.RequestCancel(stationState);
        }
    }
}
