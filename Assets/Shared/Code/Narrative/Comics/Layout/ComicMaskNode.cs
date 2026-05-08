using BeauUtil;
using FieldDay.Scenes;
using System;
using UnityEngine;
using FieldDay;

#if UNITY_EDITOR
using UnityEditor;
#endif // UNITY_EDITOR

namespace SpaceFab.Comic {
    [RequireComponent(typeof(DynamicMeshFilter), typeof(MeshRenderer))]
    [ExecuteAlways]
    public sealed class ComicMaskNode : MonoBehaviour, IEditModeOnly {
        public Color32 Color = ColorBank.OldLace;
        public Vector2 Offset0 = new Vector2(-1, -1);
        public Vector2 Offset1 = new Vector2(-1, 1);
        public Vector2 Offset2 = new Vector2(1, -1);
        public Vector2 Offset3 = new Vector2(1, 1);

#if UNITY_EDITOR
        static private MaterialPropertyBlock s_SharedPropertyBlock;

        private void Update() {
            if (EditorApplication.isPlaying || !Frame.IsActive(this)) {
                return;
            }

            RegenerateMesh();
        }

        private void RegenerateMesh() {
            if (s_SharedPropertyBlock == null) {
                s_SharedPropertyBlock = new MaterialPropertyBlock();
            }

            MeshRenderer renderer = GetComponent<MeshRenderer>();
            renderer.GetPropertyBlock(s_SharedPropertyBlock);
            s_SharedPropertyBlock.SetColor("_RendererColor", Color);
            renderer.SetPropertyBlock(s_SharedPropertyBlock);
            renderer.sortingOrder = -10000;

            MeshData16<ComicMeshVertex> meshData = new MeshData16<ComicMeshVertex>(4, 6, MeshTopology.Triangles, false);
            meshData.AddQuad(new ComicMeshVertex() {
                Position = Offset0,
                PackedUVs = new Vector4(0.5f, 0.5f, 0, 0)
            }, new ComicMeshVertex() {
                Position = Offset1,
                PackedUVs = new Vector4(0.5f, 0.5f, 0, 0)
            }, new ComicMeshVertex() {
                Position = Offset2,
                PackedUVs = new Vector4(0.5f, 0.5f, 0, 0)
            }, new ComicMeshVertex() {
                Position = Offset3,
                PackedUVs = new Vector4(0.5f, 0.5f, 0, 0)
            });

            DynamicMeshFilter dynFilter = GetComponent<DynamicMeshFilter>();
            dynFilter.Upload(meshData, MeshDataUploadFlags.DontRecalculateBounds);
        }

        [CustomEditor(typeof(ComicMaskNode))]
        private sealed class Inspector : UnityEditor.Editor {
            private void OnSceneGUI() {
                ComicMaskNode node = (ComicMaskNode)target;
                Handles.matrix = node.transform.localToWorldMatrix;
                bool changed = DragPositionHandle(ref node.Offset0, ColorBank.Red, node);
                changed |= DragPositionHandle(ref node.Offset1, ColorBank.Yellow, node);
                changed |= DragPositionHandle(ref node.Offset2, ColorBank.Green, node);
                changed |= DragPositionHandle(ref node.Offset3, ColorBank.Blue, node);
                if (changed) {
                    node.RegenerateMesh();
                }
            }

            static private bool DragPositionHandle(ref Vector2 position, Color color, UnityEngine.Object obj) {
                Handles.color = color;

                Vector3 localPos = position;
                Vector3 newPos = Handles.FreeMoveHandle(localPos, 0.1f * HandleUtility.GetHandleSize(localPos), default, Handles.DotHandleCap);
                newPos.z = 0;

                if (newPos != localPos) {
                    Undo.RecordObject(obj, "Adjusting vertex position");
                    position.x = newPos.x;
                    position.y = newPos.y;
                    EditorUtility.SetDirty(obj);
                    return true;
                }

                return false;
            }
        }
#endif // UNITY_EDITOR
    }

    [Serializable]
    public struct MaskData {
        public PackedPoint P0;
        public PackedPoint P1;
        public PackedPoint P2;
        public PackedPoint P3;
        public ushort PackedColor;
    }
}