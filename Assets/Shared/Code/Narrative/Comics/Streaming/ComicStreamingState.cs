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
        public RingBuffer<ushort> TextureRequestQueue = new RingBuffer<ushort>(64, RingBufferMode.Fixed);

        public UnsafeSpan<byte> MeshDecompressionPool;
        public UnsafeSpan<byte> TextureDecompressionPool;

        void IRegistrationCallbacks.OnDeregister() {
        }

        void IRegistrationCallbacks.OnRegister() {
            ComicResourcePool resources = Find.State<ComicResourcePool>();
            MeshDecompressionPool = resources.Allocator.AllocSpan<byte>(Unsafe.KiB * 256);
            TextureDecompressionPool = resources.Allocator.AllocSpan<byte>(Unsafe.MiB * 16);
        }
    }

    public struct MeshDecompressionState {
        public ushort MeshIndex;
        public MeshReader Reader;
    }
}