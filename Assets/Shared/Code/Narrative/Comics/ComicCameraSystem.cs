using BeauRoutine;
using BeauUtil.Debugger;
using FieldDay;
using FieldDay.Collections;
using FieldDay.SharedState;
using FieldDay.Systems;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using UnityEngine;

namespace SpaceFab.Comic
{
    public sealed class ComicCameraSystem : SystemComponent
    {
        public override unsafe void RegisterSystems(ref SystemRegistrationTable ecs) {
            ecs.Register(&CalculateCameraColorAnimation,
                new SysUpdate(GameLoopPhase.UnscaledLateUpdate, 999),
                new SysPermissions()
                    .ReadWriteShared<ComicCameraState>()
            );

            ecs.Register(&ApplyCameraColor,
                new SysUpdate(GameLoopPhase.UnscaledLateUpdate, 1000),
                new SysPermissions()
                    .ReadShared<ComicCameraState>()
            );
        }

        static private void CalculateCameraColorAnimation(float dt) {
            Find.State(out ComicCameraState cameraState);
            // TODO: apply animation
            cameraState.FinalColor = cameraState.BaseColor;
        }

        static private void ApplyCameraColor(float dt) {
            Find.State(out ComicCameraState cameraState);
            cameraState.Camera.backgroundColor = cameraState.FinalColor;
        }
    }
}