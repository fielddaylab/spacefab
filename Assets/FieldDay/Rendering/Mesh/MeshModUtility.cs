using System;
using System.Collections.Generic;
using BeauUtil;
using BeauUtil.Debugger;
using Unity.Collections;
using UnityEngine;
using UnityEngine.Rendering;

namespace FieldDay.Rendering {
    static public class MeshModUtility {
        static private List<Vector2> s_Vector2Cache = new List<Vector2>(128);
        static private List<Vector3> s_Vector3Cache = new List<Vector3>(128);
        static private readonly Rect s_DefaultUVSpace = new Rect(0, 0, 1, 1);

        /// <summary>
        /// Applies a transform to all vertex positions.
        /// </summary>
        static public void TransformPositions(Mesh mesh, Matrix4x4 matrix) {
            mesh.GetVertices(s_Vector3Cache);
            for (int i = 0; i < s_Vector3Cache.Count; i++) {
                s_Vector3Cache[i] = matrix.MultiplyPoint3x4(s_Vector3Cache[i]);
            }
            mesh.SetVertices(s_Vector3Cache);

            mesh.RecalculateNormals(MeshUpdateFlags.DontRecalculateBounds | MeshUpdateFlags.DontNotifyMeshUsers);
            mesh.RecalculateTangents(MeshUpdateFlags.DontRecalculateBounds | MeshUpdateFlags.DontNotifyMeshUsers);
            mesh.RecalculateBounds();

            s_Vector3Cache.Clear();
        }

        /// <summary>
        /// Remaps the UVs for the given mesh channel from 0-1 space to the given space.
        /// </summary>
        static public void RemapUVs(Mesh mesh, int channel, Rect uvSpace) {
            mesh.GetUVs(channel, s_Vector2Cache);
            for(int i = 0; i < s_Vector2Cache.Count; i++) {
                s_Vector2Cache[i] = Geom.Remap(s_Vector2Cache[i], s_DefaultUVSpace, uvSpace);
            }
            mesh.SetUVs(channel, s_Vector2Cache);
            s_Vector2Cache.Clear();
        }

        /// <summary>
        /// Remaps the UVs for the given mesh channel from the given original space to the new space.
        /// </summary>
        static public void RemapUVs(Mesh mesh, int channel, Rect originalSpace, Rect uvSpace) {
            mesh.GetUVs(channel, s_Vector2Cache);
            for (int i = 0; i < s_Vector2Cache.Count; i++) {
                s_Vector2Cache[i] = Geom.Remap(s_Vector2Cache[i], originalSpace, uvSpace);
            }
            mesh.SetUVs(channel, s_Vector2Cache);
            s_Vector2Cache.Clear();
        }

        /// <summary>
        /// Ensures index buffers are 16-bit.
        /// </summary>
        static public unsafe void UseShortIndexBuffer(Mesh mesh) {
            if (mesh.indexFormat == IndexFormat.UInt32) {
                Assert.True(mesh.vertexCount <= ushort.MaxValue, "Mesh has too many vertices to be converted to 16-bit");

                // TODO: optimize
                Assert.True(Game.IsEditor, "This method is ugly and needs to be optimized a lot (eventually) - don't use it outside of the editor");

                int[] indices = mesh.GetIndices(0);
                mesh.indexFormat = IndexFormat.UInt16;
                mesh.SetIndices(indices, MeshTopology.Triangles, 0);

                mesh.UploadMeshData(false);

                Assert.True(mesh.indexFormat == IndexFormat.UInt16);

                //int subMeshCount = mesh.subMeshCount;
                //int maxIndexCountInSubMesh = 0;
                //int maxIndexDiscovered = 0;
                //SubMeshDescriptor* subMeshes = stackalloc SubMeshDescriptor[subMeshCount];
                //for(int i = 0; i < subMeshCount; i++) {
                //    SubMeshDescriptor subMesh = mesh.GetSubMesh(i);
                //    subMeshes[i] = subMesh;
                //    maxIndexCountInSubMesh = Math.Max(maxIndexCountInSubMesh, subMesh.indexCount);
                //    maxIndexDiscovered = Math.Max(maxIndexDiscovered, subMesh.indexStart + subMesh.indexCount);
                //}

                //uint* readIndex32 = stackalloc uint[maxIndexCountInSubMesh];
                //ushort* writeIndex16 = stackalloc ushort[maxIndexCountInSubMesh];


                //using (var meshDataArray = Mesh.AcquireReadOnlyMeshData(mesh)) {
                //    var meshData = meshDataArray[0];
                //    meshData.indexFormat
                //}

                //using (var indexBuffer = mesh.GetIndexBuffer()) {
                //    int indexCount = indexBuffer.count;
                //    ushort* copy = stackalloc ushort[indexCount];

                //    mesh.SetIndexBufferParams(indexCount, IndexFormat.UInt16);
                //    mesh.SetIndexBufferData(Unsafe.NativeArray(copy, indexCount), 0, 0, indexCount, MeshUpdateFlags.Default);
                //}

                //mesh.SetIndexBufferParams(maxIndexDiscovered, IndexFormat.UInt16);
                //mesh.SetSubMeshes(Unsafe.NativeArray(subMeshes, subMeshCount), MeshUpdateFlags.Default);
            }
        }
    }
}