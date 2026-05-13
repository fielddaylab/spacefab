using BeauUtil;
using BeauUtil.Debugger;
using FieldDay;
using FieldDay.Data;
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
                new SysUpdate(GameLoopPhase.PreUpdate, 0).AllowDuringLoad(),
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
                    UploadMesh(decompState, resourcePool);
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

        static private unsafe void UploadMesh(in MeshDecompressionState decompState, ComicResourcePool resourcePool) {
            Mesh mesh = decompState.TargetMesh;

            int vertexCount = decompState.Reader.VertexCount;
            Assert.True(vertexCount == decompState.Reader.WrittenVertices, "Vertex counts do not match!");
            ComicMeshVertex* vertexHead = decompState.Reader.VertexWriteHead - vertexCount;

            int indexCount = decompState.Reader.IndexCount;
            Assert.True(indexCount == decompState.Reader.WrittenIndices, "Index counts do not match!");
            ushort* indexHead = decompState.Reader.IndexWriteHead - indexCount;

            mesh.SetVertexBufferParams(vertexCount, resourcePool.MeshVertexLayout.Descriptors);
            mesh.SetVertexBufferData(Unsafe.NativeArray(vertexHead, vertexCount), 0, 0, vertexCount, 0, MeshUpdate_IgnoreAll);
            mesh.SetIndexBufferParams(decompState.Reader.IndexCount, IndexFormat.UInt16);
            mesh.SetIndexBufferData(Unsafe.NativeArray(indexHead, indexCount), 0, 0, indexCount, MeshUpdate_IgnoreAll);
            mesh.subMeshCount = 1;
            mesh.SetSubMesh(0, new SubMeshDescriptor(0, indexCount, MeshTopology.Triangles), MeshUpdate_IgnoreAll);

            mesh.bounds = new Bounds(decompState.Reader.PositionBase + decompState.Reader.PositionRange / 2, decompState.Reader.PositionRange);
            mesh.UploadMeshData(false);
        }

        static private unsafe bool DequeueNextMesh(ComicStreamingState streaming, ComicResourcePool resourcePool) {
            while(streaming.MeshRequestQueue.TryPopFront(out ushort meshIndex)) {
                if (resourcePool.ActiveMeshes.ContainsKey(meshIndex)) {
                    continue;
                }

                streaming.MeshBufferArena.Reset();

                ComicSequenceManifest manifest = ComicSequenceManifest.Current;
                ComicMeshHeader meshData = manifest.Meshes[meshIndex];

                Mesh newMesh = resourcePool.MeshPool.Alloc();
                resourcePool.ActiveMeshes.Add(meshIndex, newMesh);

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
                Assert.False(LZCompression.IsError(lzResult), "mesh decompression failed!");

                return true;
            }

            return false;
        }
    }
}

