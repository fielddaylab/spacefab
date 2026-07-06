using System.Collections.Generic;
using BeauUtil;
using FieldDay;
using FieldDay.Scenes;
using UnityEngine;

namespace SpaceFab {
    [PreloadOrder(-100)]
    public sealed class ChapterLoader : MonoBehaviour, IScenePreload {
        public IEnumerator<WorkSlicer.Result?> Preload() {
            Find.State(out ChapterState chapterState);
            ChapterUtility.LoadChapterData(chapterState, chapterState.ChapterIndex);
            while(chapterState.LoadRoutine) {
                yield return WorkSlicer.Result.HaltForFrame;
            }
        }
    }
}