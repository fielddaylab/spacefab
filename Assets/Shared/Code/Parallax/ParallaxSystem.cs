using System;
using BeauRoutine;
using BeauUtil;
using FieldDay;
using FieldDay.Components;
using FieldDay.Systems;
using UnityEngine;

namespace SpaceFab.Parallax {
    public sealed class ParallaxSystem : SystemComponent {
        public unsafe override void RegisterSystems(ref SystemRegistrationTable ecs) {
            ecs.Register(&Update,
                new SysUpdate(GameLoopPhase.UnscaledLateUpdate, 10000),
                new SysPermissions()
                    .ReadWrite<ParallaxLayer>());
        }

        static private void Update(float dt) {
            Camera camera = Game.Rendering.PrimaryCamera;
            if (!camera.TryGetComponent(out ParallaxTracker tracker)) {
                return;
            }

            Vector2 pos = camera.transform.localPosition;

            var layers = Find.Components<ParallaxLayer>();
            foreach(var layer in layers) {
                Transform root = layer.CacheComponent(ref layer.Root);
                Vector3 rootPos = root.localPosition;
                rootPos.x = pos.x * layer.Scale;
                rootPos.y = pos.y * layer.Scale;
                root.localPosition = rootPos;
            }
        }
    }
}