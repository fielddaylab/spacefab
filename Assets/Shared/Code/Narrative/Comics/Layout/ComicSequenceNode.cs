using BeauUtil;
using BeauUtil.Debugger;
using FieldDay.Collections;
using FieldDay.Mathematics;
using ScriptableBake;
using SpaceFab.Design;
using System;
using System.Collections.Generic;
using UnityEngine;
using FieldDay.Data;


#if UNITY_EDITOR
using UnityEditor;
#endif // UNITY_EDITOR

namespace SpaceFab.Comic {
    public sealed class ComicSequenceNode : MonoBehaviour {
        public ComicSequenceManifest Manifest;

#if UNITY_EDITOR

        [CustomEditor(typeof(ComicSequenceNode))]
        private class Inspector : Editor {
            public override void OnInspectorGUI() {
                base.OnInspectorGUI();

                GUILayout.Space(12);
                if (GUILayout.Button("Compile")) {
                    foreach(ComicSequenceNode node in targets) {
                        BuildManifest(node.transform, node.Manifest);
                    }
                }
            }
        }

        static public bool BuildManifest(Transform root, ComicSequenceManifest manifest) {
            if (!manifest) {
                Log.Error("[ComicSequenceNode] No manifest provided to export {0}", root.gameObject.name);
                return false;
            }

            SequenceBuilder builder;
            builder.Pages = new WorkList<PageData>(8);
            builder.Panels = new WorkList<PanelData>(48);
            builder.Layers = new WorkList<LayerData>(128);
            builder.Cameras = new WorkList<CameraData>(24);
            builder.Masks = new WorkList<MaskData>(48);
            builder.Meshes = new WorkList<ComicMeshHeader>(128);
            builder.DiscoveredLayers = new WorkList<ComicLayerNode>(128);
            builder.OutputMeshes = null;
            builder.OutputTextures = null;

            using (Profiling.Time("Building Page Data")) {
                int childCount = root.childCount;
                for (int i = 0; i < childCount; i++) {
                    Transform child = root.GetChild(i);
                    if (!child.gameObject.activeSelf) {
                        continue;
                    }

                    if (child.TryGetComponent(out ComicPageNode page)) {
                        PageData pageData = BuildPageData(ref builder, page);
                        builder.Pages.Add(pageData);
                    }

                    if (child.TryGetComponent(out ComicCameraNode camera)) {
                        CameraData cameraData = BuildCameraData(ref builder, camera);
                        builder.Cameras.Add(cameraData);
                    }
                }
            }

            using (Profiling.Time("Generating Meshes and Packing Textures")) {
                ScanAndPackLayers(ref builder);
            }

            Assert.True(builder.Pages.Count <= ComicResourceUtility.MaxPages, "Cannot exceed " + ComicResourceUtility.MaxPages + " pages per comic");

            Baking.SetDirty(manifest);

            manifest.Pages = builder.Pages.ToArray();
            manifest.Panels = builder.Panels.ToArray();
            manifest.Layers = builder.Layers.ToArray();
            manifest.Cameras = builder.Cameras.ToArray();
            manifest.Masks = builder.Masks.ToArray();
            manifest.Meshes = builder.Meshes.ToArray();
            manifest.CompressedMeshData = builder.OutputMeshes;
            manifest.Textures = builder.OutputTextures;

            return true;
        }

        private struct SequenceBuilder {
            public WorkList<PageData> Pages;
            public WorkList<PanelData> Panels;
            public WorkList<LayerData> Layers;
            public WorkList<CameraData> Cameras;
            public WorkList<MaskData> Masks;
            public WorkList<ComicMeshHeader> Meshes;

            public WorkList<ComicLayerNode> DiscoveredLayers;
            
            public Texture2D[] OutputTextures;
            public byte[] OutputMeshes;
        }

        static private short PackDegrees(float degrees) {
            return FixedPoint.Q9_6.FromFloat(degrees);
        }

        static private ushort PackColorRGB565(Color c) {
            return (ushort) (((ushort)(c.r * 0x1F) << 11)
                | ((ushort)(c.g * 0x3F) << 5)
                | ((ushort)(c.b * 0x1F)));
        }

        static private PageData BuildPageData(ref SequenceBuilder builder, ComicPageNode node) {
            PageData pageData;

            node.transform.GetLocalPositionAndRotation(out Vector3 pos, out Quaternion rot);

            pageData.Position = new PackedPoint() {
                PackedX = FixedPoint.Q12_3.FromFloat(pos.x),
                PackedY = FixedPoint.Q12_3.FromFloat(pos.y),
            };
            pageData.PackedRotation = PackDegrees(rot.eulerAngles.z);

            ushort panelStart = (ushort) builder.Panels.Count;
            ushort panelCount = 0;

            ushort cameraStart = (ushort) builder.Cameras.Count;
            ushort cameraCount = 0;

            int childCount = node.transform.childCount;
            for (int i = 0; i < childCount; i++) {
                Transform child = node.transform.GetChild(i);
                if (!child.gameObject.activeSelf) {
                    continue;
                }

                if (child.TryGetComponent(out ComicPanelNode panel)) {
                    PanelData panelData = BuildPanelData(ref builder, panel);
                    builder.Panels.Add(panelData);
                    panelCount++;
                }
                if (child.TryGetComponent(out ComicCameraNode camera)) {
                    CameraData cameraData = BuildCameraData(ref builder, camera);
                    builder.Cameras.Add(cameraData);
                    cameraCount++;
                }
            }

            pageData.Panels = new OffsetLengthU16(panelStart, panelCount);
            pageData.Cameras = new OffsetLengthU16(cameraStart, cameraCount);

            return pageData;
        }

        static private PanelData BuildPanelData(ref SequenceBuilder builder, ComicPanelNode node) {
            PanelData panelData;

            node.transform.GetLocalPositionAndRotation(out Vector3 pos, out Quaternion rot);

            panelData.Id = node.gameObject.name;
            panelData.Position = new PackedPoint() {
                PackedX = FixedPoint.Q9_6.FromFloat(pos.x),
                PackedY = FixedPoint.Q9_6.FromFloat(pos.y),
            };
            panelData.PackedRotation = PackDegrees(rot.eulerAngles.z);

            ushort maskIndex = ushort.MaxValue;

            ushort layerStart = (ushort) builder.Layers.Count;
            ushort layerCount = 0;

            using (TempReferenceBuffer<ComicLayerNode> lateLinkLayers = TempReferenceBuffer<ComicLayerNode>.Create(4)) {
                int childCount = node.transform.childCount;
                for (int i = 0; i < childCount; i++) {
                    Transform child = node.transform.GetChild(i);
                    if (!child.gameObject.activeSelf) {
                        continue;
                    }

                    if (child.TryGetComponent(out ComicLayerNode layer)) {
                        LayerData layerData = BuildLayerData(ref builder, layer);
                        layer.CachedIndex = (ushort)builder.Layers.Count;
                        builder.Layers.Add(layerData);
                        layerCount++;

                        builder.DiscoveredLayers.Add(layer);

                        if (layer.Sibling != null) {
                            if (layer.Sibling.transform.parent != node.transform) {
                                Log.Error("[ComicSequenceManifest] Layer siblings must be siblings in hierarchy");
                            } else if (layer.Sibling.gameObject.activeSelf) {
                                lateLinkLayers.Add(layer);
                            }
                        }
                    }
                    if (child.TryGetComponent(out ComicMaskNode mask)) {
                        if (maskIndex != ushort.MaxValue) {
                            Log.Warn("[ComicSequenceManifest] Multiple masks detected for panel {0} - only using first mask", node.gameObject.name);
                        } else {
                            MaskData maskData = BuildMaskData(ref builder, mask);
                            maskIndex = (ushort)builder.Masks.Count;
                            builder.Masks.Add(maskData);
                        }
                    }
                }

                for(int i = 0; i < lateLinkLayers.Count; i++) {
                    ComicLayerNode layer = lateLinkLayers[i];
                    builder.Layers[layer.CachedIndex].SiblingLayerIndex = layer.Sibling.CachedIndex;
                }
            }

            panelData.Layers = new OffsetLengthU16(layerStart, layerCount);
            panelData.MaskIndex = maskIndex;

            return panelData;
        }

        static private LayerData BuildLayerData(ref SequenceBuilder builder, ComicLayerNode node) {
            LayerData layerData;

            node.transform.GetLocalPositionAndRotation(out Vector3 pos, out Quaternion rot);

            layerData.Id = node.gameObject.name;
            layerData.Position = new PackedPoint() {
                PackedX = FixedPoint.Q9_6.FromFloat(pos.x),
                PackedY = FixedPoint.Q9_6.FromFloat(pos.y),
            };
            layerData.PackedRotation = PackDegrees(rot.eulerAngles.z);

            // flags, mesh, and texture will be determined in second pass

            layerData.RenderOrder = 0;
            layerData.Flags = 0;
            layerData.MeshIndex = ComicMesh.NullIndex;
            layerData.TextureIndex = ushort.MaxValue;
            layerData.SiblingLayerIndex = ushort.MaxValue;

            return layerData;
        }

        static private CameraData BuildCameraData(ref SequenceBuilder builder, ComicCameraNode node) {
            CameraData cameraData;

            node.transform.GetPositionAndRotation(out Vector3 pos, out Quaternion rot);

            cameraData.Id = node.gameObject.name;
            cameraData.Position = new PackedPoint() {
                PackedX = FixedPoint.Q12_3.FromFloat(pos.x),
                PackedY = FixedPoint.Q12_3.FromFloat(pos.y),
            };
            cameraData.PackedRotation = PackDegrees(rot.eulerAngles.z);
            cameraData.PackedBackgroundColor = PackColorRGB565(node.BackgroundColor);

            cameraData.PackedClipHeight = FixedPoint.Q11_4.FromFloat(node.ClipHeight);

            return cameraData;
        }

        static private MaskData BuildMaskData(ref SequenceBuilder builder, ComicMaskNode node) {
            MaskData maskData;

            node.transform.GetLocalPositionAndRotation(out Vector3 pos, out Quaternion rot);

            Vector2 basePos = (Vector2)pos;
            Vector2 p0 = basePos + (Vector2) (rot * node.Offset0);
            Vector2 p1 = basePos + (Vector2)(rot * node.Offset1);
            Vector2 p2 = basePos + (Vector2)(rot * node.Offset2);
            Vector2 p3 = basePos + (Vector2)(rot * node.Offset3);

            maskData.P0 = new PackedPoint() {
                PackedX = FixedPoint.Q9_6.FromFloat(p0.x),
                PackedY = FixedPoint.Q9_6.FromFloat(p0.y)
            };

            maskData.P1 = new PackedPoint() {
                PackedX = FixedPoint.Q9_6.FromFloat(p1.x),
                PackedY = FixedPoint.Q9_6.FromFloat(p1.y)
            };

            maskData.P2 = new PackedPoint() {
                PackedX = FixedPoint.Q9_6.FromFloat(p2.x),
                PackedY = FixedPoint.Q9_6.FromFloat(p2.y)
            };

            maskData.P3 = new PackedPoint() {
                PackedX = FixedPoint.Q9_6.FromFloat(p3.x),
                PackedY = FixedPoint.Q9_6.FromFloat(p3.y)
            };

            maskData.PackedColor = PackColorRGB565(node.Color);

            return maskData;
        }

        static private unsafe void ScanAndPackLayers(ref SequenceBuilder builder) {
            // TODO: tile packing, setting flags

            WorkList<Texture2D> textures = new WorkList<Texture2D>(builder.DiscoveredLayers.Count);
            var blob = Unsafe.AllocSpan<byte>(Unsafe.MiB * 8);
            var localBlob = Unsafe.AllocSpan<byte>(Unsafe.MiB * 3);
            try {
                ByteWriter finalBlobWriter = new ByteWriter(blob.Ptr, blob.Length);
                ByteWriter localWriter = new ByteWriter(localBlob.Ptr, localBlob.Length);

                for (int i = 0, len = builder.DiscoveredLayers.Count; i < len; i++) {
                    ComicLayerNode layer = builder.DiscoveredLayers[i];
                    ref LayerData data = ref builder.Layers[layer.CachedIndex];
                    if (layer.Image) {
                        Texture2D texture = layer.Image.texture;
                        int textureIndex = textures.IndexOf(texture);
                        if (textureIndex < 0) {
                            textureIndex = textures.Count;
                            textures.Add(texture);
                        }

                        data.TextureIndex = (ushort) textureIndex;

                        Vector2 texSize = new Vector2(texture.width, texture.height);
                        Rect uvRect = layer.Image.textureRect;
                        uvRect.Set(uvRect.x / texSize.x, uvRect.y / texSize.y, uvRect.width / texSize.x, uvRect.height / texSize.y);
                        Bounds bounds = layer.Image.bounds;

                        Vector2 min = bounds.min;
                        Vector2 max = bounds.max;
                        Vector2 range = max - min;

                        // range
                        localWriter.Write(min);
                        localWriter.Write(range);

                        // vertices
                        CompressedMeshVertex vertA, vertB, vertC, vertD;
                        vertA.X = (ushort) ((min.x - min.x) / range.x * ComicMesh.PositionMultiplier);
                        vertA.Y = (ushort) ((min.y - min.y) / range.y * ComicMesh.PositionMultiplier);
                        vertA.U = (ushort) (uvRect.xMin * ComicMesh.UVMultiplier);
                        vertA.V = (ushort) (uvRect.yMin * ComicMesh.UVMultiplier);

                        vertB.X = (ushort) ((min.x - min.x) / range.x * ComicMesh.PositionMultiplier);
                        vertB.Y = (ushort) ((max.y - min.y) / range.y * ComicMesh.PositionMultiplier);
                        vertB.U = (ushort) (uvRect.xMin * ComicMesh.UVMultiplier);
                        vertB.V = (ushort) (uvRect.yMax * ComicMesh.UVMultiplier);

                        vertC.X = (ushort) ((max.x - min.x) / range.x * ComicMesh.PositionMultiplier);
                        vertC.Y = (ushort) ((min.y - min.y) / range.y * ComicMesh.PositionMultiplier);
                        vertC.U = (ushort) (uvRect.xMax * ComicMesh.UVMultiplier);
                        vertC.V = (ushort) (uvRect.yMin * ComicMesh.UVMultiplier);

                        vertD.X = (ushort) ((max.x - min.x) / range.x * ComicMesh.PositionMultiplier);
                        vertD.Y = (ushort) ((max.y - min.y) / range.y * ComicMesh.PositionMultiplier);
                        vertD.U = (ushort) (uvRect.xMax * ComicMesh.UVMultiplier);
                        vertD.V = (ushort) (uvRect.yMax * ComicMesh.UVMultiplier);

                        localWriter.Write(vertA);
                        localWriter.Write(vertB);
                        localWriter.Write(vertC);
                        localWriter.Write(vertD);

                        // indices (0, 1, 2, 3, 2, 1) (0, +1, +1, +1, -1, -1)
                        localWriter.Write((ushort) 0);
                        localWriter.Write((sbyte) 1);
                        localWriter.Write((sbyte) 1);
                        localWriter.Write((sbyte) 1);
                        localWriter.Write((sbyte) -1);
                        localWriter.Write((sbyte) -1);

                        ComicMeshHeader header;
                        header.VertexCount = 4;
                        header.IndexCount = 6;

                        header.BinaryOffset = finalBlobWriter.GetMarker();

                        UnsafeSpan<byte> meshData = localWriter.GetData();
                        var compressResult = LZCompression.Compress(meshData.Ptr, (uint) meshData.Length, finalBlobWriter.Head, finalBlobWriter.GetRemaining(), out uint size);
                        Assert.False(LZCompression.IsError(compressResult));
                        finalBlobWriter.Skip((int) size);

                        header.BinaryLength = size;

                        int meshIndex = builder.Meshes.Count;
                        builder.Meshes.Add(header);
                        data.MeshIndex = (ushort) meshIndex;

                        localWriter.Reset();
                    }
                }

                builder.OutputMeshes = finalBlobWriter.GetDataCopy();
                builder.OutputTextures = textures.ToArray();
            } finally {
                Unsafe.Free(blob.Ptr);
                Unsafe.Free(localBlob.Ptr);
            }
        }
#endif // UNITY_EDITOR
    }
}