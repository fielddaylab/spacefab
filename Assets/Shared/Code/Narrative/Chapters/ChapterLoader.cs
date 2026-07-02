using System.Collections.Generic;
using BeauUtil;
using FieldDay.Scenes;
using UnityEngine;

namespace SpaceFab {
    [PreloadOrder(-100)]
    public sealed class ChapterLoader : MonoBehaviour, IScenePreload {
        public IEnumerator<WorkSlicer.Result?> Preload() {
            return null;
        }
    }
}