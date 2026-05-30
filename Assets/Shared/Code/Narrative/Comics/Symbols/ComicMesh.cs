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
    [Serializable]
    public struct ComicMeshHeader {
        public ushort VertexCount;
        public ushort IndexCount;
        public uint BinaryOffset;
        public uint BinaryLength;
    }

    public unsafe struct MeshReader {
        public byte* Stream;
        public byte* StreamEnd;

        public int VertexCount;
        public int IndexCount;
        public Vector2 PositionBase;
        public Vector2 PositionRange;

        public ComicMeshVertex* VertexWriteHead;
        public ushort* IndexWriteHead;
        public int WrittenVertices;
        public int WrittenIndices;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct CompressedMeshVertex {
        public ushort X;
        public ushort Y;
        public ushort U;
        public ushort V;
    }

    static public unsafe class ComicMesh {
        public const float UVMultiplier = (1 << 15);
        private const float UVScale = 1.0f / UVMultiplier;

        public const float PositionMultiplier = (1 << 15);
        private const float PositionScale = 1.0f / PositionMultiplier;

        public const ushort NullIndex = ushort.MaxValue;

        static public void DecodeHeaderBlock(ref MeshReader reader) {
            Vector2* vecStream = (Vector2*)reader.Stream;
            reader.PositionBase = *vecStream++;
            reader.PositionRange = *vecStream++;

            reader.Stream = (byte*) vecStream;
        }

        static public bool DecodeVertexBlock(ref MeshReader reader, uint blockSize) {
            int toRead = Math.Min((int) blockSize, reader.VertexCount - reader.WrittenVertices);
            if (toRead <= 0) {
                return false;
            }

            float posBaseX = reader.PositionBase.x,
                posBaseY = reader.PositionBase.y,
                posScaleX = PositionScale * reader.PositionRange.x,
                posScaleY = PositionScale * reader.PositionRange.y;

            CompressedMeshVertex* vertStream = (CompressedMeshVertex*) reader.Stream;
            reader.WrittenVertices += toRead;

            while(toRead-- > 0) {
                CompressedMeshVertex vert;
                vert = *vertStream++;
                *reader.VertexWriteHead++ = new ComicMeshVertex() {
                    Position = new Vector2(posBaseX + vert.X * posScaleX, posBaseY + vert.Y * posScaleY),
                    PackedUVs = new Vector4(vert.U * UVScale, vert.V * UVScale,
                        vert.X * PositionScale, vert.Y * PositionScale)
                };
            }

            reader.Stream = (byte*) vertStream;

            return reader.WrittenVertices < reader.VertexCount;
        }

        static public bool DecodeIndexBlock(ref MeshReader reader, uint blockSize) {
            int toRead = Math.Min((int) blockSize, reader.IndexCount - reader.WrittenIndices);
            if (toRead <= 0) {
                return false;
            }

            ushort baseIndex;
            if (reader.WrittenIndices <= 0) {
                baseIndex = *(ushort*) (reader.Stream);
                reader.Stream += sizeof(ushort);
                *reader.IndexWriteHead++ = baseIndex;
                toRead--;
                reader.WrittenIndices++;
            } else {
                baseIndex = *(reader.IndexWriteHead - 1);
            }

            reader.WrittenIndices += toRead;

            sbyte* idxStream = (sbyte*) reader.Stream;

            while (toRead-- > 0) {
                sbyte adjust = *idxStream++;
                baseIndex = (ushort) (baseIndex + adjust);
                *reader.IndexWriteHead++ = baseIndex;
            }

            reader.Stream = (byte*) idxStream;

            return reader.WrittenIndices < reader.IndexCount;
        }
    }
}