using System.Runtime.CompilerServices;
using Unity.IL2CPP.CompilerServices;

namespace FieldDay.Mathematics {
    /// <summary>
    /// Fixed point conversions.
    /// </summary>
    [Il2CppEagerStaticClassConstruction]
    static public class FixedPoint {
        #region Shared

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static private short FloatToShort(float value, int fractionalBits) {
            return (short)(value * (float)(1 << fractionalBits));
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static private float ShortToFloat(short value, int fractionalBits) {
            return (value * (1.0f / (float)(1 << fractionalBits)));
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static private ushort FloatToUShort(float value, int fractionalBits) {
            return (ushort)(value * (float)(1 << fractionalBits));
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static private float UShortToFloat(ushort value, int fractionalBits) {
            return (value * (1.0f / (float)(1 << fractionalBits)));
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static private sbyte FloatToSByte(float value, int fractionalBits) {
            return (sbyte)(value * (float)(1 << fractionalBits));
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static private float SByteToFloat(sbyte value, int fractionalBits) {
            return (value * (1.0f / (float)(1 << fractionalBits)));
        }

        #endregion // Shared

        /// <summary>
        /// [-2048, 2047] + [1/8]
        /// </summary>
        [Il2CppEagerStaticClassConstruction]
        static public class Q12_3 {
            private const int FractionalBits = 3;

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            static public short FromFloat(float value) {
                return FloatToShort(value, FractionalBits);
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            static public float ToFloat(short value) {
                return ShortToFloat(value, FractionalBits);
            }
        }

        /// <summary>
        /// [-8, 7] + [1/4096]
        /// </summary>
        [Il2CppEagerStaticClassConstruction]
        static public class Q3_12 {
            private const int FractionalBits = 12;

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            static public short FromFloat(float value) {
                return FloatToShort(value, FractionalBits);
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            static public float ToFloat(short value) {
                return ShortToFloat(value, FractionalBits);
            }
        }

        /// <summary>
        /// [-512, 511] + [1/64]
        /// </summary>
        [Il2CppEagerStaticClassConstruction]
        static public class Q9_6 {
            private const int FractionalBits = 6;

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            static public short FromFloat(float value) {
                return FloatToShort(value, FractionalBits);
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            static public float ToFloat(short value) {
                return ShortToFloat(value, FractionalBits);
            }
        }

        /// <summary>
        /// [-128, 127] + [1/256]
        /// </summary>
        [Il2CppEagerStaticClassConstruction]
        static public class Q7_8 {
            private const int FractionalBits = 8;

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            static public short FromFloat(float value) {
                return FloatToShort(value, FractionalBits);
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            static public float ToFloat(short value) {
                return ShortToFloat(value, FractionalBits);
            }
        }

        /// <summary>
        /// +- [1/127]
        /// </summary>
        [Il2CppEagerStaticClassConstruction]
        static public class Q0_7 {
            private const int FractionalBits = 7;

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            static public sbyte FromFloat(float value) {
                return FloatToSByte(value, FractionalBits);
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            static public float ToFloat(sbyte value) {
                return SByteToFloat(value, FractionalBits);
            }
        }

        /// <summary>
        /// [-16, 15] + [1/2048]
        /// </summary>
        [Il2CppEagerStaticClassConstruction]
        static public class Q4_11 {
            private const int FractionalBits = 11;

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            static public short FromFloat(float value) {
                return FloatToShort(value, FractionalBits);
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            static public float ToFloat(short value) {
                return ShortToFloat(value, FractionalBits);
            }
        }
    }
}