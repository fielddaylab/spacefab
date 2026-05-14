using BeauPools;
using BeauRoutine;
using BeauUtil;
using BeauUtil.Debugger;
using FieldDay;
using FieldDay.Collections;
using FieldDay.SharedState;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using UnityEngine;

namespace SpaceFab.Comic
{
    public class ComicLayoutState : SharedStateComponent
    {
        [NonSerialized] public Transform[] PageHierarchies = new Transform[ComicResourceUtility.MaxPages];
        [NonSerialized] public BitSet64 AllocatedPageMask;
    }

    static public partial class ComicResourceUtility {
        public const int MaxPages = 32;

        static public void AllocatePageHierarchy(int pageIndex) {
            Find.State(out ComicResourcePool resourcePool, out ComicLayoutState layout);
            ComicSequenceManifest manifest = ComicsUtility.Manifest;
            Assert.NotNullOrDestroyed(manifest);
            Assert.True(pageIndex >= 0 && pageIndex < manifest.Pages.Length);

            if (!layout.AllocatedPageMask.IsSet(pageIndex)) {
                Log.Msg("[ComicsUtility] Spawning hierarchy for page {0}", pageIndex);
                PageData pageData = manifest.Pages[pageIndex];
                Transform root = SpawnPageTransform(resourcePool, pageData, pageIndex);
                layout.PageHierarchies[pageIndex] = root;
                var panelRange = pageData.Panels;
                for(int i = panelRange.Offset; i < panelRange.End; i++) {
                    SpawnPanelTransform(resourcePool, root, manifest.Panels[i], i - panelRange.Offset);
                }
                layout.AllocatedPageMask.Set(pageIndex);
            }
        }

        static private Transform SpawnPageTransform(ComicResourcePool resourcePool, in PageData pageData, int pageIndex) {
            Vector2 pos = ComicsUtility.UnpackPoint(pageData.Position);
            float rot = ComicsUtility.UnpackDegrees(pageData.PackedRotation);
            Transform root = resourcePool.ParentPool.Alloc(pos, Quaternion.Euler(0, 0, rot), false);
            if (Game.IsEditor) {
                root.gameObject.name = "Page " + pageIndex.ToStringLookup();
            }
            return root;
        }

        static private Transform SpawnPanelTransform(ComicResourcePool resourcePool, Transform parent, in PanelData panelData, int panelIndex) {
            Vector2 pos = ComicsUtility.UnpackPointPrecise(panelData.Position);
            float rot = ComicsUtility.UnpackDegrees(panelData.PackedRotation);
            Transform panel = resourcePool.ParentPool.Alloc(pos, Quaternion.Euler(0, 0, rot), parent, false);
            if (Game.IsEditor) {
                panel.gameObject.name = string.Format("Panel {0}: {1}", panelIndex.ToStringLookup(), panelData.Id.ToDebugString());
            }
            return panel;
        }

        static public void FreePageHierarchy(int pageIndex) {
            Find.State(out ComicResourcePool resourcePool, out ComicLayoutState layout);
            ComicSequenceManifest manifest = ComicsUtility.Manifest;
            Assert.NotNullOrDestroyed(manifest);
            Assert.True(pageIndex >= 0 && pageIndex < manifest.Pages.Length);

            if (layout.AllocatedPageMask.IsSet(pageIndex)) {
                Log.Msg("[ComicsUtility] Freeing hierarchy for page {0}", pageIndex);
                Transform root = layout.PageHierarchies[pageIndex];
                layout.PageHierarchies[pageIndex] = null;
                for(int i = root.childCount; i-- > 0;) {
                    FreePanelHierarchy(resourcePool, root.GetChild(i));
                }
                resourcePool.ParentPool.Free(root);
                layout.AllocatedPageMask.Unset(pageIndex);
            }
        }

        static private void FreePanelHierarchy(ComicResourcePool resourcePool, Transform panel) {
            for(int i = panel.childCount; i-- > 0;) {
                Transform child = panel.GetChild(i);
                bool freed = Pool.TryFree(child);
                Assert.True(freed, "Child was not pooled! What happened?");
            }
            resourcePool.ParentPool.Free(panel);
        }
    }
}