using System;
using BeauPools;
using BeauRoutine;
using BeauUtil;
using FieldDay.Components;
using FieldDay.Mathematics;
using UnityEngine;

namespace SpaceFab.Comic {
    public sealed class ComicRenderElement : BatchedComponent {
        [Serializable] public sealed class Pool : SerializablePool<ComicRenderElement> { }

        public MeshFilter MeshFilter;
        public MeshRenderer MeshRenderer;

        [NonSerialized] public StringHash32 Id;
        [NonSerialized] public ComicRenderElement Sibling;
        [NonSerialized] public float Visibility;
        [NonSerialized] public Routine Animation;

        [NonSerialized] public ushort TextureIndex = ComicTexture.NullTextureIndex;
        [NonSerialized] public Material BaseMaterial;
        [NonSerialized] public Material TempMaterial;
    }

    static public partial class ComicsUtility {
        static public Color UnpackColor565(ushort packedColor) {
            int r = (packedColor >> 11) & 0x1F;
            int b = (packedColor >> 5) & 0x3F;
            int g = (packedColor) & 0x1F;
            return new Color(
                (float) r / 0x1f,
                (float) g / 0x3f,
                (float) b / 0x1f,
                1.0f);
        }

        static public Vector2 UnpackPoint(PackedPoint point) {
            return new Vector2(
                FixedPoint.Q12_3.ToFloat(point.PackedX),
                FixedPoint.Q12_3.ToFloat(point.PackedY)
            );
        }

        static public void GenerateMaskMesh(UnmanagedMeshData16<ComicMeshVertex> meshData, in MaskData data, Vector2 texCoord) {
            Vector2 a = UnpackPoint(data.P0);
            Vector2 b = UnpackPoint(data.P1);
            Vector2 c = UnpackPoint(data.P2);
            Vector2 d = UnpackPoint(data.P3);

            Vector2 min = new Vector2(
                Math.Min(Math.Min(Math.Min(a.x, b.x), c.x), d.x),
                Math.Min(Math.Min(Math.Min(a.y, b.y), c.y), d.y)
                );
            Vector2 range = new Vector2(
                Math.Max(Math.Max(Math.Max(a.x, b.x), c.x), d.x) - min.x,
                Math.Max(Math.Max(Math.Max(a.y, b.y), c.y), d.y) - min.y
                );

            ComicMeshVertex vertA;
            vertA.Position = a;
            vertA.PackedUVs = new Vector4(
                texCoord.x,
                texCoord.y,
                (a.x - min.x) / range.x,
                (a.y - min.y) / range.y
            );

            ComicMeshVertex vertB;
            vertB.Position = b;
            vertB.PackedUVs = new Vector4(
                texCoord.x,
                texCoord.y,
                (b.x - min.x) / range.x,
                (b.y - min.y) / range.y
            );

            ComicMeshVertex vertC;
            vertC.Position = c;
            vertC.PackedUVs = new Vector4(
                texCoord.x,
                texCoord.y,
                (c.x - min.x) / range.x,
                (c.y - min.y) / range.y
            );

            ComicMeshVertex vertD;
            vertD.Position = d;
            vertD.PackedUVs = new Vector4(
                texCoord.x,
                texCoord.y,
                (d.x - min.x) / range.x,
                (d.y - min.y) / range.y
            );

            meshData.AddQuad(vertA, vertB, vertC, vertD);
        }
    }
}