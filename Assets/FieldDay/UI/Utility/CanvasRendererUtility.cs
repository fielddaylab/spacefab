using BeauUtil;
using BeauUtil.Debugger;
using FieldDay.Rendering;
using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using TinyIL;
using Unity.IL2CPP.CompilerServices;
using UnityEngine;
using UnityEngine.UI;

namespace FieldDay.UI {
    static public class CanvasRendererUtility {

        /// <summary>
        /// Calculates the bounds of the given vertex stream's positions.
        /// </summary>
        [Il2CppSetOption(Option.NullChecks, false)]
        [Il2CppSetOption(Option.ArrayBoundsChecks, false)]
        static public Rect CalculateBounds(this VertexHelper helper) {
            Assert.NotNull(helper);

            List<Vector3> positions = GetMutablePositions(helper);
            int vertCount = positions.Count;
            if (vertCount <= 0) {
                return default;
            }

            Vector3 vert = positions[0];
            float minX, minY, maxX, maxY;
            minX = maxX = vert.x;
            minY = maxY = vert.y;
            for(int i = 1; i < vertCount; i++) {
                vert = positions[i];
                minX = Math.Min(minX, vert.x);
                minY = Math.Min(minY, vert.y);
                maxX = Math.Max(maxX, vert.x);
                maxY = Math.Max(maxY, vert.y);
            }

            return Rect.MinMaxRect(minX, minY, maxX, maxY);
        }

        /// <summary>
        /// Packs normalized bounds into UV0's z and w channels.
        /// </summary>
        [Il2CppSetOption(Option.NullChecks, false)]
        [Il2CppSetOption(Option.ArrayBoundsChecks, false)]
        static public void PackLocalPosIntoUV0ZW(this VertexHelper helper, Rect bounds) {
            Assert.NotNull(helper);

            List<Vector3> positions = GetMutablePositions(helper);
            List<Vector4> uvs = GetMutableUV0(helper);
            
            int vertCount = positions.Count;
            if (vertCount <= 0) {
                return;
            }

            Vector3 pos;
            Vector4 uv;

            float minX = bounds.xMin,
                minY = bounds.yMin,
                width = bounds.width,
                height = bounds.height;

            for (int i = 0; i < vertCount; i++) {
                pos = positions[i];
                uv = uvs[i];

                uv.z = (pos.x - minX) / width;
                uv.w = (pos.y - minY) / height;
                uvs[i] = uv;
            }
        }

        #region Private Extraction

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        [IntrinsicIL("ldarg.0; dup; callvirt [arg helper]::InitializeListIfRequired(); ldfld [arg helper]::m_Positions; ret")]
        static public List<Vector3> GetMutablePositions(this VertexHelper helper) {
            throw new NotImplementedException();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        [IntrinsicIL("ldarg.0; dup; callvirt [arg helper]::InitializeListIfRequired(); ldfld [arg helper]::m_Colors; ret")]
        static public List<Color32> GetMutableColors(this VertexHelper helper) {
            throw new NotImplementedException();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        [IntrinsicIL("ldarg.0; dup; callvirt [arg helper]::InitializeListIfRequired(); ldfld [arg helper]::m_Uv0S; ret")]
        static public List<Vector4> GetMutableUV0(this VertexHelper helper) {
            throw new NotImplementedException();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        [IntrinsicIL("ldarg.0; dup; callvirt [arg helper]::InitializeListIfRequired(); ldfld [arg helper]::m_Uv1S; ret")]
        static public List<Vector4> GetMutableUV1(this VertexHelper helper) {
            throw new NotImplementedException();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        [IntrinsicIL("ldarg.0; dup; callvirt [arg helper]::InitializeListIfRequired(); ldfld [arg helper]::m_Uv2S; ret")]
        static public List<Vector4> GetMutableUV2(this VertexHelper helper) {
            throw new NotImplementedException();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        [IntrinsicIL("ldarg.0; dup; callvirt [arg helper]::InitializeListIfRequired(); ldfld [arg helper]::m_Uv3S; ret")]
        static public List<Vector4> GetMutableUV3(this VertexHelper helper) {
            throw new NotImplementedException();
        }

        #endregion // Private Extraction
    }
}