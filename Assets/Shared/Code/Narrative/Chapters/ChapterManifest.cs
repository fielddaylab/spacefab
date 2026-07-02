using System;
using BeauUtil;
using FieldDay.Assets;
using UnityEngine;

namespace SpaceFab {
    [CreateAssetMenu(menuName = "SpaceFab/Chapters/Chapter Manifest")]
    public sealed class ChapterManifest : GlobalAsset {
        [Serializable]
        public struct Entry {
            [StreamedPackId] public StringHash32 PackageId;
            [AssetName(typeof(ChapterDef))] public StringHash32 ChapterDef;
        }

        public Entry[] Chapters;
    }
}