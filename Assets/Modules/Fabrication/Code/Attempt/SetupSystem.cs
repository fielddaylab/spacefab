using FieldDay.Systems;
using SpaceFab.Fabrication.Layout;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace SpaceFab.Fabrication
{
    /// <summary>
    /// Sets up data for a new attempt
    /// </summary>
    [SysUpdate(FieldDay.GameLoopPhaseMask.PreUpdate, 0, UpdateMasks.SetupMask)]
    public class SetupSystem : SharedStateSystemBehaviour<WaferState, LayoutState>
    {
        protected override unsafe SystemFunctionShim GetDelegate() {
            return new SystemFunctionShim(&ProcessWork);
        }

        static private void ProcessWork(float deltaTime)
        {
            GetDependencies();

            if (m_StateB.NeedsReshuffling)
            {
                LayoutUtility.ShuffleStations(m_StateB);
                m_StateB.NeedsReshuffling = false;
            }
        }
    }
}