using BeauPools;
using BeauUtil;
using BeauUtil.Debugger;
using FieldDay;
using FieldDay.Data;
using FieldDay.Debugging;
using FieldDay.Systems;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace SpaceFab.Comic
{
    public class ComicStreamingSystem : SystemComponent
    {
        private const MeshUpdateFlags MeshUpdate_IgnoreAll = MeshUpdateFlags.DontRecalculateBounds | MeshUpdateFlags.DontValidateIndices | MeshUpdateFlags.DontResetBoneBounds | MeshUpdateFlags.DontNotifyMeshUsers;
        private const long TimeSliceMS = 3;

        public override unsafe void RegisterSystems(ref SystemRegistrationTable ecs) {
            ecs.Register(&UpdateMeshStreaming,
                new SysUpdate(GameLoopPhase.PreUpdate, 100).AllowDuringLoad(),
                new SysPermissions()
                    .ReadWriteShared<ComicStreamingState>()
                    .ReadWriteShared<ComicResourcePool>());
        }

        static private unsafe void UpdateMeshStreaming(float dt) {
            Find.State(out ComicStreamingState streaming, out ComicResourcePool resourcePool);

            long endTS = Frame.Timestamp() * TimeSpan.TicksPerMillisecond * TimeSliceMS;
            long currentTs;
            do {
                if (RunMeshDecompressionStep(streaming, resourcePool)) {
                    continue;
                }
                if (DequeueNextMesh(streaming, resourcePool)) {
                    continue;
                }

                break;
            }
            while ((currentTs = Frame.Timestamp()) < endTS);

            if (Game.IsDevBuild) {
                if (DebugInput.IsDown(KeyCode.F)) {
                    using (PooledStringBuilder psb = PooledStringBuilder.Create()) {
                        psb.Builder.Append("Meshes in Use: ").AppendNoAlloc(resourcePool.MeshPool.InUse)
                            .Append("\nMaterials in Use: ").AppendNoAlloc(resourcePool.MaterialPool.InUse)
                            .Append("\nTransforms in Use: ").AppendNoAlloc(resourcePool.ParentPool.InUse)
                            .Append("\nElements in Use: ").AppendNoAlloc(resourcePool.ElementPool.InUse);
                        DebugDraw.AddLogText(psb, Color.green);
                    }
                }
            }
        }

        static private unsafe bool RunMeshDecompressionStep(ComicStreamingState streaming, ComicResourcePool resourcePool) {
            ref MeshDecompressionState decompState = ref streaming.Decompressor;
            if (decompState.MeshIndex == ComicMesh.NullIndex || decompState.Phase == MeshDecompressionPhase.Done) {
                return false;
            }

            switch(decompState.Phase) {
                case MeshDecompressionPhase.Header: {
                    ComicMesh.DecodeHeaderBlock(ref decompState.Reader);
                    decompState.Phase = MeshDecompressionPhase.Vertex;
                    return true;
                }

                case MeshDecompressionPhase.Vertex: {
                    bool hasMore = ComicMesh.DecodeVertexBlock(ref decompState.Reader, 512u);
                    if (!hasMore) {
                        decompState.Phase = MeshDecompressionPhase.Index;
                    }
                    return true;
                }

                case MeshDecompressionPhase.Index: {
                    bool hasMore = ComicMesh.DecodeIndexBlock(ref decompState.Reader, 512u);
                    if (!hasMore) {
                        decompState.Phase = MeshDecompressionPhase.Upload;
                    }
                    return true;
                }

                case MeshDecompressionPhase.Upload: {
                    UploadMesh(streaming, decompState, resourcePool);
                    decompState.Phase = MeshDecompressionPhase.Done;
                    decompState.MeshIndex = ComicMesh.NullIndex;
                    decompState.TargetMesh = null;
                    return true;
                }

                default: {
                    return false;
                }
            }
        }

        static private unsafe void UploadMesh(ComicStreamingState streaming, in MeshDecompressionState decompState, ComicResourcePool resourcePool) {
            Mesh mesh = decompState.TargetMesh;

            int vertexCount = decompState.Reader.VertexCount;
            Assert.True(vertexCount == decompState.Reader.WrittenVertices, "Vertex counts do not match!");
            ComicMeshVertex* vertexHead = decompState.Reader.VertexWriteHead - vertexCount;

            int indexCount = decompState.Reader.IndexCount;
            Assert.True(indexCount == decompState.Reader.WrittenIndices, "Index counts do not match!");
            ushort* indexHead = decompState.Reader.IndexWriteHead - indexCount;

            Assert.True(decompState.Reader.Stream == decompState.Reader.StreamEnd, "Stream read too many bytes!");

            mesh.SetVertexBufferParams(vertexCount, resourcePool.MeshVertexLayout.Descriptors);
            mesh.SetVertexBufferData(Unsafe.NativeArray(vertexHead, vertexCount), 0, 0, vertexCount, 0, MeshUpdate_IgnoreAll);
            mesh.SetIndexBufferParams(indexCount, IndexFormat.UInt16);
            mesh.SetIndexBufferData(Unsafe.NativeArray(indexHead, indexCount), 0, 0, indexCount, MeshUpdate_IgnoreAll);
            mesh.subMeshCount = 1;
            mesh.SetSubMesh(0, new SubMeshDescriptor(0, indexCount, MeshTopology.Triangles), MeshUpdate_IgnoreAll & ~MeshUpdateFlags.DontValidateIndices);

            mesh.bounds = new Bounds(decompState.Reader.PositionBase + decompState.Reader.PositionRange / 2, decompState.Reader.PositionRange);
            mesh.UploadMeshData(false);

            Log.Msg("[ComicStreamingSystem] Generated layer mesh {0}", decompState.MeshIndex);
        }

        static private unsafe bool DequeueNextMesh(ComicStreamingState streaming, ComicResourcePool resourcePool) {
            while(streaming.MeshRequestQueue.TryPopFront(out ushort meshId)) {
                if (resourcePool.ActiveMeshes.ContainsKey(meshId)) {
                    continue;
                }

                streaming.MeshBufferArena.Reset();

                ComicsUtility.UnpackMeshId(meshId, out ushort meshIndex, out StreamedMeshType meshType);

                switch (meshType) {
                    case StreamedMeshType.Layer: {
                        BeginLoadingLayerMesh(streaming, resourcePool, meshId, meshIndex);
                        break;
                    }
                    case StreamedMeshType.Mask: {
                        GenerateMaskMesh(streaming, resourcePool, meshId, meshIndex);
                        break;
                    }
                }

                return true;
            }

            return false;
        }

        static private unsafe void BeginLoadingLayerMesh(ComicStreamingState streaming, ComicResourcePool resourcePool, ushort meshId, ushort meshIndex) {
            ComicSequenceManifest manifest = ComicsUtility.Manifest;
            ComicMeshHeader meshData = manifest.Meshes[meshIndex];

            Mesh newMesh = resourcePool.MeshPool.Alloc();
            resourcePool.ActiveMeshes.Add(meshId, newMesh);

            newMesh.name = "Layer Mesh " + meshIndex;

            ref MeshDecompressionState decompState = ref streaming.Decompressor;
            decompState.MeshIndex = meshIndex;
            decompState.Phase = MeshDecompressionPhase.Header;
            decompState.TargetMesh = newMesh;

            ref MeshReader reader = ref decompState.Reader;
            reader.Stream = streaming.MeshDecompressionPool.Ptr;
            reader.VertexCount = meshData.VertexCount;
            reader.IndexCount = meshData.IndexCount;
            reader.WrittenVertices = reader.WrittenIndices = 0;
            reader.PositionBase = default;
            reader.PositionRange = default;
            reader.VertexWriteHead = streaming.MeshBufferArena.AllocArray<ComicMeshVertex>(meshData.VertexCount);
            reader.IndexWriteHead = streaming.MeshBufferArena.AllocArray<ushort>(meshData.IndexCount);

            // lz decompress
            LZDecompressionResult lzResult = LZCompression.Decompress(manifest.MeshBuffer.Ptr + meshData.BinaryOffset, meshData.BinaryLength, streaming.MeshDecompressionPool.Ptr, (uint) streaming.MeshDecompressionPool.Length, out uint decompressedSize);
            Assert.False(LZCompression.IsError(lzResult), "mesh decompression failed: {0}!", lzResult);
            reader.StreamEnd = reader.Stream + decompressedSize;
        }
    
        static private unsafe void GenerateMaskMesh(ComicStreamingState streaming, ComicResourcePool resourcePool, ushort meshId, ushort maskIndex) {
            Mesh newMesh = resourcePool.MeshPool.Alloc();
            resourcePool.ActiveMeshes.Add(meshId, newMesh);

            newMesh.name = "Panel Mask " + maskIndex;

            MaskData maskData = ComicsUtility.Manifest.Masks[maskIndex];
            
            Vector2 a = ComicsUtility.UnpackPointPrecise(maskData.P0);
            Vector2 b = ComicsUtility.UnpackPointPrecise(maskData.P1);
            Vector2 c = ComicsUtility.UnpackPointPrecise(maskData.P2);
            Vector2 d = ComicsUtility.UnpackPointPrecise(maskData.P3);

            Vector2 min = new Vector2(
                Math.Min(Math.Min(Math.Min(a.x, b.x), c.x), d.x),
                Math.Min(Math.Min(Math.Min(a.y, b.y), c.y), d.y)
                );
            Vector2 range = new Vector2(
                Math.Max(Math.Max(Math.Max(a.x, b.x), c.x), d.x) - min.x,
                Math.Max(Math.Max(Math.Max(a.y, b.y), c.y), d.y) - min.y
                );

            ComicMeshVertex vertA;
            vertA.Position = a;
            vertA.PackedUVs = new Vector4(
                1,
                1,
                (a.x - min.x) / range.x,
                (a.y - min.y) / range.y
            );

            ComicMeshVertex vertB;
            vertB.Position = b;
            vertB.PackedUVs = new Vector4(
                1,
                1,
                (b.x - min.x) / range.x,
                (b.y - min.y) / range.y
            );

            ComicMeshVertex vertC;
            vertC.Position = c;
            vertC.PackedUVs = new Vector4(
                1,
                1,
                (c.x - min.x) / range.x,
                (c.y - min.y) / range.y
            );

            ComicMeshVertex vertD;
            vertD.Position = d;
            vertD.PackedUVs = new Vector4(
                1,
                1,
                (d.x - min.x) / range.x,
                (d.y - min.y) / range.y
            );

            resourcePool.MaskBuilder.AddQuad(vertA, vertB, vertC, vertD);
            resourcePool.MaskBuilder.Upload(newMesh, MeshDataUploadFlags.DontRecalculateBounds);
            resourcePool.MaskBuilder.Clear();

            newMesh.bounds = Geom.AABB(min, min + range);

            Log.Msg("[ComicStreamingSystem] Generated mask mesh {0}", maskIndex);
        }
    }
}

