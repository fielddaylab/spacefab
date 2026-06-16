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
using FieldDay.Music;
using FieldDay.Scenes;

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

            Log.Msg("[SpacefabGame] Creating Music player...");
            MusicPlayer.Initialize();
            MusicPlayer.SetDefaultTransition(new MusicTransitionParams() {
                FadeIn = 0.4f,
                FadeOut = 0.4f,
                Overlap = 0.2f
            });
            MusicPlayer.ConfigureSceneUnloadBehavior(true, "PreserveMusic");

            Game.Scenes.RegisterLoadDependency(new WaitForSetupMasksToBeSuspended());
        }

        [InvokeOnBoot]
        static private void OnBoot()
        {
        }

        private class WaitForSetupMasksToBeSuspended : ISceneLoadDependency
        {
            bool ISceneLoadDependency.IsLoaded(SceneLoadFence loadPhase) {
                if (loadPhase == SceneLoadFence.BeforeReady)
                    return GameLoop.IsSuspended(UpdateMasks.SetupMask) || GameLoop.IsSuspended(UpdateMasks.OverarchingMask);
                return true;
            }
        }
    }
}