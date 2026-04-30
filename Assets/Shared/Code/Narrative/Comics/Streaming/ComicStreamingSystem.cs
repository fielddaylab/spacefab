using BeauUtil;
using FieldDay;
using FieldDay.Systems;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace SpaceFab.Comic
{
    public class ComicStreamingSystem : SystemComponent
    {
        public override unsafe void RegisterSystems(ref SystemRegistrationTable ecs) {
            ecs.Register(&UpdateAssetStreaming,
                new SysUpdate(GameLoopPhase.PreUpdate, 0).AllowDuringLoad(),
                new SysPermissions()
                    .ReadWriteShared<ComicStreamingState>()
                    .ReadWriteShared<ComicResourcePool>()
                    .ReadShared<ComicManifestState>());
        }

        static private void UpdateAssetStreaming(float dt) {

        }
    }
}

