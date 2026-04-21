using FieldDay;
using FieldDay.Systems;
using SpaceFab.Fabrication.Layout;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace SpaceFab.Fabrication.Movement
{
    /// <summary>
    /// Manages robot movement
    /// Robot moves between Station slots (allows for station shuffling).
    /// </summary>
    [SysUpdate(FieldDay.GameLoopPhaseMask.Update, 0, UpdateMasks.PreAttemptMask | UpdateMasks.AttemptMask)]
    public class MovementSystem : SharedStateSystemBehaviour<MovementState, LayoutState>
    {
        #region Input Mappings

        private const KeyCode Left0 = KeyCode.A;
        private const KeyCode Left1 = KeyCode.LeftArrow;

        private const KeyCode Right0 = KeyCode.D;
        private const KeyCode Right1 = KeyCode.RightArrow;

        #endregion // Input Mappings

        static private void ProcessWork(float deltaTime)
        {
            GetDependencies();

            if (!m_StateA.MoveEnabled) { return; }

            ProcessInputs();
        }

        protected override unsafe SystemFunctionShim GetDelegate() {
            return new SystemFunctionShim(&ProcessWork);
        }

        static private void ProcessInputs()
        {
            if (Input.GetKeyDown(Left0) || Input.GetKeyDown(Left1))
            {
                // move left
            }
            else if (Input.GetKeyDown(Right0) || Input.GetKeyDown(Right1))
            {
                // move right
            }
        }
    }
}