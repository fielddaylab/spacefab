using BeauUtil;
using BeauUtil.Editor;
using ScriptableBake;
using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace FieldDay.Editor {
    public sealed class BookmarksWindow : EditorWindow {
        [SerializeField] private List<string> m_ExpandedGuids = new List<string>();

        [SerializeField] private BookmarksPageAsset[] m_ProjectShortcuts;
        [SerializeField] private Vector2 m_Scroll;
        [SerializeField] private bool m_HorizontalView;

        [NonSerialized] private bool m_Rebuilt = false;

        [NonSerialized] private GUIContent m_CachedContent;
        [NonSerialized] private GUIStyle m_ButtonStyle;
        [NonSerialized] private GUIStyle m_ReadmeStyle;

        public void OnEnable() {
            titleContent = new GUIContent("Bookmarks");
            minSize = new Vector2(200, 200);
            UpdateProjectShortcuts();
        }

        public void OnDisable() {
            m_ProjectShortcuts = null;
        }

        private void UpdateProjectShortcuts() {
            m_ProjectShortcuts = AssetDBUtils.FindAssets<BookmarksPageAsset>();
            Array.Sort(m_ProjectShortcuts, (a, b) => {
                if (a.SortOrder != b.SortOrder) {
                    return a.SortOrder.CompareTo(b.SortOrder);
                }
                return a.name.CompareTo(b.name);
            });

            foreach(var shortcut in m_ProjectShortcuts) {
                shortcut.CachedName = shortcut.name;
                AssetDatabase.TryGetGUIDAndLocalFileIdentifier(shortcut, out shortcut.CachedGuid, out long _);
            }
        }

        public void OnGUI() {
            if (!m_Rebuilt) {
                UpdateProjectShortcuts();
                m_Rebuilt = true;
            }

            if (m_CachedContent == null) {
                m_CachedContent = new GUIContent();
            }

            if (m_ButtonStyle == null) {
                m_ButtonStyle = new GUIStyle(EditorStyles.miniButtonLeft);
                m_ButtonStyle.imagePosition = ImagePosition.ImageLeft;
                m_ButtonStyle.alignment = TextAnchor.MiddleLeft;
                m_ButtonStyle.fixedHeight = 20;
            }

            if (m_ReadmeStyle == null) {
                m_ReadmeStyle = new GUIStyle(EditorStyles.miniLabel);
                m_ReadmeStyle.alignment = TextAnchor.MiddleLeft;
                m_ReadmeStyle.wordWrap = true;
            }

            EditorGUILayout.BeginHorizontal(EditorStyles.toolbar); {
                if (GUILayout.Button(EditorGUIUtility.IconContent("d_Refresh"), EditorStyles.toolbarButton)) {
                    UpdateProjectShortcuts();
                }

                if (GUILayout.Button(EditorGUIUtility.IconContent(m_HorizontalView ? "d_align_horizontally" : "d_align_vertically"), EditorStyles.toolbarButton)) {
                    Baking.PrepareUndo(this, "changing horizontal");
                    m_HorizontalView = !m_HorizontalView;
                }

                EditorGUILayout.EndHorizontal();
            }

            EditorGUILayout.Space();

            if (m_ProjectShortcuts.Length <= 0) {
                EditorGUILayout.HelpBox("No shortcut assets are in the project", MessageType.Info);
                return;
            }

            Vector2 newScroll = EditorGUILayout.BeginScrollView(m_Scroll);
            if (m_Scroll != newScroll) {
                Baking.PrepareUndo(this, "scrolling");
                m_Scroll = newScroll;
            }
            foreach(var shortcutGroup in m_ProjectShortcuts) {
                RenderShortcutGroup(shortcutGroup);
            }
            EditorGUILayout.EndScrollView();
        }

        private void RenderShortcutGroup(BookmarksPageAsset shortcutsGroup) {
            bool isExpanded = m_ExpandedGuids.Contains(shortcutsGroup.CachedGuid);
            bool newExpended = EditorGUILayout.Foldout(isExpanded, string.Format("{0} ({1})", shortcutsGroup.CachedName, shortcutsGroup.Items.Length));
            if (newExpended != isExpanded) {
                Baking.PrepareUndo(this, "change expand");
                if (newExpended) {
                    m_ExpandedGuids.Add(shortcutsGroup.CachedGuid);
                } else {
                    m_ExpandedGuids.FastRemove(shortcutsGroup.CachedGuid);
                }
            }
            if (newExpended) {
                EditorGUILayout.BeginVertical(EditorStyles.helpBox);

                if (!string.IsNullOrEmpty(shortcutsGroup.ReadMe)) {
                    GUILayout.Box(shortcutsGroup.ReadMe, m_ReadmeStyle);
                    if (shortcutsGroup.Items.Length > 0) {
                        EditorGUILayout.Space();
                    }
                }

                if (shortcutsGroup.Items.Length > 0) {
                    if (m_HorizontalView) {
                        EditorGUILayout.BeginHorizontal();
                    }
                }

                foreach (var item in shortcutsGroup.Items) {
                    if (item.Object == null && !item.Scene.IsValid) {
                        continue;
                    }

                    GUIContent content = m_CachedContent;
                    string buttonName = item.CustomName;
                    if (string.IsNullOrEmpty(buttonName)) {
                        if (item.Object != null) {
                            buttonName = item.Object.name;
                        } else {
                            buttonName = item.Scene.Name;
                        }
                    }
                    content.text = buttonName;
                    if (item.Object != null) {
                        var objContent = EditorGUIUtility.ObjectContent(item.Object, item.Object.GetType());
                        content.image = objContent.image;
                    } else {
                        content.image = EditorGUIUtility.LoadRequired("d_Scene") as Texture;
                    }
                    if (GUILayout.Button(content, m_ButtonStyle)) {
                        if (item.Object != null) {
                            Selection.activeObject = item.Object;
                            EditorGUIUtility.PingObject(item.Object);
                            AssetDatabase.OpenAsset(item.Object);
                        } else {
                            if (EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) {
                                if (Event.current.shift) {
                                    EditorSceneManager.OpenScene(item.Scene.Path, OpenSceneMode.Additive);
                                } else {
                                    EditorSceneManager.OpenScene(item.Scene.Path, OpenSceneMode.Single);
                                }
                            }
                        }
                    }
                }

                if (shortcutsGroup.Items.Length > 0) {
                    if (m_HorizontalView) {
                        EditorGUILayout.EndHorizontal();
                    }
                }

                EditorGUILayout.EndVertical();
            }
        }

        [MenuItem("Field Day/Open Bookmarks Window", priority = -1000)]
        static private void OpenWindow() {
            if (!EditorWindow.HasOpenInstances<BookmarksWindow>()) {
                EditorWindow.GetWindow<BookmarksWindow>().Show();
            }
        }

        [MenuItem("Field Day/Open Bookmarks Window", priority = -1000, validate = true)]
        static private bool OpenWindow_Validate() {
            return !EditorWindow.HasOpenInstances<BookmarksWindow>();
        }

        [InitializeOnLoadMethod]
        static private void QueueInitialPrompt() {
            EditorApplication.delayCall += TryPresentInitialPrompt;
        }

        static private void TryPresentInitialPrompt() {
            if (!File.Exists("Library/HasDisplayedProjectShortcutsPrompt.txt")) {
                File.WriteAllText("Library/HasDisplayedProjectShortcutsPrompt.txt", " ");
                if (EditorWindow.HasOpenInstances<BookmarksWindow>()) {
                    return;
                }
                if (EditorUtility.DisplayDialog("Bookmarks", "\"Bookmarks\" are now available! Would you like to open them?", "Yes", "No")) {
                    OpenWindow();
                }
            }
        }
    }
}