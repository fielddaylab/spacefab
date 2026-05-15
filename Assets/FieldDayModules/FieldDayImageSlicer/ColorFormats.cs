using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Unity.Collections;

namespace FieldDay.ImageSlicer {
    [StructLayout(LayoutKind.Explicit)]
    public struct PixelRGB24 {
        [FieldOffset(0)] public byte R;
        [FieldOffset(1)] public byte G;
        [FieldOffset(2)] public byte B;
    }

    [StructLayout(LayoutKind.Explicit)]
    public struct PixelARGB32 {
        [FieldOffset(0)] public uint Raw;

        [FieldOffset(0)] public byte A;
        [FieldOffset(1)] public byte R;
        [FieldOffset(2)] public byte G;
        [FieldOffset(3)] public byte B;
    }

    [StructLayout(LayoutKind.Explicit)]
    public struct PixelRGBA32 {
        [FieldOffset(0)] public uint Raw;

        [FieldOffset(0)] public byte R;
        [FieldOffset(1)] public byte G;
        [FieldOffset(2)] public byte B;
        [FieldOffset(3)] public byte A;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static public bool Equals(in PixelRGBA32 a, in PixelRGBA32 b) {
            return a.Raw == b.Raw;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static public bool ApproximatelyEquals(in PixelRGBA32 a, in PixelRGBA32 b) {
            int diffA = Math.Abs(a.A - b.A);
            int diffR = Math.Abs(a.R - b.R);
            int diffG = Math.Abs(a.G - b.G);
            int diffB = Math.Abs(a.B - b.B);
            return diffA < 2 & diffR < 2 && diffG < 2 && diffB < 2;
        }
    }

    static public unsafe partial class TileUtility {
        static public PixelRGBA32 RGB24ToRGBA32(PixelRGB24 pixel) {
            return new PixelRGBA32() {
                R = pixel.R,
                G = pixel.G,
                B = pixel.B,
                A = 255,
            };
        }

        static public PixelRGBA32 RGBA32Identity(PixelRGBA32 pixel) {
            return pixel;
        }

        static public PixelRGBA32 ARGB32ToRGBA32(PixelARGB32 pixel) {
            return new PixelRGBA32() {
                R = pixel.R,
                G = pixel.G,
                B = pixel.B,
                A = pixel.A,
            };
        }
    }
}