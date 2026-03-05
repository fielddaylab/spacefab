using System.Collections;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using BeauUtil.Debugger;
using FieldDay;
using FieldDay.Processes;
using FieldDay.Systems;
using UnityEngine;

namespace Spacefab.Shared
{
    public sealed class SpacefabGame : Game
    {
        static public new EventDispatcher<EvtArgs> Events { get; private set; }
        static public TransitionStateMgr TransitionState { [MethodImpl(MethodImplOptions.AggressiveInlining)] get; internal set; }


        [InvokePreBoot]
        static private void OnPreBoot()
        {
            Events = new EventDispatcher<EvtArgs>();
            SetEventDispatcher(Events);

            Log.Msg("[SpacefabGame] Creating TransitionState manager...");
            TransitionState = new TransitionStateMgr();
        }

        [InvokeOnBoot]
        static private void OnBoot()
        {
        }
    }
}