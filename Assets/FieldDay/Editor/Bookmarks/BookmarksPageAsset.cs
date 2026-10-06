using BeauUtil;
using System;
using UnityEditor;
using UnityEngine;

namespace FieldDay.Editor {
    [CreateAssetMenu(menuName = "Field Day/Bookmarks Page", order = -250)]
    public sealed class BookmarksPageAsset : ScriptableObject {
        public int SortOrder = 0;
        [TextArea(2, 16)] public string ReadMe;
        public BookmarkItem[] Items;

        [NonSerialized] public string CachedName;
        [NonSerialized] public string CachedGuid;
    }

    [Serializable]
    public struct BookmarkItem {
        public SceneReference Scene;
        public UnityEngine.Object Object;
        public string CustomName;
    }
}