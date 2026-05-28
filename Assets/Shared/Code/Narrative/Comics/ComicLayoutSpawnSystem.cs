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
                if (!AreMeshesForRequestLoaded(resourcePool, streamingState, layoutState, request)) {
                    break;
                }

                layoutState.SpawnBuffer.PopFront();
                layoutState.SpawnIdAllocator.Free(request.RequestId);
            }
        }

        static private bool AreMeshesForRequestLoaded(ComicResourcePool resourcePool, ComicStreamingState streamingState, ComicLayoutState layoutState, in LayoutSpawnRequest spawnRequest) {
            ushort pageIndex;
            
            if (spawnRequest.IsMask) {
                pageIndex = ComicsUtility.GetPageIndexForPanel(ComicsUtility.GetPanelIndexForMask(spawnRequest.LayerIndex));
                if (!layoutState.AllocatedPageMask.IsSet(pageIndex)) {
                    return false;
                }

                ushort meshId = ComicsUtility.PackMeshId(spawnRequest.LayerIndex, StreamedMeshType.Mask);
                return ComicsUtility.IsMeshLoaded(streamingState, resourcePool, meshId);
            }

            var manifest = ComicsUtility.Manifest;
            Assert.NotNullOrDestroyed(manifest);

            pageIndex = ComicsUtility.GetPageIndexForPanel(ComicsUtility.GetPanelIndexForLayer(spawnRequest.LayerIndex));
            if (!layoutState.AllocatedPageMask.IsSet(pageIndex)) {
                return false;
            }

            ushort layerIndex = spawnRequest.LayerIndex;
            while (layerIndex != ushort.MaxValue) {
                LayerData data = manifest.Layers[layerIndex];
                if (data.MeshIndex != ComicMesh.NullIndex) {
                    ushort meshId = ComicsUtility.PackMeshId(data.MeshIndex, StreamedMeshType.Layer);
                    if (!ComicsUtility.IsMeshLoaded(streamingState, resourcePool, meshId)) {
                        return false;
                    }
                }
                layerIndex = data.SiblingLayerIndex;
            }

            return true;
        }
    }
}