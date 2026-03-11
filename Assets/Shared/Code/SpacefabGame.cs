using System.Collections;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using BeauUtil.Debugger;
using FieldDay;
using FieldDay.Processes;
using FieldDay.Systems;
using SpaceFab.Save;
using SpaceFab;
using UnityEngine;

namespace SpaceFab
{
    public sealed class SpacefabGame : Game
    {
        static public new EventDispatcher<EvtArgs> Events { get; private set; }
        static public TransitionStateMgr TransitionState { [MethodImpl(MethodImplOptions.AggressiveInlining)] get; internal set; }
        static public SaveMgr SaveBuffer { get; private set; }

        [InvokePreBoot]
        static private void OnPreBoot()
        {
            Events = new EventDispatcher<EvtArgs>();
            SetEventDispatcher(Events);

            Log.Msg("[SpacefabGame] Creating TransitionState manager...");
            TransitionState = new TransitionStateMgr();

            Log.Msg("[SpacefabGame] Creating Save manager...");
            SaveBuffer = new SaveMgr();
        }

        [InvokeOnBoot]
        static private void OnBoot()
        {
        }
    }
}