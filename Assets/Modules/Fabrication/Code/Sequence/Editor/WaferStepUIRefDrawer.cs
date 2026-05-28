using BeauUtil;
using BeauUtil.Editor;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace SpaceFab.Fabrication.Sequence.Editor
{
    /// <summary>
    /// Property drawer for [WaferStepUIRef]-tagged SerializedHash32 fields. Locates the
    /// singleton WaferStepUILookup asset and renders a dropdown of its entry ids, with a
    /// leading "&lt;none&gt;" option for fields that are allowed to be empty (e.g.
    /// SequenceStepEntry.ConvertToB).
    /// </summary>
    [CustomPropertyDrawer(typeof(WaferStepUIRefAttribute))]
    public class WaferStepUIRefDrawer : PropertyDrawer
    {
        // How long the cached entry list survives before being rebuilt. Matches the cadence
        // used by AssetNamePropertyDrawer.
        private const double RebuildCacheDelay = 1;

        private static double s_LastUpdateTime;
        private static WaferStepUILookup s_CachedLookup;
        private static NamedItemList<string> s_Items;

        // Invalidate the cache whenever an asset is imported / moved / deleted so newly
        // authored entries become available without restarting the editor.
        private class ImportHook : AssetPostprocessor
        {
            private static void OnPostprocessAllAssets(string[] importedAssets, string[] deletedAssets, string[] movedAssets, string[] movedFromAssetPaths)
            {
                s_LastUpdateTime = 0;
                s_CachedLookup = null;
            }
        }

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            // 1. Bail out cleanly if the drawer is applied to the wrong field type.
            SerializedProperty hashProp = property.FindPropertyRelative("m_HashValue");
            SerializedProperty stringProp = property.FindPropertyRelative("m_Source");
            if (hashProp == null || stringProp == null) {
                EditorGUI.LabelField(position, label, new GUIContent("[WaferStepUIRef] requires SerializedHash32"));
                return;
            }

            // 2. Refresh the cache lazily — singleton lookup is cheap, but the popup item list isn't.
            EnsureCache();

            label = EditorGUI.BeginProperty(position, label, property);
            EditorGUI.showMixedValue = hashProp.hasMultipleDifferentValues;

            // 3. If no lookup asset exists yet, render a placeholder and the raw source string so
            //    the field is still editable manually.
            if (s_CachedLookup == null) {
                EditorGUI.LabelField(position, label, new GUIContent("(no WaferStepUILookup asset found)"));
                EditorGUI.EndProperty();
                return;
            }

            // 4. Render the dropdown. The popup value is the entry's source string; on change we
            //    write both the source string and the corresponding StringHash32 hash back.
            string current = stringProp.stringValue;
            EditorGUI.BeginChangeCheck();
            string next = ListGUI.Popup(position, label, current, s_Items);
            if (EditorGUI.EndChangeCheck() && next != current) {
                stringProp.stringValue = next ?? string.Empty;
                hashProp.longValue = string.IsNullOrEmpty(next) ? 0 : new StringHash32(next).HashValue;
            }

            EditorGUI.EndProperty();
        }

        // Locates the singleton WaferStepUILookup and rebuilds the dropdown item list at most
        // once per RebuildCacheDelay window. The AssetPostprocessor hook also invalidates the
        // cache on any asset change.
        private static void EnsureCache()
        {
            double now = EditorApplication.timeSinceStartup;
            if (s_LastUpdateTime != 0 && (now - s_LastUpdateTime) <= RebuildCacheDelay && s_CachedLookup != null) {
                return;
            }
            s_LastUpdateTime = now;

            // Find the first WaferStepUILookup asset in the project. The project is expected to
            // own exactly one of these; multiple are not supported and we just use the first hit.
            string[] guids = AssetDatabase.FindAssets("t:WaferStepUILookup");
            if (guids == null || guids.Length == 0) {
                s_CachedLookup = null;
                s_Items = null;
                return;
            }
            string path = AssetDatabase.GUIDToAssetPath(guids[0]);
            s_CachedLookup = AssetDatabase.LoadAssetAtPath<WaferStepUILookup>(path);
            if (s_CachedLookup == null) {
                s_Items = null;
                return;
            }

            // Build the popup item list: a leading "<none>" entry (empty string), then one per
            // authored entry. The popup writes the chosen string back to m_Source.
            IReadOnlyList<WaferStepUIEntry> entries = s_CachedLookup.Entries;
            int capacity = (entries != null ? entries.Count : 0) + 1;
            if (s_Items == null) {
                s_Items = new NamedItemList<string>(capacity);
            } else {
                s_Items.Clear();
            }
            s_Items.Add(string.Empty, "<none>", -1);
            if (entries != null) {
                for (int i = 0; i < entries.Count; i++) {
                    string source = entries[i].Id.Source();
                    if (string.IsNullOrEmpty(source)) {
                        continue;
                    }
                    s_Items.Add(source, source);
                }
            }
        }
    }
}
