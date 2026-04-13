using FieldDay.SharedState;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace SpaceFab
{
    public enum RequestState
    {
        None,
        Requested,
        Pending,
        Confirmed
    }

    /// <summary>
    /// Layer of indirection to allow for confirm of level exit
    /// </summary>
    public class MinigameRequestExitState : SharedStateComponent
    {
        public RequestState ExitRequestState;
    }
}