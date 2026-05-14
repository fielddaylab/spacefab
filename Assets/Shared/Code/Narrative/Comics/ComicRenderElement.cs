using System;
using BeauPools;
using BeauRoutine;
using BeauUtil;
using FieldDay.Animation;
using FieldDay.Components;
using FieldDay.Mathematics;
using UnityEngine;

namespace SpaceFab.Comic {
    public sealed class ComicRenderElement : BatchedComponent {
        [Serializable] public sealed class Pool : SerializablePool<ComicRenderElement> { }

        public MeshFilter MeshFilter;
        public MeshRenderer MeshRenderer;

        [NonSerialized] public StringHash32 Id;
        [NonSerialized] public ComicRenderElementType Type;
        [NonSerialized] public ComicRenderElement Sibling;
        [NonSerialized] public float Visibility;
        [NonSerialized] public Routine CoroutineAnimation;
        [NonSerialized] public AnimHandle LiteAnimation;

        [NonSerialized] public ushort MeshId = ushort.MaxValue;
        [NonSerialized] public Material BaseMaterial;
        [NonSerialized] public Material TempMaterial;
    }

    public enum ComicRenderElementType : uint {
        Layer,
        Mask
    }

    static public partial class ComicsUtility {
        #region Common Formats

        static public Color UnpackColor565(ushort packedColor) {
            int r = (packedColor >> 11) & 0x1F;
            int g = (packedColor >> 5) & 0x3F;
            int b = (packedColor) & 0x1F;
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

        static public Vector2 UnpackPointPrecise(PackedPoint point) {
            return new Vector2(
                FixedPoint.Q9_6.ToFloat(point.PackedX),
                FixedPoint.Q9_6.ToFloat(point.PackedY)
            );
        }

        static public float UnpackDegrees(short packed) {
            return FixedPoint.Q9_6.ToFloat(packed);
        }

        #endregion // Common Formats
    }
}