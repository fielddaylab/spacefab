using BeauUtil;
using BeauUtil.Debugger;
using FieldDay;
using FieldDay.Scenes;
using FieldDay.Scripting;
using FieldDay.SharedState;
using Leaf;
using Leaf.Runtime;
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

    static public class OverarchingCameraScripting {
        [LeafMember("OverarchingSwapCamera")]
        static private void Leaf_SwapCameraPose(StringHash32 poseId) {
            ScriptActor actor = ScriptUtility.FindActor(poseId);
            Assert.NotNullOrDestroyed(actor, "No actor with id '{0}'", poseId);
            actor.TryGetComponent(out OverarchingRenderPose pose);
            Assert.NotNullOrDestroyed(pose, "Actor '{0}' is not a valid camera pose", poseId);
            OverarchingRenderUtility.SwitchPose(Find.State<OverarchingCamera>(), pose);
        }
    }
}