using BeauUtil;
using FieldDay;
using FieldDay.SharedState;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace SpaceFab.Overarching
{
    public class OverarchingState : SharedStateComponent, IRegistrationCallbacks
    {
        [NonSerialized] public int DefaultUpdateMask;

        #region Interfaces

        // IRegistrationCallbacks

        public void OnDeregister()
        {
        }

        public void OnRegister()
        {
            DefaultUpdateMask = UpdateMasks.OverarchingMask;
            GameLoop.SuspendUpdates(Bits.All32);
            GameLoop.ResumeUpdates(DefaultUpdateMask);
        }

        #endregion // Interfaces
    }
}