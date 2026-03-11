using BeauUtil;
using BeauUtil.UI;
using FieldDay;
using FieldDay.Components;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace SpaceFab.Overarching
{
    public class MinigameZone : BatchedComponent
    {
        public PointerListener PointerListener;
        public SceneReference MinigameScene;

        private void Start()
        {
            // TEMP
            PointerListener.onClick.AddListener(() => {
                GameLoop.SuspendUpdates(Bits.All32);
                GameLoop.ResumeUpdates(UpdateMasks.MinigameTransitionMask);
                Game.Scenes.LoadMainScene(MinigameScene);
                Game.Events.Dispatch(GameEvents.OnMinigameLoad);
            });
            PointerListener.onPointerEnter.AddListener(() => { Debug.Log("enter"); });
        }
    }
}