using System;
using UnityEditor;
using UnityEngine;

namespace Bookmarking {
    [CreateAssetMenu(menuName = "Bookmarks Page", order = 5)]
    public sealed class BookmarksPageAsset : ScriptableObject {
        public int SortOrder = 0;
        [TextArea(2, 16)] public string ReadMe;
        public BookmarkItem[] Items;
        public bool IsLocked;

        [NonSerialized] public string CachedName;
        [NonSerialized] public string CachedGuid;
    }

    [Serializable]
    public struct BookmarkItem {
        public UnityEngine.Object Object;
        public string CustomName;
    }

    static public class BookmarksUtility {
        static public bool ContainsReference(BookmarksPageAsset page, UnityEngine.Object obj) {
            return IndexOfReference(page, obj) >= 0;
        }

        static private int IndexOfReference(BookmarksPageAsset page, UnityEngine.Object obj) {
            for (int i = 0; i < page.Items.Length; i++) {
                if (page.Items[i].Object == obj) {
                    return i;
                }
            }
            return -1;
        }

        static public bool AddReference(BookmarksPageAsset page, UnityEngine.Object obj) {
            if (IndexOfReference(page, obj) >= 0) {
                return false;
            }

            if (!EditorUtility.IsPersistent(obj)) {
                Debug.LogErrorFormat("[BookmarksUtility] Object '{0}' is not persistent, and cannot be added to a bookmark page", obj.name);
                return false;
            }

            Undo.RecordObject(page, "Adding reference");
            ArrayUtility.Add(ref page.Items, new BookmarkItem() {
                Object = obj
            });
            EditorUtility.SetDirty(page);
            return true;
        }

        static public bool RemoveReference(BookmarksPageAsset page, UnityEngine.Object obj) {
            int index = IndexOfReference(page, obj);
            if (index >= 0) {
                Undo.RecordObject(page, "Removing reference");
                ArrayUtility.RemoveAt(ref page.Items, index);
                EditorUtility.SetDirty(page);
                return true;
            }

            return false;
        }
    }
}