using BeauUtil;
using System;
using System.Runtime.InteropServices;
using UnityEngine;
using UnityEngine.Rendering;
using BeauUtil.Debugger;


#if UNITY_EDITOR
using UnityEditor;
#endif // UNITY_EDITOR

namespace FieldDay.Rendering {
    static public class RenderBuffers {
        private const int InitialVertexCount = 2048;
        private const int InitialIndexCount = InitialVertexCount * 6 / 4;

        static private MeshData16<SpriteVertex> s_SpriteVertexBuilder;
        static private MeshData16<PositionVertex> s_PositionVertexBuilder;

        /// <summary>
        /// Retrieves the empty sprite buffer.
        /// </summary>
        static public MeshData16<SpriteVertex> GetSpriteBuffer() {
            if (s_SpriteVertexBuilder == null) {
                s_SpriteVertexBuilder = new MeshData16<SpriteVertex>(InitialVertexCount, InitialIndexCount, MeshTopology.Triangles, true);
            } else {
                s_SpriteVertexBuilder.Clear();
            }

            return s_SpriteVertexBuilder;
        }

        /// <summary>
        /// Retrieves the empty position buffer.
        /// </summary>
        static public MeshData16<PositionVertex> GetPositionBuffer() {
            if (s_PositionVertexBuilder == null) {
                s_PositionVertexBuilder = new MeshData16<PositionVertex>(InitialVertexCount, InitialIndexCount, MeshTopology.Triangles, true);
            } else {
                s_PositionVertexBuilder.Clear();
            }

            return s_PositionVertexBuilder;
        }
    }

    /// <summary>
    /// Vertex with only a position.
    /// </summary>
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public struct PositionVertex {
        [VertexAttr(VertexAttribute.Position)] public Vector4 Position;
    }
}