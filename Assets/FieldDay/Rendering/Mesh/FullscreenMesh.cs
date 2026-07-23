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
    static public class FullscreenMesh {
        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        private struct Vertex {
            [VertexAttr(VertexAttribute.Position)] public Vector2 Position;
        }

        static private Mesh s_CachedMesh;

        [EditorStaticResource]
        static public Mesh CreateMesh() {
            if (s_CachedMesh) {
                return s_CachedMesh;
            }

            EditorStaticResource.SetupLifetime(() => CreateMesh(), DestroyMesh);

            MeshData16<Vertex> meshData = new MeshData16<Vertex>(3, MeshTopology.Triangles, false);
            meshData.AddTriangle(
                new Vertex() { Position = new Vector2(0, 0) },
                new Vertex() { Position = new Vector2(0, 2) },
                new Vertex() { Position = new Vector2(2, 0) });
            Mesh mesh = new Mesh();
            meshData.Upload(mesh, MeshDataUploadFlags.MarkNoLongerReadable | MeshDataUploadFlags.DontRecalculateBounds);
            mesh.bounds = new Bounds(default, new Vector3(ushort.MaxValue, ushort.MaxValue, ushort.MaxValue));
            mesh.name = "Fullscreen-Viewport";
            meshData.Release();
            s_CachedMesh = mesh;

            Log.Msg("[FullscreenMesh] Created mesh");

            return mesh;
        }

        static public void DestroyMesh() {
            if (s_CachedMesh) {
                GameObject.DestroyImmediate(s_CachedMesh);
                s_CachedMesh = null;

                Log.Msg("[FullscreenMesh] Destroyed mesh");
            }
        }
    }
}