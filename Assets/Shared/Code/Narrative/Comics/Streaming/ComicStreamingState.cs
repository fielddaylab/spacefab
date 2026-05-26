using System;
using System.Runtime.CompilerServices;
using BeauPools;
using BeauUtil;
using BeauUtil.Debugger;
using FieldDay;
using FieldDay.Scenes;
using FieldDay.SharedState;
using UnityEngine;

namespace SpaceFab.Comic {
    [SharedStateInitOrder(10)]
    public sealed class ComicStreamingState : SharedStateComponent, IRegistrationCallbacks, ISceneLoadDependency {
        public RingBuffer<ushort> MeshRequestQueue = new RingBuffer<ushort>(64, RingBufferMode.Fixed);

        public UnsafeSpan<byte> MeshDecompressionPool;
        public Unsafe.ArenaHandle MeshBufferArena;
        public MeshDecompressionState Decompressor;

        bool ISceneLoadDependency.IsLoaded(SceneLoadPhase loadPhase) {
            if (loadPhase == SceneLoadPhase.BeforeReady) {
                return MeshRequestQueue.Count == 0 && Decompressor.Phase == MeshDecompressionPhase.Done;
            } else {
                return true;
            }
        }

        void IRegistrationCallbacks.OnDeregister() {
            Game.Scenes.DeregisterLoadDependency(this);
        }

        unsafe void IRegistrationCallbacks.OnRegister() {
            ComicResourcePool resources = Find.State<ComicResourcePool>();
            MeshDecompressionPool = new UnsafeSpan<byte>((byte*) resources.Allocator.AllocAligned(Unsafe.MiB, 8), Unsafe.MiB);
            MeshBufferArena = Unsafe.CreateArena(resources.Allocator, Unsafe.MiB * 2);

            Decompressor = new MeshDecompressionState() {
                Phase = MeshDecompressionPhase.Done,
                MeshIndex = ComicMesh.NullIndex,
                Reader = default
            };

            Game.Scenes.RegisterLoadDependency(this);
        }
    }

    public struct MeshDecompressionState {
        public ushort MeshIndex;
        public MeshDecompressionPhase Phase;
        public MeshReader Reader;
        public Mesh TargetMesh;
    }

    public enum MeshDecompressionPhase : byte {
        Header,
        Vertex,
        Index,
        Upload,
        Done
    }

    public enum StreamedMeshType : byte {
        Layer,
        Mask
    }

    static public partial class ComicsUtility {
        private const ushort MeshIndexMask = (1 << 15) - 1;
        private const ushort MeshTypeShift = 15;
        private const ushort MeshTypeBit = (1 << MeshTypeShift);
        
        static public ushort PreloadMesh(ushort index, StreamedMeshType type) {
            ushort meshId = PackMeshId(index, type);
            Find.State<ComicStreamingState>().MeshRequestQueue.PushBack(meshId);
            return meshId;
        }

        static public void PreloadMesh(ushort meshId) {
            Find.State<ComicStreamingState>().MeshRequestQueue.PushBack(meshId);
        }

        static public void CancelMeshPreload(ushort meshId) {
            if (meshId == ComicMesh.NullIndex) {
                return;
            }

            UnpackMeshId(meshId, out ushort index, out StreamedMeshType meshType);
            CancelMeshPreload(index, meshType);
        }

        static public void CancelMeshPreload(ushort index, StreamedMeshType type) {
            ushort meshId = PackMeshId(index, type);

            if (meshId == ComicMesh.NullIndex) {
                return;
            }

            ComicStreamingState streamState = Find.State<ComicStreamingState>();
            if (streamState.MeshRequestQueue.Remove(meshId)) {
                Log.Msg("[ComicsUtility] Cancelled mesh {0} (1) load request", index, type);
                return;
            }

            if (type == StreamedMeshType.Layer) {
                ref MeshDecompressionState decompState = ref streamState.Decompressor;
                if (decompState.MeshIndex == index) {
                    decompState.Phase = MeshDecompressionPhase.Done;
                    decompState.MeshIndex = ComicMesh.NullIndex;
                    decompState.TargetMesh = null;
                    decompState.Reader = default;
                    Log.Msg("[ComicsUtility] Cancelled in-progress mesh {0} (1) load", index, type);
                }
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static public ushort PackMeshId(ushort index, StreamedMeshType type) {
            return (ushort) ((index & MeshIndexMask) | (((int) type & 1) << MeshTypeShift));
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static public void UnpackMeshId(ushort id, out ushort index, out StreamedMeshType type) {
            index = (ushort) (id & MeshIndexMask);
            type = (StreamedMeshType) ((id >> MeshTypeShift) & 1);
        }
    }
}