using BeauPools;
using BeauRoutine;
using BeauUtil;
using BeauUtil.Debugger;
using FieldDay;
using FieldDay.Collections;
using FieldDay.Scenes;
using FieldDay.SharedState;
using FieldDay.Systems;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using UnityEngine;

namespace SpaceFab.Comic
{
    public class ComicLayoutSpawnSystem : SystemComponent
    {
        public override unsafe void RegisterSystems(ref SystemRegistrationTable ecs) {
            ecs.Register(&UpdateSpawns,
                new SysUpdate(GameLoopPhase.Update, 100),
                new SysPermissions()
                    .ReadShared<ComicResourcePool>()
                    .ReadShared<ComicStreamingState>()
                    .ReadWriteShared<ComicLayoutState>());
        }

         static private void UpdateSpawns(float dt) {
            Find.State(out ComicResourcePool resourcePool, out ComicLayoutState layoutState, out ComicStreamingState streamingState);

            while(layoutState.SpawnBuffer.TryPeekFront(out LayoutSpawnRequest request)) {
                if (ComicResourceUtility.AreMeshesForRequestLoaded(resourcePool, streamingState, layoutState, request)) {
                    ComicResourceUtility.FulfillSpawnRequest(resourcePool, streamingState, layoutState, request);
                    layoutState.SpawnBuffer.PopFront();
                    layoutState.SpawnIdAllocator.Free(request.RequestId);
                } else {
                    break;
                }
            }
        }
    }
}