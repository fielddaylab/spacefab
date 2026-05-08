using System;
using BeauPools;
using BeauUtil;
using FieldDay;
using FieldDay.SharedState;
using UnityEngine;

namespace SpaceFab.Comic {
    [SharedStateInitOrder(10)]
    public sealed class ComicStreamingState : SharedStateComponent, IRegistrationCallbacks {
        public RingBuffer<ushort> MeshRequestQueue = new RingBuffer<ushort>(64, RingBufferMode.Fixed);

        public UnsafeSpan<byte> MeshDecompressionPool;
        public Unsafe.ArenaHandle MeshBufferArena;
        public MeshDecompressionState Decompressor;

        void IRegistrationCallbacks.OnDeregister() {
        }

        void IRegistrationCallbacks.OnRegister() {
            ComicResourcePool resources = Find.State<ComicResourcePool>();
            MeshDecompressionPool = resources.Allocator.AllocSpan<byte>(Unsafe.KiB * 256);
            MeshBufferArena = Unsafe.CreateArena(resources.Allocator, Unsafe.KiB * 256);

            Decompressor = new MeshDecompressionState() {
                Phase = MeshDecompressionPhase.Done,
                MeshIndex = ComicMesh.NullIndex,
                Reader = default
            };
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
}