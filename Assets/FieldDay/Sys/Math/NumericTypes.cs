using BeauUtil;
using BeauUtil.Debugger;
using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using UnityEngine;

namespace FieldDay.Mathematics {
    /// <summary>
    /// 128-bit numeric vector.
    /// </summary>
    [StructLayout(LayoutKind.Explicit)]
    public unsafe struct Vector128 : IEquatable<Vector128> {
        // packed
        [FieldOffset(0)] public fixed byte PackedUInt8[16];
        [FieldOffset(0)] public fixed sbyte PackedInt8[16];
        [FieldOffset(0)] public fixed ushort PackedUInt16[8];
        [FieldOffset(0)] public fixed short PackedInt16[8];
        [FieldOffset(0)] public fixed uint PackedUInt32[4];
        [FieldOffset(0)] public fixed int PackedInt32[4];
        [FieldOffset(0)] public fixed ulong PackedUInt64[2];
        [FieldOffset(0)] public fixed long PackedInt64[2];
        [FieldOffset(0)] public fixed float PackedFloat32[4];
        [FieldOffset(0)] public fixed double PackedFloat64[2];
        [FieldOffset(0)] public BitSet128 PackedBits;

        #region Accessors

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public ref int Int32() {
            return ref PackedInt32[0];
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public ref int Int32(uint index) {
            Assert.True(index < 4);
            return ref PackedInt32[index];
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public ref uint UInt32() {
            return ref PackedUInt32[0];
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public ref uint UInt32(uint index) {
            Assert.True(index < 4);
            return ref PackedUInt32[index];
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public ref long Int64() {
            return ref PackedInt64[0];
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public ref long Int64(uint index) {
            Assert.True(index < 2);
            return ref PackedInt64[index];
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public ref ulong UInt64() {
            return ref PackedUInt64[0];
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public ref ulong UInt64(uint index) {
            Assert.True(index < 2);
            return ref PackedUInt64[index];
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public ref short Int16() {
            return ref PackedInt16[0];
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public ref short Int16(uint index) {
            Assert.True(index < 8);
            return ref PackedInt16[index];
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public ref ushort UInt16() {
            return ref PackedUInt16[0];
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public ref ushort UInt16(uint index) {
            Assert.True(index < 8);
            return ref PackedUInt16[index];
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public ref sbyte Int8() {
            return ref PackedInt8[0];
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public ref sbyte Int8(uint index) {
            Assert.True(index < 16);
            return ref PackedInt8[index];
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public ref byte UInt8() {
            return ref PackedUInt8[0];
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public ref byte UInt8(uint index) {
            Assert.True(index < 16);
            return ref PackedUInt8[index];
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public ref float Float() {
            return ref PackedFloat32[0];
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public ref float Float(uint index) {
            Assert.True(index < 4);
            return ref PackedFloat32[index];
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public ref double Double() {
            return ref PackedFloat64[0];
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public ref double Double(uint index) {
            Assert.True(index < 2);
            return ref PackedFloat64[index];
        }

        public ref Vector2 Float2() {
            return ref Unsafe.FastCast<float, Vector2>(ref PackedFloat32[0]);
        }

        public ref Vector2 Float2(uint index) {
            Assert.True(index < 2);
            return ref Unsafe.FastCast<float, Vector2>(ref PackedFloat32[index * 2]);
        }

        public ref Vector3 Float3() {
            return ref Unsafe.FastCast<float, Vector3>(ref PackedFloat32[0]);
        }

        public ref Vector4 Float4() {
            return ref Unsafe.FastCast<float, Vector4>(ref PackedFloat32[0]);
        }

        public ref Quaternion Quat() {
            return ref Unsafe.FastCast<float, Quaternion>(ref PackedFloat32[0]);
        }

        public ref Color32 Color() {
            return ref Unsafe.FastCast<uint, Color32>(ref PackedUInt32[0]);
        }

        public ref Color ColorF() {
            return ref Unsafe.FastCast<float, Color>(ref PackedFloat32[0]);
        }

        public ref Color32 Color(uint index) {
            Assert.True(index < 4);
            return ref Unsafe.FastCast<uint, Color32>(ref PackedUInt32[0]);
        }

        public ref bool Bool() {
            return ref Unsafe.FastCast<byte, bool>(ref PackedUInt8[0]);
        }

        public ref bool Bool(uint index) {
            Assert.True(index < 16);
            return ref Unsafe.FastCast<byte, bool>(ref PackedUInt8[index]);
        }

        #endregion // Accessors

        #region Overrides

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool Equals(Vector128 other) {
            return PackedUInt64[0] == other.PackedUInt64[0]
                & PackedUInt64[1] == other.PackedUInt64[1];
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public override bool Equals(object obj) {
            return Equals((Vector128)obj);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public override int GetHashCode() {
            return PackedUInt64[0].GetHashCode() ^ PackedUInt64[1].GetHashCode();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static public bool operator ==(Vector128 left, Vector128 right) {
            return left.PackedUInt64[0] == right.PackedUInt64[0]
                & left.PackedUInt64[1] == right.PackedUInt64[1];
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static public bool operator !=(Vector128 left, Vector128 right) {
            return left.PackedUInt64[0] != right.PackedUInt64[0]
                | left.PackedUInt64[1] != right.PackedUInt64[1];
        }

        #endregion // Overrides
    }

    /// <summary>
    /// Fixed-size float array of 8 elements.
    /// </summary>
    [Serializable]
    public unsafe struct Float8 {
        public fixed float Values[8];

        public Float8(int count, float initialValue) {
            Assert.True(count <= 8);
            for (int i = 0; i < count; i++) {
                Values[i] = initialValue;
            }
        }

        public Float8(float initialValue) {
            for (int i = 0; i < 8; i++) {
                Values[i] = initialValue;
            }
        }

        public ref float this[int index] {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get { return ref Values[index]; }
        }

        public int Length {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get { return 8; }
        }
    }

    /// <summary>
    /// Fixed-size double array of 8 elements.
    /// </summary>
    [Serializable]
    public unsafe struct Double8 {
        public fixed double Values[8];

        public Double8(int count, double initialValue) {
            Assert.True(count <= 8);
            for (int i = 0; i < count; i++) {
                Values[i] = initialValue;
            }
        }

        public Double8(double initialValue) {
            for (int i = 0; i < 8; i++) {
                Values[i] = initialValue;
            }
        }

        public ref double this[int index] {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get { return ref Values[index]; }
        }

        public int Length {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get { return 8; }
        }
    }
}