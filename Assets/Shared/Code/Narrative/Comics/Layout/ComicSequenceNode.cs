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
using FieldDay.ImageSlicer;
using BeauPools;
using System.IO;
using FieldDay.Assets;

#if UNITY_EDITOR
using UnityEditor;
#endif // UNITY_EDITOR

namespace SpaceFab.Comic {
    public sealed class ComicSequenceNode : MonoBehaviour {
        public ComicSequenceManifest Manifest;
        [Range(8, 128)] public int SlicingTileSize = 16;
        [Range(0, 4)] public int TilePadding = 1;

        [Header("-- DIAGNOSTICS --")]
        [SerializeField] private ExportHashes m_ExportHashes;

        [Serializable]
        public struct ExportHashes {
            public int LastExportLayerCount;
            public string[] LastExportSpriteGuids;
            public Hash128[] LastExportSpriteTextureHashes;
            public ulong LastWrittenSettingsHash;
        }

#if UNITY_EDITOR

        [CustomEditor(typeof(ComicSequenceNode))]
        private class Inspector : Editor {
            public override void OnInspectorGUI() {
                base.OnInspectorGUI();

                GUILayout.Space(12);

                if (GUILayout.Button("Compile")) {
                    foreach(ComicSequenceNode node in targets) {
                        BuildManifest(node.transform, node.Manifest, ref node.m_ExportHashes, node, node.SlicingTileSize, node.TilePadding, false);
                    }
                }
                if (GUILayout.Button("Compile (Force Rebuild Textures)")) {
                    foreach (ComicSequenceNode node in targets) {
                        BuildManifest(node.transform, node.Manifest, ref node.m_ExportHashes, node, node.SlicingTileSize, node.TilePadding, true);
                    }
                }
            }
        }

        static public bool BuildManifest(Transform root, ComicSequenceManifest manifest, ref ExportHashes exportHashes, UnityEngine.Object source, int slicing, int padding, bool forceRebuildTextures) {
            if (!manifest) {
                Log.Error("[ComicSequenceNode] No node provided to export {0}", root.gameObject.name);
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
            builder.ExportedSequencePath = Baking.GetAssetDirectory(manifest) + "/" + manifest.name;

            using (Profiling.Time("Building Page Data")) {
                int childCount = root.childCount;
                for (int i = 0; i < childCount; i++) {
                    Transform child = root.GetChild(i);
                    if (!child.gameObject.activeSelf) {
                        continue;
                    }

                    if (child.TryGetComponent(out ComicPageNode page)) {
                        PageData pageData = BuildPageData(ref builder, page, builder.Pages.Count);
                        builder.Pages.Add(pageData);
                    }

                    if (child.TryGetComponent(out ComicCameraNode camera)) {
                        CameraData cameraData = BuildCameraData(ref builder, camera);
                        builder.Cameras.Add(cameraData);
                    }
                }
            }

            TilePackingSettings packingSettings = GetPackingSettings(slicing, padding);
            bool needToRebuildTextures = AreTexturesDirty(ref builder, ref exportHashes, packingSettings);
            if (needToRebuildTextures || forceRebuildTextures) {
                Baking.SetDirty(source);
                using (Profiling.Time("Generating Meshes and Packing Textures")) {
                    ScanAndPackLayers(ref builder, packingSettings);
                }
            } else {
                // TODO: ensure all layers have the correct mesh indices again
                for(int i = 0; i < builder.DiscoveredLayers.Count; i++) {
                    ComicLayerNode layer = builder.DiscoveredLayers[i];
                    builder.Layers[layer.CachedIndex].MeshIndex = manifest.Layers[layer.CachedIndex].MeshIndex;
                    builder.Layers[layer.CachedIndex].TextureIndex = manifest.Layers[layer.CachedIndex].TextureIndex;
                }
                Log.Msg("[ComicSequenceNode] No texture changes detected, skipping rebuild");
                for(int i = 0; i < manifest.Meshes.Length; i++) {
                    builder.Meshes.Add(manifest.Meshes[i]);
                }
                builder.OutputMeshes = manifest.CompressedMeshData;
                builder.OutputTextures = manifest.Textures;
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

            public string ExportedSequencePath;
            
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

        #region Nodes

        static private PageData BuildPageData(ref SequenceBuilder builder, ComicPageNode node, int pageIndex) {
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
                    PanelData panelData = BuildPanelData(ref builder, panel, pageIndex);
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

        static private PanelData BuildPanelData(ref SequenceBuilder builder, ComicPanelNode node, int pageIndex) {
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
                        LayerData layerData = BuildLayerData(ref builder, layer, pageIndex);
                        // CachedIndex must match the slot the layer lands in; assign after Add so
                        // later passes (ScanAndPackLayers, sibling linkage) write to the right entry.
                        builder.Layers.Add(layerData);
                        layer.CachedIndex = (ushort) (builder.Layers.Count - 1);
                        layer.CachedPageIndex = (ushort) pageIndex;
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

        static private LayerData BuildLayerData(ref SequenceBuilder builder, ComicLayerNode node, int pageIndex) {
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

        #endregion // Nodes

        static private bool AreTexturesDirty(ref SequenceBuilder builder, ref ExportHashes hashes, TilePackingSettings settings) {
            bool isDifferent = false;

            ulong hash = Unsafe.Hash64(settings);
            if (hashes.LastWrittenSettingsHash != hash) {
                hashes.LastWrittenSettingsHash = hash;
                isDifferent = true;
            }

            if (hashes.LastExportLayerCount != builder.Layers.Count) {
                hashes.LastExportLayerCount = builder.Layers.Count;
                isDifferent = true;
            }

            using (TempReferenceBuffer<Sprite> tempSprites = TempReferenceBuffer<Sprite>.Create(builder.DiscoveredLayers.Count)) {
                for (int i = 0; i < builder.DiscoveredLayers.Count; i++) {
                    ComicLayerNode node = builder.DiscoveredLayers[i];
                    if (node.Image) {
                        tempSprites.Add(node.Image);
                    }
                }

                string[] guids = new string[tempSprites.Count];
                for (int i = 0; i < tempSprites.Count; i++) {
                    AssetDatabase.TryGetGUIDAndLocalFileIdentifier(tempSprites[i], out guids[i], out long localId);
                    guids[i] += localId.ToString();
                }

                if (!ArrayUtils.ContentEquals(guids, hashes.LastExportSpriteGuids)) {
                    hashes.LastExportSpriteGuids = guids;
                    isDifferent = true;
                }

                Hash128[] texHashes = new Hash128[tempSprites.Count];
                for(int i = 0; i < tempSprites.Count; i++) {
                    texHashes[i] = tempSprites[i].texture.imageContentsHash;
                }

                if (!ArrayUtils.ContentEquals(texHashes, hashes.LastExportSpriteTextureHashes)) {
                    hashes.LastExportSpriteTextureHashes = texHashes;
                    isDifferent = true;
                }
            }

            return isDifferent;
        }

        #region Packing

        private const int MaxTextureSize = 4096;
        private const int MaxPaletteEntries = 4096;

        private const int MaxQuads = ushort.MaxValue / 6;

        static private TilePackingSettings GetPackingSettings(int slicing, int padding) {
            TilePackingSettings packingSettings;
            packingSettings.Padding = padding;
            packingSettings.PaletteTileSize = 4;
            packingSettings.TileSize = slicing;
            return packingSettings;
        }

        static private unsafe void ScanAndPackLayers(ref SequenceBuilder builder, TilePackingSettings packingSettings) {
            // TODO: tile packing, setting flags

            WorkList<Sprite> discoveredSprites = new WorkList<Sprite>(builder.DiscoveredLayers.Count);
            WorkList<CondensedMesh> condensedMeshes = new WorkList<CondensedMesh>(builder.DiscoveredLayers.Count);

            var arena = Unsafe.CreateArena(Unsafe.MiB * 800);
            var blob = arena.AllocSpan<byte>(Unsafe.MiB * 8);
            var localBlob = arena.AllocSpan<byte>(Unsafe.MiB * 3);
            try {
                ImageSlicingBuffer* imageSlicer = arena.AllocArray<ImageSlicingBuffer>(1);
                TileCondenserBuffer* tileCondenser = arena.AllocArray<TileCondenserBuffer>(1);
                TileExporter* tileExporter = arena.AllocArray<TileExporter>(1);

                ImageSlicingBuffer.Initialize(imageSlicer, 2048, packingSettings.TileSize, packingSettings.TileSize, arena);
                TileCondenserBuffer.Initialize(tileCondenser, (MaxTextureSize / packingSettings.TileSize) * (MaxTextureSize / packingSettings.TileSize), MaxPaletteEntries, packingSettings.TileSize, arena);

                // always commit a white and black palette entries (for use later)
                TileCondenserBuffer.CommitPaletteEntry(tileCondenser, new PixelRGBA32(0xFFFFFFFF));
                TileCondenserBuffer.CommitPaletteEntry(tileCondenser, new PixelRGBA32(0, 0, 0, 255));

                ByteWriter finalBlobWriter = new ByteWriter(blob.Ptr, blob.Length);
                ByteWriter localWriter = new ByteWriter(localBlob.Ptr, localBlob.Length);

                TileCondenserTransferStats transferStats = default;
                TileCondenserStats condenserStats = default;

                long srcPixels = 0;

                using (Profiling.Time("slicing images")) {
                    for (int i = 0, len = builder.DiscoveredLayers.Count; i < len; i++) {
                        ComicLayerNode layer = builder.DiscoveredLayers[i];
                        ref LayerData data = ref builder.Layers[layer.CachedIndex];
                        if (layer.Image) {
                            Sprite sprite = layer.Image;
                            Texture2D texture = sprite.texture;

                            int meshIndex = discoveredSprites.IndexOf(sprite);
                            if (meshIndex < 0) {
                                meshIndex = discoveredSprites.Count;
                                discoveredSprites.Add(sprite);

                                srcPixels += texture.width * texture.height;

                                try {
                                    EditorUtility.DisplayProgressBar("Slicing image...", texture.name, 0);
                                    bool processed = TileUtility.ProcessImageIntoSlices(imageSlicer, texture, packingSettings, out ImageInstance image);
                                    Assert.True(processed, "Image was unable to be processed!");
                                    EditorUtility.DisplayProgressBar("Condensing tiles...", texture.name, 0.25f);
                                    TileUtility.CondenseTilesFromSlices(imageSlicer, &image, tileCondenser, arena, ref transferStats, out CondensedMesh mesh);
                                    meshIndex = condensedMeshes.Count;
                                    condensedMeshes.Add(mesh);
                                } finally {
                                    EditorUtility.ClearProgressBar();
                                }
                            }

                            data.TextureIndex = 0;
                            data.MeshIndex = (ushort) meshIndex;
                        }
                    }
                }

                Texture2D exportedTexture;

                using (Profiling.Time("exporting texture")) {
                    bool canExport = TileExporter.Initialize(tileExporter, tileCondenser, packingSettings, arena);
                    if (!canExport) {
                        // TODO: do something
                        Assert.Fail("cannot export");
                    }

                    exportedTexture = TileUtility.CreateExportTexture(tileExporter);
                    bool wroteToTexture = TileUtility.WriteTilesToTexture(tileCondenser, tileExporter, exportedTexture);

                    byte[] exportedPNG = exportedTexture.EncodeToPNG();
                    string pngPath = builder.ExportedSequencePath + "_tex.png";

                    DestroyImmediate(exportedTexture);

                    File.WriteAllBytes(pngPath, exportedPNG);

                    AssetDatabase.ImportAsset(pngPath, ImportAssetOptions.ForceSynchronousImport);

                    TextureImporter importer = (TextureImporter) TextureImporter.GetAtPath(pngPath);
                    importer.maxTextureSize = TileUtility.MaxExportSize;
                    importer.mipmapEnabled = false;
                    importer.wrapMode = TextureWrapMode.Clamp;
                    importer.SaveAndReimport();

                    exportedTexture = AssetDatabase.LoadAssetAtPath<Texture2D>(pngPath);
                }

                long uncompressedSize = 0;

                using (Profiling.Time("writing meshes")) {

                    for (int i = 0; i < condensedMeshes.Count; i++) {
                        CondensedMesh mesh = condensedMeshes[i];
                        Sprite sprite = discoveredSprites[i];
                        Rect originalSpace = sprite.rect;
                        Vector2 pivot = sprite.pivot;
                        pivot.x /= originalSpace.width;
                        pivot.y /= originalSpace.height;

                        Rect worldBounds = TileUtility.ComputeMeshBounds(mesh, sprite.pixelsPerUnit, pivot);

                        Vector2 min = worldBounds.min;
                        Vector2 range = worldBounds.size;

                        Bounds originalBounds = sprite.bounds;
                        Bounds newBounds = new Bounds();
                        newBounds.SetMinMax(min, min + range);

                        Assert.True(Mathf.Approximately(originalBounds.min.x, newBounds.min.x) && Mathf.Approximately(originalBounds.min.y, newBounds.min.y), "Computed bounds do not line up with original bounds");

                        // range
                        localWriter.Write(min);
                        localWriter.Write(range);

                        CompressedMeshVertex vertA, vertB, vertC, vertD;
                        int meshTileCount = mesh.TileCount;
                        int meshTileSize = mesh.Condenser->TileSize;

                        // TODO: condense rectlinear single-color regions into larger quads

                        //Log.Msg("mesh {0} has {1} quads", i, meshTileCount);

                        Assert.True(meshTileCount <= MaxQuads, "Mesh {0} has too many quads ({1} vs {2})", i, meshTileCount, MaxQuads);

                        //// vertices
                        for (int meshTileIdx = 0; meshTileIdx < meshTileCount; meshTileIdx++) {
                            CondensedMeshTile meshTile = mesh.Tiles[meshTileIdx];

                            Vector4 pos = TileUtility.ComputeMeshTilePositions(mesh, meshTile);

                            //Log.Msg("quad: ({0}, {1}) -> ({2}, {3})", x0, y0, x1, y1);

                            vertA.X = (ushort) (pos.x * ComicMesh.PositionMultiplier);
                            vertB.X = (ushort) (pos.x * ComicMesh.PositionMultiplier);
                            vertC.X = (ushort) (pos.z * ComicMesh.PositionMultiplier);
                            vertD.X = (ushort) (pos.z * ComicMesh.PositionMultiplier);

                            vertA.Y = (ushort) (pos.y * ComicMesh.PositionMultiplier);
                            vertB.Y = (ushort) (pos.w * ComicMesh.PositionMultiplier);
                            vertC.Y = (ushort) (pos.y * ComicMesh.PositionMultiplier);
                            vertD.Y = (ushort) (pos.w * ComicMesh.PositionMultiplier);

                            Vector4 uvs = TileUtility.ComputeMeshTileTexCoords(meshTile, tileExporter);

                            vertA.U = (ushort) (uvs.x * ComicMesh.UVMultiplier);
                            vertB.U = (ushort) (uvs.x * ComicMesh.UVMultiplier);
                            vertC.U = (ushort) (uvs.z * ComicMesh.UVMultiplier);
                            vertD.U = (ushort) (uvs.z * ComicMesh.UVMultiplier);

                            vertA.V = (ushort) (uvs.y * ComicMesh.UVMultiplier);
                            vertB.V = (ushort) (uvs.w * ComicMesh.UVMultiplier);
                            vertC.V = (ushort) (uvs.y * ComicMesh.UVMultiplier);
                            vertD.V = (ushort) (uvs.w * ComicMesh.UVMultiplier);

                            localWriter.Write(vertA);
                            localWriter.Write(vertB);
                            localWriter.Write(vertC);
                            localWriter.Write(vertD);
                        }

                        for (int meshTileIdx = 0; meshTileIdx < meshTileCount; meshTileIdx++) {
                            //// indices (0, 1, 2, 3, 2, 1) (0, +1, +1, +1, -1, -1)
                            if (meshTileIdx == 0) {
                                localWriter.Write((ushort) 0);
                            } else {
                                localWriter.Write((sbyte) 3);
                            }

                            localWriter.Write((sbyte) 1);
                            localWriter.Write((sbyte) 1);
                            localWriter.Write((sbyte) 1);
                            localWriter.Write((sbyte) -1);
                            localWriter.Write((sbyte) -1);
                        }


                        ComicMeshHeader header;
                        header.VertexCount = (ushort) (meshTileCount * 4);
                        header.IndexCount = (ushort) (meshTileCount * 6);

                        header.BinaryOffset = finalBlobWriter.GetMarker();

                        UnsafeSpan<byte> meshData = localWriter.GetData();
                        var compressResult = LZCompression.Compress(meshData.Ptr, (uint) meshData.Length, finalBlobWriter.Head, finalBlobWriter.GetRemaining(), out uint compressedSize);
                        Assert.False(LZCompression.IsError(compressResult));
                        finalBlobWriter.Skip((int) compressedSize);

                        header.BinaryLength = compressedSize;

                        builder.Meshes.Add(header);

                        uncompressedSize += localWriter.Written;
                        localWriter.Reset();
                    }

                }

                TileUtility.ComputeCondenserStats(tileCondenser, out condenserStats);
                using (PooledStringBuilder psb = PooledStringBuilder.Create()) {
                    psb.Builder.Append("Condensed ").AppendNoAlloc(discoveredSprites.Count).Append(" textures into ")
                        .AppendNoAlloc(condenserStats.TotalUniqueContentTiles).Append(" unique tiles and ")
                        .AppendNoAlloc(condenserStats.TotalUniquePaletteTiles).Append(" unique colors (").AppendNoAlloc(packingSettings.TileSize).Append("px tiles)");

                    uint percentEmpty = 100 * transferStats.TotalEmptyTiles / transferStats.TotalProcessedTiles;
                    uint percentColor = 100 * transferStats.TotalPaletteTiles / transferStats.TotalProcessedTiles;
                    uint percentContent = 100 - (percentEmpty + percentColor);
                    psb.Builder.Append("\n").AppendNoAlloc(percentEmpty).Append("% empty, ").AppendNoAlloc(percentColor).Append("% color, ").AppendNoAlloc(percentContent).Append("% content");
                    psb.Builder.Append("\n").AppendNoAlloc(condenserStats.TotalContentReused).Append(" tiles reused at least once, ").AppendNoAlloc(condenserStats.TotalContentReusedTransformed).Append(" tiles reused with a transformation");

                    long finalPixels = tileExporter->TextureWidth * tileExporter->TextureHeight;

                    uint percentChange = (uint) (100 * finalPixels / srcPixels);

                    psb.Builder.Append("\nFinal Texture Size: ").AppendNoAlloc(tileExporter->TextureWidth).Append("x").AppendNoAlloc(tileExporter->TextureHeight)
                        .Append(", ");
                    Unsafe.FormatBytes(finalPixels * 4, psb.Builder);
                    psb.Builder.Append(", ").AppendNoAlloc(percentChange).Append("% of original source images ");
                    Unsafe.FormatBytes(srcPixels * 4, psb.Builder);
                    psb.Builder.Append("\nMesh Blob: ");
                    Unsafe.FormatBytes(finalBlobWriter.Written, psb.Builder);
                    psb.Builder.Append(", ").AppendNoAlloc((int) (100 * finalBlobWriter.Written / uncompressedSize))
                        .Append("% of uncompressed size ");
                    Unsafe.FormatBytes(uncompressedSize, psb.Builder);

                    //psb.Builder.Append("\nCondensed from ");
                    //Unsafe.FormatBytes(totalTexturePixels * 4, psb);
                    //psb.Builder.Append(" to ");
                    //Unsafe.FormatBytes(totalResultPixels * 4, psb);

                    Log.Msg(psb.Builder.Flush());
                }

                builder.OutputMeshes = finalBlobWriter.GetDataCopy();
                builder.OutputTextures = new Texture2D[] { exportedTexture };
            } finally {
                Unsafe.DestroyArena(arena);
            }
        }

        static private unsafe void ExportDebugTiles(TileCondenserBuffer* condenserBuffer) {
            Directory.CreateDirectory("Temp/ImageSlicerDEBUG/");

            int tilePixelSize = condenserBuffer->TilePixelSize;
            int tileDimensions = condenserBuffer->TileSize;

            Texture2D tempTex = new Texture2D(tileDimensions, tileDimensions, TextureFormat.RGBA32, false);
            var pixelData = tempTex.GetPixelData<PixelRGBA32>(0);
            PixelRGBA32* dstBuffer = Unsafe.NativePointer(pixelData);
            PixelRGBA32* tileBuffer;
            try {
                for (int i = 0; i < condenserBuffer->TileCount; i++) {
                    EditorUtility.DisplayProgressBar("Exporting debug tile textures...", i.ToStringLookup(), i / (float) condenserBuffer->TileCount);
                    tileBuffer = condenserBuffer->TileColorBuffer + i * tilePixelSize;
                    Unsafe.FastCopyArray(tileBuffer, tilePixelSize, dstBuffer);
                    byte[] pngData = tempTex.EncodeToPNG();
                    TileReuseStats reuseStats = condenserBuffer->TileStatsBuffer[i];
                    File.WriteAllBytes(string.Format("Temp/ImageSlicerDEBUG/{0}-reused{1}.png", i, reuseStats.DirectReuse + reuseStats.TransformedReuse), pngData);
                }
            } finally {
                pixelData.Dispose();
                DestroyImmediate(tempTex);
                EditorUtility.ClearProgressBar();
            }
        }

        #endregion // Packing

#endif // UNITY_EDITOR
    }
}