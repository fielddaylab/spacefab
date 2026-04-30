using UnityEngine;
using System.Runtime.InteropServices;
using BeauUtil;
using UnityEngine.Rendering;
using FieldDay.Data;
using System;

namespace SpaceFab.Comic {
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public struct ComicMeshVertex {
        [VertexAttr(VertexAttribute.Position)] public Vector2 Position;
        [VertexAttr(VertexAttribute.TexCoord0)] public Vector4 PackedUVs;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct ComicMeshHeader {
        public ushort VertexCount;
        public ushort IndexCount;
        public uint BinaryOffset;
        public uint BinaryLength;
    }

    public unsafe struct MeshReader {
        public byte* Stream;

        public int VertexCount;
        public int IndexCount;
        public Vector2 PositionBase;
        public Vector2 PositionRange;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct CompressedMeshVertex {
        public ushort X;
        public ushort Y;
        public ushort U;
        public ushort V;
    }

    static public unsafe class ComicMesh {
        public const float UVScale = 1.0f / (1 << 15);
        public const float PositionScale = 1.0f / (1 << 15);

        static public void DecodeHeaderBlock(ref MeshReader reader) {
            Vector2* vecStream = (Vector2*)reader.Stream;
            reader.PositionBase = *vecStream++;
            reader.PositionRange = *vecStream++;

            reader.Stream = (byte*) vecStream;
        }

        static public bool DecodeVertexBlock(ref MeshReader reader, UnmanagedMeshData16<ComicMeshVertex> meshData, uint blockSize) {
            int toRead = Math.Min((int) blockSize, reader.VertexCount - meshData.VertexCount);
            if (toRead <= 0) {
                return false;
            }

            float posBaseX = reader.PositionBase.x,
                posBaseY = reader.PositionBase.y,
                posScaleX = PositionScale * reader.PositionRange.x,
                posScaleY = PositionScale * reader.PositionRange.y;

            CompressedMeshVertex* vertStream = (CompressedMeshVertex*) reader.Stream;

            while(toRead-- > 0) {
                CompressedMeshVertex vert;
                vert = *vertStream++;
                meshData.AddVertex(new ComicMeshVertex() {
                    Position = new Vector2(posBaseX + vert.X * posScaleX, posBaseY * vert.Y * posScaleY),
                    PackedUVs = new Vector4(vert.U * UVScale, vert.V * UVScale,
                        vert.X * PositionScale, vert.Y * PositionScale)
                });
            }

            reader.Stream = (byte*) vertStream;

            return meshData.VertexCount < reader.VertexCount;
        }

        static public bool DecodeIndexBlock(ref MeshReader reader, UnmanagedMeshData16<ComicMeshVertex> meshData, uint blockSize) {
            int toRead = Math.Min((int) blockSize, reader.IndexCount - meshData.IndexCount);
            if (toRead <= 0) {
                return false;
            }

            ushort baseIndex;
            if (meshData.IndexCount <= 0) {
                baseIndex = *(ushort*) (reader.Stream);
                reader.Stream += sizeof(ushort);
            } else {
                baseIndex = meshData.Index(meshData.IndexCount - 1);
            }

            sbyte* idxStream = (sbyte*) reader.Stream;

            while (toRead-- > 0) {
                sbyte adjust = *idxStream++;
                baseIndex = (ushort) (baseIndex + adjust);
                meshData.AddIndex(baseIndex);
            }

            reader.Stream = (byte*) idxStream;

            return meshData.IndexCount < reader.IndexCount;
        }
    }
}