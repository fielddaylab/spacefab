using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using BeauUtil;
using BeauUtil.Debugger;
using UnityEngine;
using UnityEngine.Rendering;

namespace FieldDay.Rendering {
    static public class SpriteMeshUtility {
        static public SpriteRectMeshInfo ComputeRectMesh(Sprite sprite) {
            Assert.NotNullOrDestroyed(sprite);
            Assert.True(!sprite.packed || sprite.packingMode == SpritePackingMode.Rectangle, "Tightly-packed sprites cannot be made into rect meshes");
            
            SpriteRectMeshInfo rectMesh;
            rectMesh.Texture = sprite.texture;
            rectMesh.Bounds = Geom.BoundsToRect(sprite.bounds);
            rectMesh.Texcoords = RectUVs.FromTextureRect(rectMesh.Texture, sprite.textureRect, sprite.packingRotation);
            return rectMesh;
        }

        /// <summary>
        /// Appends a colored quad to the given mesh buffer.
        /// </summary>
        static public OffsetLengthU16 AppendColoredQuad(UnmanagedMeshData16<SpriteVertex> data, Vector3 pos0, Vector3 pos1, Vector3 pos2, Vector3 pos3, Vector2 texcoord, Color color) {
            SpriteVertex vertA, vertB, vertC, vertD;
            vertA.Texcoord = vertB.Texcoord = vertC.Texcoord = vertD.Texcoord = texcoord;
            vertA.Color = vertB.Color = vertC.Color = vertD.Color = color;
            vertA.Position = pos0;
            vertB.Position = pos1;
            vertC.Position = pos2;
            vertD.Position = pos3;
            return data.AddQuad(vertA, vertB, vertC, vertD);
        }

        /// <summary>
        /// Appends a colored quad to the given mesh buffer.
        /// </summary>
        static public OffsetLengthU16 AppendColoredQuad(MeshData16<SpriteVertex> data, Vector3 pos0, Vector3 pos1, Vector3 pos2, Vector3 pos3, Vector2 texcoord, Color color) {
            SpriteVertex vertA, vertB, vertC, vertD;
            vertA.Texcoord = vertB.Texcoord = vertC.Texcoord = vertD.Texcoord = texcoord;
            vertA.Color = vertB.Color = vertC.Color = vertD.Color = color;
            vertA.Position = pos0;
            vertB.Position = pos1;
            vertC.Position = pos2;
            vertD.Position = pos3;
            return data.AddQuad(vertA, vertB, vertC, vertD);
        }

        /// <summary>
        /// Appends a colored quad to the given mesh buffer.
        /// </summary>
        static public OffsetLengthU32 AppendColoredQuad(MeshData32<SpriteVertex> data, Vector3 pos0, Vector3 pos1, Vector3 pos2, Vector3 pos3, Vector2 texcoord, Color color) {
            SpriteVertex vertA, vertB, vertC, vertD;
            vertA.Texcoord = vertB.Texcoord = vertC.Texcoord = vertD.Texcoord = texcoord;
            vertA.Color = vertB.Color = vertC.Color = vertD.Color = color;
            vertA.Position = pos0;
            vertB.Position = pos1;
            vertC.Position = pos2;
            vertD.Position = pos3;
            return data.AddQuad(vertA, vertB, vertC, vertD);
        }
    }

    /// <summary>
    /// Rectangular sprite mesh information.
    /// </summary>
    public struct SpriteRectMeshInfo {
        /// <summary>
        /// Source texture.
        /// </summary>
        public Texture2D Texture;

        /// <summary>
        /// World bounds.
        /// </summary>
        public Rect Bounds;

        /// <summary>
        /// Texture coordinates.
        /// </summary>
        public RectUVs Texcoords;
    }

    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public struct SpriteVertex {
        [VertexAttr(VertexAttribute.Position)] public Vector4 Position;
        [VertexAttr(VertexAttribute.Color)] public Color32 Color;
        [VertexAttr(VertexAttribute.TexCoord0)] public Vector2 Texcoord;
    }

    /// <summary>
    /// Texture coordinates for a rectangular region.
    /// </summary>
    [StructLayout(LayoutKind.Sequential)]
    public struct RectUVs {
        public float U0;
        public float U1;
        public float V0;
        public float V1;

        public Vector2 Min {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get { return new Vector2(U0, V0); }
        }

        public Vector2 Center {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get { return new Vector2(0.5f * (U0 + U1), 0.5f * (V0 + V1)); }
        }

        public Vector2 Max {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get { return new Vector2(U1, V1); }
        }

        /// <summary>
        /// Calculates the UV coordinates for the given texture pixel rect.
        /// </summary>
        static public RectUVs FromTextureRect(Texture2D source, Rect pixelRegion) {
            RectUVs uvs;
            float texWidth = source.width, texHeight = source.height;
            uvs.U0 = pixelRegion.xMin / texWidth;
            uvs.U1 = pixelRegion.xMax / texWidth;
            uvs.V0 = pixelRegion.yMin / texHeight;
            uvs.V1 = pixelRegion.yMax / texHeight;
            return uvs;
        }

        /// <summary>
        /// Calculates the UV coordinates for the given texture pixel rect, optionally flipped on the X/Y axis.
        /// </summary>
        static public RectUVs FromTextureRect(Texture2D source, Rect pixelRegion, SpritePackingRotation rotation) {
            RectUVs uvs;
            float texWidth = source.width, texHeight = source.height;
            uvs.U0 = pixelRegion.xMin / texWidth;
            uvs.U1 = pixelRegion.xMax / texWidth;
            uvs.V0 = pixelRegion.yMin / texHeight;
            uvs.V1 = pixelRegion.yMax / texHeight;
            if ((rotation & SpritePackingRotation.FlipHorizontal) != 0) {
                Ref.Swap(ref uvs.U0, ref uvs.U1);
            }
            if ((rotation & SpritePackingRotation.FlipVertical) != 0) {
                Ref.Swap(ref uvs.V0, ref uvs.V1);
            }
            return uvs;
        }
        
        /// <summary>
        /// Computes the scale-offset vector for the given rectangular region.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static public Vector4 ComputeScaleOffset(RectUVs rectUvs) {
            return new Vector4(rectUvs.U1 - rectUvs.U0, rectUvs.V1 - rectUvs.V0, rectUvs.U0, rectUvs.V0);
        }
    }
}