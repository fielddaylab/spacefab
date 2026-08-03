using BeauUtil;
using FieldDay;
using FieldDay.Scenes;
using FieldDay.SharedState;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace SpaceFab.Overarching
{
    public class OverarchingCamera : SharedStateComponent, IScenePreload {
        public Camera Camera;
        public Transform Root;

        public OverarchingRenderPose DefaultPose;

        [NonSerialized] public OverarchingRenderPose CurrentPose;

        IEnumerator<WorkSlicer.Result?> IScenePreload.Preload() {
            OverarchingRenderUtility.SwitchPose(this, DefaultPose);
            return null;
        }
    }
}