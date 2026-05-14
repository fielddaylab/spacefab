using BeauUtil;
using BeauUtil.Debugger;
using FieldDay;
using FieldDay.Assets;
using FieldDay.Scenes;
using System;
using UnityEditor;
using UnityEngine;

namespace SpaceFab.Comic {
    [RequireComponent(typeof(DynamicMeshFilter), typeof(MeshRenderer), typeof(SetRendererLayer))]
    [ExecuteAlways]
    public sealed class ComicLayerNode : MonoBehaviour, IEditModeOnly {
        [NonSerialized] public ushort CachedIndex;

        public Sprite Image;

        [Header("Animation Linkage")]
        public ComicLayerNode Sibling;

#if UNITY_EDITOR
        static private MaterialPropertyBlock s_SharedPropertyBlock;
        [NonSerialized] private int m_LastKnownImageId;

        private void OnDisable() {
            m_LastKnownImageId = 0;
        }

        private void Update() {
            if (EditorApplication.isPlaying || !Frame.IsActive(this)) {
                return;
            }

            int id = UnityHelper.Id(Image);
            if (m_LastKnownImageId != id) {
                m_LastKnownImageId = id;
                RegenerateMesh();
            }
        }

        private void RegenerateMesh() {
            if (s_SharedPropertyBlock == null) {
                s_SharedPropertyBlock = new MaterialPropertyBlock();
            }

            MeshRenderer renderer = GetComponent<MeshRenderer>();
            renderer.GetPropertyBlock(s_SharedPropertyBlock);
            s_SharedPropertyBlock.SetColor("_RendererColor", Color.white);
            renderer.sharedMaterial = renderer.sharedMaterial ?? AssetUtility.Editor.FindAsset<Material>("Sprites-Premultiplied");

            MeshData16<VertexP3U2> meshData = new MeshData16<VertexP3U2>(4, 6, MeshTopology.Triangles, false);
            if (Image) {
                if (Image.packed && Image.packingMode != SpritePackingMode.Rectangle) {
                    Log.Error("Sprite must be set to Full Rect packing mode");
                } else {
                    s_SharedPropertyBlock.SetTexture("_MainTex", Image.texture);

                    Vector2 texSize = new Vector2(Image.texture.width, Image.texture.height);
                    Rect uvRect = Image.textureRect;
                    uvRect.Set(uvRect.x / texSize.x, uvRect.y / texSize.y, uvRect.width / texSize.x, uvRect.height / texSize.y);
                    Bounds bounds = Image.bounds;

                    Vector2 min = bounds.min;
                    Vector2 max = bounds.max;

                    meshData.AddQuad(new VertexP3U2() {
                        Position = new Vector3(min.x, min.y, 0),
                        UV = new Vector2(uvRect.xMin, uvRect.yMin)
                    }, new VertexP3U2() {
                        Position = new Vector3(min.x, max.y, 0),
                        UV = new Vector2(uvRect.xMin, uvRect.yMax)
                    }, new VertexP3U2() {
                        Position = new Vector3(max.x, min.y, 0),
                        UV = new Vector2(uvRect.xMax, uvRect.yMin)
                    }, new VertexP3U2() {
                        Position = new Vector3(max.x, max.y, 0),
                        UV = new Vector2(uvRect.xMax, uvRect.yMax)
                    });
                }
            }
            renderer.SetPropertyBlock(s_SharedPropertyBlock);

            DynamicMeshFilter dynFilter = GetComponent<DynamicMeshFilter>();
            dynFilter.Upload(meshData);
            GetComponent<MeshFilter>().sharedMesh = dynFilter.Mesh;
        }
#endif // UNITY_EDITOR
    }

    [Serializable]
    public struct LayerData {
        public StringHash32 Id;
        public PackedPoint Position;
        public short PackedRotation;
        public LayerFlags Flags;
        public ushort MeshIndex;
        public ushort TextureIndex;
        public short RenderOrder;
        public ushort SiblingLayerIndex;
    }

    [Flags]
    public enum LayerFlags : ushort {
        FullyOpaque = 0x01
    }
}