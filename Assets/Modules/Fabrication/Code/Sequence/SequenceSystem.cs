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
    /// Drives the sequence state machine in response to station-control one-frame flags.
    /// On microgame completion at a non-Defrag station, checks station id + wafer snapshot against
    /// the current step's expected values and either advances or flags misalignment. On microgame
    /// completion at the Defrag station, clears the current step's glitch flag (without advancing).
    /// Runs on Update at order 15 under AttemptMask (after StationControlSystem at 5 and
    /// WorldInteractSystem at 10).
    /// </summary>
    public class SequenceSystem : SystemComponent
    {
        public override unsafe void RegisterSystems(ref SystemRegistrationTable ecs)
        {
            ecs.Register(&ProcessWork,
                new SysUpdate(GameLoopPhase.Update, 15, UpdateMasks.AttemptMask),
                new SysPermissions()
                    .ReadWriteShared<SequenceState>()
                    .ReadWriteShared<SequenceVisualsState>()
                    .ReadShared<StationControlState>()
                    .ReadShared<MovementState>()
                    .ReadShared<LayoutState>()
                    .ReadShared<WaferState>()
                    .ReadShared<TimeState>()
            );
        }

        // Processes this frame's microgame-completion event (if any) against the sequence.
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
                out TimeState timeState,
                out SequenceVisualsState visualsState
                );

            if (sequenceState.Status != SequenceStatus.Active && sequenceState.Status != SequenceStatus.Restoring) {
                return;
            }

            HandleMicrogameCompleted(sequenceState, stationState, movementState, layoutState, waferState, timeState, visualsState);
        }

        // On microgame completion:
        //   - If the completed station was the Defrag station, unglitch the current step (does NOT advance).
        //   - Otherwise verify the station matched the current step AND the wafer matches the expected
        //     snapshot. Advance on success, flag misalignment on failure.
        // A microgame that is cancelled (rather than completed) does NOT trigger either path, because
        // StationControlState.MicrogameCompletedThisFrame is only set on a normal completion.
        static private void HandleMicrogameCompleted(SequenceState sequenceState, StationControlState stationState, MovementState movementState, LayoutState layoutState, WaferState waferState, TimeState timeState, SequenceVisualsState visualsState)
        {
            // TODO:
            //   if (!stationState.MicrogameCompletedThisFrame) return
            //   if (sequenceState.Status != SequenceStatus.Active) return   // can't complete a step during Restoring
            //   MicrogameStationInterfacer interfacer = layoutState.StationSlots[movementState.CurrSlotPosition].AssignedStationInterfacer
            //   if (interfacer == null) return
            //
            //   // Defrag branch: completion at the Defrag station unglitches without advancing.
            //   if (SequenceUtility.IsDefragStation(interfacer.Id)) {
            //       SequenceUtility.UnglitchCurrentStep(sequenceState)
            //       return
            //   }
            //
            //   // Non-Defrag branch: check station + wafer against the current step.
            //   FabricationStep? step = SequenceUtility.GetCurrentStep(sequenceState)
            //   if (step == null) return
            //   SerializedHash32 expectedStation = Find.GlobalAsset<SequenceLookup>().GetStationForStep(step.Value.StepId)
            //   if (interfacer.Id != expectedStation) {
            //       SequenceUtility.FlagMisalignment(sequenceState)
            //       return
            //   }
            //   if (!WaferStateUtility.MatchesSnapshot(waferState, step.Value.ExpectedWaferAfter)) {
            //       SequenceUtility.FlagMisalignment(sequenceState)
            //       return
            //   }
            //   SequenceUtility.AdvanceStep(sequenceState, waferState, timeState, movementState, visualsState)
        }
    }
}
