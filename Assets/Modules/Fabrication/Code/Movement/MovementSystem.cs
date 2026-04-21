using FieldDay;
using FieldDay.Systems;
using SpaceFab.Fabrication.Layout;
using SpaceFab.Fabrication.Robot;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace SpaceFab.Fabrication.Movement
{
    /// <summary>
    /// Manages robot movement
    /// Robot moves between Station slots (allows for station shuffling).
    /// </summary>
    [SysUpdate(FieldDay.GameLoopPhaseMask.Update, 0/*, UpdateMasks.PreAttemptMask | UpdateMasks.AttemptMask*/)]
    public class MovementSystem : SharedStateSystemBehaviour<MovementState, LayoutState>
    {
        #region Input Mappings

        private const KeyCode Left0 = KeyCode.A;
        private const KeyCode Left1 = KeyCode.LeftArrow;

        private const KeyCode Right0 = KeyCode.D;
        private const KeyCode Right1 = KeyCode.RightArrow;

        #endregion // Input Mappings

        [SerializeField] private RobotState m_RobotState;
        [SerializeField] private Transform cameraTransform;

        public override void ProcessWork(float deltaTime)
        {
            base.ProcessWork(deltaTime);

            // Update main camera
            Vector3 camPos = cameraTransform.position;
            Vector3 targetPos = m_RobotState.transform.position;
            cameraTransform.position = Vector3.Lerp(
                cameraTransform.position,
                new Vector3(targetPos.x, camPos.y, camPos.z),
                0.1f
            );

            if (!MovementUtility.CanMove(m_StateA, m_RobotState)) { return; }

            ProcessInputs();
        }

        private void ProcessInputs()
        {
            int curr = m_StateA.CurrSlotPosition;
            int max = m_StateB.StationSlots.Length - 1;

            if (Input.GetKeyDown(Left0) || Input.GetKeyDown(Left1))
            {
                if (curr > 0)
                    TryMove(curr - 1);
            }
            else if (Input.GetKeyDown(Right0) || Input.GetKeyDown(Right1))
            {
                if (curr < max)
                    TryMove(curr + 1);
            }
        }

        private void TryMove(int targetIndex)
        {
            if (!MovementUtility.CanMove(m_StateA, m_RobotState))
                return;

            m_StateA.CurrSlotPosition = MovementState.TRAVELING;
            StartCoroutine(MoveRoutine(targetIndex));
        }

        private IEnumerator MoveRoutine(int targetIndex)
        {
            Vector3 startPos = m_RobotState.transform.position;
            Vector3 targetPos = m_StateB.StationSlots[targetIndex].transform.position;

            float duration = 0.25f;
            float time = 0f;

            while (time < duration)
            {
                time += Time.deltaTime;
                m_RobotState.transform.position = Vector3.Lerp(startPos, targetPos, time / duration);
                yield return null;
            }

            m_RobotState.transform.position = targetPos;
            m_StateA.CurrSlotPosition = targetIndex;
        }
    }
}