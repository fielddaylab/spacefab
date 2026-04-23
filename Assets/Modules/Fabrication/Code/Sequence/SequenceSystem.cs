using FieldDay;
using FieldDay.Systems;
using SpaceFab.Fabrication.Layout;
using SpaceFab.Fabrication.Movement;
using SpaceFab.Fabrication.StationControl;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace SpaceFab.Fabrication.Sequence
{
    /// <summary>
    /// Drives the sequence state machine in response to station-control + movement one-frame flags.
    /// On microgame completion, checks station id + wafer snapshot against the current step's
    /// expected values and either advances or flags misalignment. On Defrag-station arrival, clears
    /// the current step's glitch flag. Runs on Update at order 15 under AttemptMask (after
    /// StationControlSystem at 5 and WorldInteractSystem at 10).
    /// </summary>
    public class SequenceSystem : SystemComponent
    {
        public override unsafe void RegisterSystems(ref SystemRegistrationTable ecs)
        {
            ecs.Register(&ProcessWork,
                new SysUpdate(GameLoopPhase.Update, 15, UpdateMasks.AttemptMask),
                new SysPermissions()
                    .ReadWriteShared<SequenceState>()
                    .ReadShared<StationControlState>()
                    .ReadShared<MovementState>()
                    .ReadShared<LayoutState>()
                    .ReadShared<WaferState>()
                    .ReadShared<TimeState>()
            );
        }

        // Handles defrag-station arrival (unglitch) and microgame-completed alignment checks.
        // Early-outs if the sequence isn't accepting progression this frame.
        static private void ProcessWork(float deltaTime)
        {
            Find.State(
                out SequenceState sequenceState,
                out StationControlState stationState,
                out MovementState movementState,
                out LayoutState layoutState
                );
            Find.State(
                out WaferState waferState,
                out TimeState timeState
                );

            if (sequenceState.Status != SequenceStatus.Active && sequenceState.Status != SequenceStatus.Restoring) {
                return;
            }

            HandleDefragArrival(sequenceState, movementState, layoutState);
            HandleMicrogameCompleted(sequenceState, stationState, movementState, layoutState, waferState, timeState);
        }

        // If the robot just arrived (not departed) at the Defrag station, unglitch the current step.
        // Does NOT advance the sequence or run any alignment check.
        static private void HandleDefragArrival(SequenceState sequenceState, MovementState movementState, LayoutState layoutState)
        {
            // TODO:
            //   if (!movementState.SlotChangedThisFrame) return
            //   if (MovementUtility.IsTraveling(movementState)) return   // departure, not arrival
            //   StationSlot slot = layoutState.StationSlots[movementState.CurrSlotPosition]
            //   if (slot.AssignedStationInterfacer == null) return
            //   if (SequenceUtility.IsDefragStation(slot.AssignedStationInterfacer.Id))
            //       SequenceUtility.UnglitchCurrentStep(sequenceState)
        }

        // On microgame completion: verify the station was the expected one for the current step
        // AND the wafer matches the expected snapshot. Advance or flag misalignment accordingly.
        // Defrag completions are ignored (Defrag never consumes a step).
        static private void HandleMicrogameCompleted(SequenceState sequenceState, StationControlState stationState, MovementState movementState, LayoutState layoutState, WaferState waferState, TimeState timeState)
        {
            // TODO:
            //   if (!stationState.MicrogameCompletedThisFrame) return
            //   if (sequenceState.Status != SequenceStatus.Active) return   // can't complete a step during Restoring
            //   FabricationStep? step = SequenceUtility.GetCurrentStep(sequenceState)
            //   if (step == null) return
            //   MicrogameStationInterfacer interfacer = layoutState.StationSlots[movementState.CurrSlotPosition].AssignedStationInterfacer
            //   if (interfacer == null) return
            //   if (SequenceUtility.IsDefragStation(interfacer.Id)) return
            //   SerializedHash32 expectedStation = Find.GlobalAsset<SequenceLookup>().GetStationForStep(step.Value.StepId)
            //   if (interfacer.Id != expectedStation) {
            //       SequenceUtility.FlagMisalignment(sequenceState)
            //       return
            //   }
            //   if (!WaferStateUtility.MatchesSnapshot(waferState, step.Value.ExpectedWaferAfter)) {
            //       SequenceUtility.FlagMisalignment(sequenceState)
            //       return
            //   }
            //   SequenceUtility.AdvanceStep(sequenceState, waferState, timeState, movementState)
        }
    }
}
