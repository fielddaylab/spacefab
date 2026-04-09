using FieldDay;
using FieldDay.Systems;
using SpaceFab.Fabrication.Layout;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace SpaceFab.Fabrication.Movement
{
    /// <summary>
    /// Manages world (non-microgame) interactions and inputs
    /// </summary>
    [SysUpdate(FieldDay.GameLoopPhaseMask.Update, 0, UpdateMasks.AttemptMask)]
    public class WorldInteractSystem : SharedStateSystemBehaviour<WorldInteractState, LayoutState>
    {
        #region Input Mappings

        private const KeyCode Up0 = KeyCode.W;
        private const KeyCode Up1 = KeyCode.UpArrow;

        private const KeyCode Down0 = KeyCode.S;
        private const KeyCode Down1 = KeyCode.DownArrow;

        private const KeyCode Activate = KeyCode.Space;

        #endregion // Input Mappings

        public override void ProcessWork(float deltaTime)
        {
            base.ProcessWork(deltaTime);

            if (!m_StateA.WorldInteractEnabled) { return; }

            ProcessInputs();
        }

        private void ProcessInputs()
        {

            if (Input.GetKeyDown(Up0) || Input.GetKeyDown(Up1) || Input.GetKeyDown(Activate))
            {
                // activate
            }
            else if (Input.GetKeyDown(Down0) || Input.GetKeyDown(Down1))
            {
                // cancel / close results
            }
        }
    }
}