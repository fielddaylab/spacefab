using BeauUtil;
using BeauUtil.Debugger;
using FieldDay;
using FieldDay.Scenes;
using FieldDay.Scripting;
using FieldDay.SharedState;
using FieldDay.Systems;
using Leaf;
using Leaf.Runtime;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace SpaceFab.Overarching
{
    public class OverarchingCameraDriftSystem : SystemComponent {
        public override unsafe void RegisterSystems(ref SystemRegistrationTable ecs) {
            ecs.Register(&ProcessWork,
                new SysUpdate(GameLoopPhase.LateUpdate, 100000),
                new SysPermissions()
                    .ReadWriteShared<OverarchingCamera>()
            );
        }

        static private void ProcessWork(float dt) {
            Find.State(out OverarchingCamera camera);

            Transform driftCam = camera.Effects;
            Vector3 localPos = default;

            if (camera.CurrentPose != null) {
                double time = Time.timeAsDouble;
                localPos.x = ComputeDriftComponent(camera.CurrentPose.DriftMin.x, camera.CurrentPose.DriftMax.x, time, camera.DriftX.Offset, camera.DriftX.Scale * camera.CurrentPose.DriftSpeed);
                localPos.y = ComputeDriftComponent(camera.CurrentPose.DriftMin.y, camera.CurrentPose.DriftMax.y, time, camera.DriftY.Offset, camera.DriftY.Scale * camera.CurrentPose.DriftSpeed);
                localPos.z = ComputeDriftComponent(camera.CurrentPose.DriftMin.z, camera.CurrentPose.DriftMax.z, time, camera.DriftZ.Offset, camera.DriftZ.Scale * camera.CurrentPose.DriftSpeed);
            }

            driftCam.localPosition = localPos;
        }

        static private float ComputeDriftComponent(float min, float max, double time, double offset, double scale) {
            double timeComponent = (time + offset) * scale * Math.PI;
            float sin = (float) Math.Sin(timeComponent);
            float rangeLerp = ((sin + 1) / 2);
            return min + (max - min) * rangeLerp;
        }
    }
}