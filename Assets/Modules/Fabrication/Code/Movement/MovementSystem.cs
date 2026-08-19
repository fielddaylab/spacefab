using FieldDay;
using FieldDay.Systems;
using SpaceFab.Fabrication.Layout;
using SpaceFab.Fabrication.Robot;
using SpaceFab.Fabrication.StationControl;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace SpaceFab.Fabrication.Movement {
    /// <summary>
    /// Manages robot movement between station slots (allowing for station shuffling), and
    /// pulls the main camera along with the robot. Runs on any Update phase at order 0.
    /// </summary>
    public class MovementSystem : SystemComponent {


        public override unsafe void RegisterSystems(ref SystemRegistrationTable ecs) {
            ecs.Register(&ProcessWork,
                new SysUpdate(GameLoopPhaseMask.Update, 0, UpdateMasks.PreAttemptMask | UpdateMasks.AttemptMask | UpdateMasks.PostAttemptMask),
                new SysPermissions()
                    .ReadWriteShared<MovementState>()
                    .ReadWriteShared<LayoutState>()
                    .ReadWriteShared<RobotState>()
                    .ReadShared<StationControlState>()
            );
        }

        // Smooth-follows the camera toward the robot, then — if movement is allowed — reads input and dispatches a slot move.
        static private void ProcessWork(float deltaTime) {
            Find.State(
                out MovementState movementState,
                out OnboardingLayoutState onboardState,
                out RobotState robotState,
                out StationControlState stationState
                );
            Find.State(
                out LayoutState layoutState
                );

            var cameraTransform = Game.Rendering.PrimaryCamera.transform;

            // Update main camera
            Vector3 camPos = cameraTransform.position;
            Vector3 targetPos = robotState.transform.position;
            cameraTransform.position = Vector3.Lerp(
                cameraTransform.position,
                new Vector3(targetPos.x, camPos.y, camPos.z),
                0.1f
            );

            if (!MovementUtility.CanMove(movementState, robotState, stationState)) { return; }

            ProcessInputs(movementState, onboardState, layoutState, robotState, stationState);
        }

        // Reads left/right input and attempts a slot move in the chosen direction, clamped to the station-slot range.
        static private void ProcessInputs(MovementState movementState, OnboardingLayoutState onboardState, LayoutState layoutState, RobotState robotState, StationControlState stationState) {
            int curr = movementState.CurrSlotPosition;
            int max = layoutState.StationSlots.Length - 1;

            if (Input.GetKeyDown(FabricationConsts.Left0) || Input.GetKeyDown(FabricationConsts.Left1) || onboardState.IsLeftArrowPressed) {
                onboardState.IsLeftArrowPressed = false;
                if (curr > 0)
                    TryMove(movementState, layoutState, robotState, stationState, curr - 1);
            }
            else if (Input.GetKeyDown(FabricationConsts.Right0) || Input.GetKeyDown(FabricationConsts.Right1) || onboardState.IsRightArrowPressed) {
                onboardState.IsRightArrowPressed = false;
                if (curr < max)
                    TryMove(movementState, layoutState, robotState, stationState, curr + 1);
            }
        }

        // Starts a move routine to the target slot if movement is allowed; marks the robot as traveling until the routine completes.
        private static void TryMove(MovementState movementState, LayoutState layoutState, RobotState robotState, StationControlState stationState, int targetIndex) {
            if (!MovementUtility.CanMove(movementState, robotState, stationState))
                return;

            movementState.CurrSlotPosition = MovementState.TRAVELING;
            movementState.SlotChangedThisFrame = true;
            movementState.MoveRoutine.Replace(MoveRoutine(movementState, layoutState, robotState, targetIndex));
        }

        // Lerps the robot's transform from its current position to the target slot's position over 0.25s, then snaps and records the new slot index.
        private static IEnumerator MoveRoutine(MovementState movementState, LayoutState layoutState, RobotState robotState, int targetIndex) {
            Vector3 startPos = robotState.transform.position;
            Vector3 targetPos = layoutState.StationSlots[targetIndex].transform.position;

            float duration = 0.25f;
            float time = 0f;

            while (time < duration) {
                time += Time.deltaTime;
                robotState.transform.position = Vector3.Lerp(startPos, targetPos, time / duration);
                yield return null;
            }

            robotState.transform.position = targetPos;
            movementState.CurrSlotPosition = targetIndex;
            movementState.SlotChangedThisFrame = true;
        }
    }
}
