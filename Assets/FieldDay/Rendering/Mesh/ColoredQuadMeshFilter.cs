using UnityEngine;
using BeauUtil;
using System;

#if UNITY_EDITOR
using UnityEditor;
#endif // UNITY_EDITOR

namespace FieldDay.Rendering {
    [RequireComponent(typeof(DynamicMeshFilter))]
    [ExecuteAlways]
    public sealed class ColoredQuadMeshFilter : MonoBehaviour {
        public Color Color = Color.white; 
        public Vector3 Offset0;
        public Vector3 Offset1;
        public Vector3 Offset2;
        public Vector3 Offset3;

        public Sprite SourceSprite;

        [NonSerialized] private DynamicMeshFilter m_Filter;
        [NonSerialized] private RectUVs m_SpriteUVs;

        private void OnEnable() {
#if UNITY_EDITOR
            if (BuildPipeline.isBuildingPlayer) {
                return;
            }
#endif // UNITY_EDITOR

            RegenerateMesh();
        }

        public void RegenerateMesh() {
            MeshData16<SpriteVertex> buffer = RenderBuffers.GetSpriteBuffer();

            Vector2 texcoord = new Vector2(0.5f, 0.5f);
            if (SourceSprite != null) {
                texcoord = RectUVs.FromTextureRect(SourceSprite.texture, SourceSprite.textureRect).Center;
            }

            SpriteMeshUtility.AppendColoredQuad(buffer, Offset0, Offset1, Offset2, Offset3, texcoord, Color);

            this.CacheComponent(ref m_Filter).Upload(buffer);
            GetComponent<MeshFilter>().sharedMesh = m_Filter.Mesh;
        }

#if UNITY_EDITOR

        private void Update() {
            if (EditorApplication.isPlaying || !Frame.IsActive(this)) {
                return;
            }

            RegenerateMesh();
        }

        [CustomEditor(typeof(ColoredQuadMeshFilter))]
        private sealed class Inspector : UnityEditor.Editor {
            private void OnSceneGUI() {
                ColoredQuadMeshFilter node = (ColoredQuadMeshFilter)target;
                Handles.matrix = node.transform.localToWorldMatrix;
                bool changed = DragPositionHandle(ref node.Offset0, ColorBank.Red, node);
                changed |= DragPositionHandle(ref node.Offset1, ColorBank.Yellow, node);
                changed |= DragPositionHandle(ref node.Offset2, ColorBank.Green, node);
                changed |= DragPositionHandle(ref node.Offset3, ColorBank.Blue, node);
                if (changed) {
                    node.RegenerateMesh();
                }
            }

            static private bool DragPositionHandle(ref Vector3 position, Color color, UnityEngine.Object obj) {
                Handles.color = color;

                Vector3 localPos = position;
                Vector3 newPos = Handles.FreeMoveHandle(localPos, 0.1f * HandleUtility.GetHandleSize(localPos), default, Handles.DotHandleCap);

                if (newPos != localPos) {
                    Undo.RecordObject(obj, "Adjusting vertex position");
                    position.x = newPos.x;
                    position.y = newPos.y;
                    position.z = newPos.z;
                    EditorUtility.SetDirty(obj);
                    return true;
                }

                return false;
            }
        }

#endif // UNITY_EDITOR
    }
}