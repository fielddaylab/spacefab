using BeauRoutine;
using BeauUtil;
using FieldDay;
using FieldDay.Animation;
using FieldDay.Assets;
using FieldDay.Scenes;
using FieldDay.Scripting;
using Leaf.Runtime;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace SpaceFab.Comic {
    static public class ComicScripting {

        [LeafMember("ComicPreloadPage")]
        static public void Leaf_PreloadPage(int pageIndex) {
            ComicsUtility.PreloadPage(pageIndex);
        }

        [LeafMember("ComicPreloadNextPage")]
        static public void Leaf_PreloadNextPage() {
            Find.State(out ComicDisplayState displayState);
            int nextIndex = displayState.CurrentPageIndex + 1;
            if (nextIndex >= 0 && nextIndex < ComicsUtility.Manifest.Pages.Length) {
                ComicsUtility.PreloadPage(nextIndex);
            }
        }

        [LeafMember("ComicOpenPage")]
        static public void Leaf_SpawnPage(int pageIndex) {
            Find.State(out ComicDisplayState displayState);
            displayState.CurrentPageIndex = pageIndex;
            ComicResourceUtility.AllocatePageHierarchy(pageIndex);
        }

        [LeafMember("ComicOpenNextPage")]
        static public void Leaf_SpawnNextPage() {
            Find.State(out ComicDisplayState displayState);
            displayState.CurrentPageIndex++;
            ComicResourceUtility.AllocatePageHierarchy(displayState.CurrentPageIndex);
        }

        [LeafMember("ComicUnloadPage")]
        static public void Leaf_UnloadPage(int pageIndex) {
            ComicResourceUtility.FreePageHierarchy(pageIndex);
            ComicsUtility.CancelPreloadPage(pageIndex);
        }

        [LeafMember("ComicUnloadPreviousPage")]
        static public void Leaf_UnloadPreviousPage() {
            Find.State(out ComicDisplayState displayState);
            int prevPage = displayState.CurrentPageIndex - 1;
            if (prevPage >= 0) {
                ComicResourceUtility.FreePageHierarchy(prevPage);
                ComicsUtility.CancelPreloadPage(prevPage);
            }
        }

        [LeafMember("ComicSnapCamera")]
        static public void Leaf_SnapCamera(StringHash32 cameraId) {
            ComicsUtility.SnapCamera(cameraId);
        }

        [LeafMember("ComicPanCameraAsync")]
        static public void Leaf_PanCamera(StringHash32 cameraId, float duration = 1, Curve easing = Curve.Smooth) {
            ComicsUtility.PanCamera(cameraId, duration, easing);
        }

        [LeafMember("ComicPanCamera")]
        static public IEnumerator Leaf_PanCameraAndWait(StringHash32 cameraId, float duration = 1, Curve easing = Curve.Smooth) {
            AnimHandle pan = ComicsUtility.PanCamera(cameraId, duration, easing);
            while(Game.Animation.IsAnimationRunning(pan)) {
                yield return null;
            }
        }

        [LeafMember("ComicWaitForCamera")]
        static public IEnumerator Leaf_WaitForCamera() {
            Find.State(out ComicCameraState camState);
            while (Game.Animation.IsAnimationRunning(camState.CameraTransition)) {
                yield return null;
            }
        }
    }
}