using System;
using System.Runtime.InteropServices;
using BeauUtil;
using UnityEngine;

namespace SpaceFab.Comic {
    public struct LiveComicTexture {
        public ushort RefCount;
        public ushort PackedFormatSizeKey;
        public ushort TextureIndex;
        public float LastTouchedTimestamp;
        public Texture2D Texture;
        public Material Material;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct ComicTextureHeader {
        public byte SizeLog2;
        public byte Format;
        public uint BinaryOffset;
        public uint BinaryLength;
    }

    public unsafe struct ComicTextureReader {
        public byte* Stream;

        public uint ByteLength;
    }

    static public class ComicTexture {
        public const ushort NullTextureIndex = ushort.MaxValue;

        static public uint FastLog2(uint value) {
            // TODO: optimize
            return (uint) (Math.Log(value, 2.0) + 0.999f);
        }

        static public unsafe bool DecodeTextureData(ref ComicTextureReader reader, Texture2D texture) {
            using (var nativeData = Unsafe.NativeArray(reader.Stream, (int) reader.ByteLength)) {
                //texture.SetPixelData
            }
            return true;
        }
    }
}