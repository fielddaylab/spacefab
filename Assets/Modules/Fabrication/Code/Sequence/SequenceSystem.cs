using BeauUtil;
using BeauUtil.Debugger;
using FieldDay;
using FieldDay.Systems;
using SpaceFab.Fabrication.Layout;
using SpaceFab.Fabrication.Movement;
using SpaceFab.Fabrication.StationControl;
using SpaceFab.Fabrication.Stations;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
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
                    .ReadWriteShared<CompletionRecapState>()
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
                out SequenceVisualsState visualsState,
                out CompletionRecapState recapState
                );

            if (sequenceState.Status != SequenceStatus.Active && sequenceState.Status != SequenceStatus.Restoring) {
                return;
            }

            HandleMicrogameCompleted(sequenceState, stationState, movementState, layoutState, waferState, timeState, visualsState, recapState);
        }

        // On a microgame completion that passed the precision gate (MicrogamePassedThisFrame):
        //   - If the completed station was the Defrag station, unglitch the current step (does NOT advance).
        //   - Otherwise verify the station matched the current step AND the wafer matches the expected
        //     snapshot. Advance on success, flag misalignment on failure.
        // A cancelled microgame, and a completion that failed the precision gate (paused for retry), do
        // NOT trigger either path: MicrogamePassedThisFrame is raised only when the station-control machine
        // accepts a completion and commits to exiting.
        static private void HandleMicrogameCompleted(SequenceState sequenceState, StationControlState stationState, MovementState movementState, LayoutState layoutState, WaferState waferState, TimeState timeState, SequenceVisualsState visualsState, CompletionRecapState recapState)
        {
            if (!stationState.MicrogamePassedThisFrame) {
                return;
            }
            // The outer ProcessWork early-out allows Restoring through (so this code path runs while
            // the restore coroutine is pacing the lead-in). The completion check itself, however,
            // can only validly land during Active — a microgame can't be "completed" mid-restore.
            if (sequenceState.Status != SequenceStatus.Active) {
                return;
            }

            // 1. Resolve which station's microgame just finished from the robot's current slot.
            if (movementState.CurrSlotPosition < 0 || layoutState.StationSlots == null || movementState.CurrSlotPosition >= layoutState.StationSlots.Length) {
                return;
            }
            MicrogameStationInterfacer interfacer = layoutState.StationSlots[movementState.CurrSlotPosition].AssignedStationInterfacer;
            if (interfacer == null) {
                return;
            }

            // 2. Defrag branch: completion at the universal Defrag station unglitches the current
            //    step without advancing the sequence pointer.
            if (SequenceUtility.IsDefragStation(interfacer.Id)) {
                SequenceUtility.UnglitchCurrentStep(sequenceState);
                return;
            }

            // 3. Non-Defrag branch: verify the activated station matches the current step.
            FabricationStep? step = SequenceUtility.GetCurrentStep(sequenceState);
            if (step == null) {
                return;
            }
            SequenceLookup lookup = Find.GlobalAsset<SequenceLookup>();
            StringHash32 expectedStation = lookup.GetStationForStep(step.Value.StepId);
            StringHash32 actualStation = interfacer.Id;
            if (actualStation != expectedStation) {
                SequenceUtility.FlagMisalignment(sequenceState);
                Log.Msg($"Misalignment! Expected: {expectedStation.ToDebugString()}, got {actualStation.ToDebugString()}");
                return;
            }

            // 4. Verify the wafer ended in the step's expected postcondition snapshot. While the
            //    wafer model is scaffold-only, MatchesSnapshot returns true by default so the
            //    pipeline runs end-to-end.
            if (!WaferStateUtility.MatchesSnapshot(waferState, step.Value.ExpectedWaferAfter)) {
                SequenceUtility.FlagMisalignment(sequenceState);
                return;
            }

            // 5. Advance — increments CurrentStepIndex, captures a checkpoint if this step is one,
            //    and arms the recap layer. The recap system fires the top-panel swap when its
            //    routine finishes.
            SequenceUtility.AdvanceStep(sequenceState, waferState, timeState, movementState, visualsState, recapState);
        }
    }
}
