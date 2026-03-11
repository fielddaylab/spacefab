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
            PointerListener.onClick.AddListener(() => { Game.Scenes.LoadMainScene(MinigameScene); });
            PointerListener.onPointerEnter.AddListener(() => { Debug.Log("enter"); });
        }
    }
}