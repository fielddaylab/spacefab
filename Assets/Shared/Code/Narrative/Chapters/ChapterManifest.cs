using System;
using System.Collections.Generic;
using BeauUtil;
using BeauUtil.Debugger;
using FieldDay;
using FieldDay.Assets;
using UnityEngine;

namespace SpaceFab {
    [CreateAssetMenu(menuName = "SpaceFab/Chapters/Chapter Manifest")]
    public sealed class ChapterManifest : GlobalAsset {
        [Serializable]
        public struct Entry {
            [AssetName(typeof(ChapterDef))] public StringHash32 ChapterId;
            [StreamedPackId] public StringHash32 PackageId;
        }

        public Entry[] Chapters;
    }

    static public partial class ChapterUtility {
        static public int GetIndex(StringHash32 chapterId) {
            Find.GlobalAsset(out ChapterManifest manifest);
            for(int i = 0, len = manifest.Chapters.Length; i < len; i++) {
                if (manifest.Chapters[i].ChapterId == chapterId) {
                    return i;
                }
            }
            Assert.Fail("No chapter with id '{0}'", chapterId);
            return -1;
        }

        static public ChapterManifest.Entry GetLoadInfo(StringHash32 chapterId) {
            Find.GlobalAsset(out ChapterManifest manifest);
            for (int i = 0, len = manifest.Chapters.Length; i < len; i++) {
                if (manifest.Chapters[i].ChapterId == chapterId) {
                    return manifest.Chapters[i];
                }
            }
            Assert.Fail("No chapter with id '{0}'", chapterId);
            return default;
        }

        static public ChapterManifest.Entry GetLoadInfo(int chapterIndex) {
            Find.GlobalAsset(out ChapterManifest manifest);
            Assert.True(chapterIndex >= 0 && chapterIndex < manifest.Chapters.Length, "Chapter index {0} out of range", chapterIndex);
            return manifest.Chapters[chapterIndex];
        }

        static public int ChapterCount() {
            Find.GlobalAsset(out ChapterManifest manifest);
            return manifest.Chapters.Length;
        }
    }
}