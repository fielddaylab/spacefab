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

        /// <summary>
        /// Retrieves the sprite buffer.
        /// </summary>
        static public MeshData16<SpriteVertex> GetSpriteBuffer() {
            if (s_SpriteVertexBuilder == null) {
                s_SpriteVertexBuilder = new MeshData16<SpriteVertex>(InitialVertexCount, InitialIndexCount, MeshTopology.Triangles, true);
            } else {
                s_SpriteVertexBuilder.Clear();
            }

            return s_SpriteVertexBuilder;
        }
    }
}