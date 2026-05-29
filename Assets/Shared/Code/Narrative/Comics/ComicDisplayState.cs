using BeauRoutine;
using BeauUtil.Debugger;
using BeauUtil.UI;
using FieldDay;
using FieldDay.Collections;
using FieldDay.SharedState;
using FieldDay.UI;
using SpaceFab.UI;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using UnityEngine;

namespace SpaceFab.Comic
{
    public class ComicDisplayState : SharedStateComponent
    {
        [NonSerialized] public int CurrentPageIndex = -1;
        [NonSerialized] public int CurrentPanelIndex = -1;

        public GameObject NextButtonGroup;
        public AutoSizedButton NextButton;
    }

    static public partial class ComicsUtility {
        static public void PreloadPage(int pageIndex) {
            ComicSequenceManifest manifest = Manifest;
            Assert.NotNullOrDestroyed(manifest);
            Assert.True(pageIndex >= 0 && pageIndex < manifest.Pages.Length);

            var panelRange = manifest.Pages[pageIndex].Panels;
            for(int i = panelRange.Offset; i < panelRange.End; i++) {
                PreloadPanel(manifest, manifest.Panels[i]);
            }
        }

        static public void CancelPreloadPage(int pageIndex) {
            ComicSequenceManifest manifest = Manifest;
            Assert.NotNullOrDestroyed(manifest);
            Assert.True(pageIndex >= 0 && pageIndex < manifest.Pages.Length);

            var panelRange = manifest.Pages[pageIndex].Panels;
            for (int i = panelRange.Offset; i < panelRange.End; i++) {
                CancelPreloadPanel(manifest, manifest.Panels[i]);
            }
        }

        static private void PreloadPanel(ComicSequenceManifest manifest, in PanelData panel) {
            if (panel.MaskIndex != ComicMesh.NullIndex) {
                PreloadMesh(panel.MaskIndex, StreamedMeshType.Mask);
            }

            var layerRange = panel.Layers;
            for(int i = layerRange.Offset; i < layerRange.End; i++) {
                PreloadLayer(manifest, manifest.Layers[i]);
            }
        }

        static private void CancelPreloadPanel(ComicSequenceManifest manifest, in PanelData panel) {
            if (panel.MaskIndex != ComicMesh.NullIndex) {
                CancelMeshPreload(panel.MaskIndex, StreamedMeshType.Mask);
            }

            var layerRange = panel.Layers;
            for (int i = layerRange.Offset; i < layerRange.End; i++) {
                CancelPreloadLayer(manifest, manifest.Layers[i]);
            }
        }

        static private void PreloadLayer(ComicSequenceManifest manifest, in LayerData layer) {
            if (layer.MeshIndex != ComicMesh.NullIndex) {
                PreloadMesh(layer.MeshIndex, StreamedMeshType.Layer);
            }
            if (layer.TextureIndex != ushort.MaxValue) {
                // TODO: texture upload? ensure it's on the gpu
            }
        }

        static private void CancelPreloadLayer(ComicSequenceManifest manifest, in LayerData layer) {
            if (layer.MeshIndex != ComicMesh.NullIndex) {
                CancelMeshPreload(layer.MeshIndex, StreamedMeshType.Layer);
            }
        }

        static public IEnumerator DisplayAndWaitForNextButton() {
            Find.State(out ComicDisplayState displayState);
            GuiCommands.SetActive(displayState.NextButtonGroup, true);
            while(!displayState.NextButton.ConsumeClick()) {
                yield return null;
            }
            GuiCommands.SetActive(displayState.NextButtonGroup, false);
        }
    }
}