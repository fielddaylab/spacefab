using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace Bookmarking {
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
            titleContent = new GUIContent("Bookmarks", EditorGUIUtility.LoadRequired("d_Favorite") as Texture);
            minSize = new Vector2(200, 200);
            UpdatePageList();
        }

        public void OnDisable() {
            m_ProjectShortcuts = null;
        }

        private void UpdatePageList() {
            m_ProjectShortcuts = FindPages();
        }

        static private BookmarksPageAsset[] FindPages() {
            string[] guids = AssetDatabase.FindAssets("t:BookmarksPageAsset");
            BookmarksPageAsset[] pages = new BookmarksPageAsset[guids.Length];
            for(int i = 0; i < guids.Length; i++) {
                string path = AssetDatabase.GUIDToAssetPath(guids[i]);
                BookmarksPageAsset page = AssetDatabase.LoadAssetAtPath<BookmarksPageAsset>(path);
                pages[i] = page;
            }
            Array.Sort(pages, (a, b) => {
                if (a.SortOrder != b.SortOrder) {
                    return a.SortOrder.CompareTo(b.SortOrder);
                }
                return a.name.CompareTo(b.name);
            });

            foreach (var shortcut in pages) {
                shortcut.CachedName = ObjectNames.NicifyVariableName(shortcut.name);
                AssetDatabase.TryGetGUIDAndLocalFileIdentifier(shortcut, out shortcut.CachedGuid, out long _);
            }
            return pages;
        }

        public void OnGUI() {
            if (!m_Rebuilt) {
                UpdatePageList();
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
                    UpdatePageList();
                }

                if (GUILayout.Button(EditorGUIUtility.IconContent(m_HorizontalView ? "d_align_horizontally" : "d_align_vertically"), EditorStyles.toolbarButton)) {
                    Undo.RecordObject(this, "changing horizontal");
                    m_HorizontalView = !m_HorizontalView;
                    EditorUtility.SetDirty(this);
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
                Undo.RecordObject(this, "scrolling");
                m_Scroll = newScroll;
                EditorUtility.SetDirty(this);
            }
            foreach(var shortcutGroup in m_ProjectShortcuts) {
                RenderPage(shortcutGroup);
            }
            EditorGUILayout.EndScrollView();
        }

        private void RenderPage(BookmarksPageAsset page) {
            bool isExpanded = m_ExpandedGuids.Contains(page.CachedGuid);
            GUIContent headerContent = m_CachedContent;
            headerContent.text = string.Format("{0} ({1})", page.CachedName, page.Items.Length);
            if (page.IsLocked) {
                headerContent.image = EditorGUIUtility.LoadRequired("d_AssemblyLock") as Texture;
            } else {
                headerContent.image = EditorGUIUtility.LoadRequired("d_ListView") as Texture;
            }

            bool newExpended = EditorGUILayout.Foldout(isExpanded, headerContent);
            if (newExpended != isExpanded) {
                Undo.RecordObject(this, "change expand");
                if (newExpended) {
                    m_ExpandedGuids.Add(page.CachedGuid);
                } else {
                    m_ExpandedGuids.Remove(page.CachedGuid);
                }
                EditorUtility.SetDirty(this);
            }

            Rect headerRect = GUILayoutUtility.GetLastRect();
            if (Event.current.type == EventType.ContextClick) {
                if (headerRect.Contains(Event.current.mousePosition)) {
                    GenericMenu menu = new GenericMenu();
                    menu.AddItem(new GUIContent("Jump to Page Definition"), false, () => {
                        Selection.activeObject = page;
                        EditorGUIUtility.PingObject(page);
                        AssetDatabase.OpenAsset(page);
                    });
                    menu.ShowAsContext();
                }
            }

            if (newExpended) {
                EditorGUILayout.BeginVertical(EditorStyles.helpBox);

                if (!string.IsNullOrEmpty(page.ReadMe)) {
                    GUILayout.Box(page.ReadMe, m_ReadmeStyle);
                    if (page.Items.Length > 0) {
                        EditorGUILayout.Space();
                    }
                }

                if (page.Items.Length > 0) {
                    if (m_HorizontalView) {
                        EditorGUILayout.BeginHorizontal();
                    }
                }

                bool canLoadScenes = !EditorApplication.isPlayingOrWillChangePlaymode;

                for(int i = 0; i < page.Items.Length; i++) {
                    var item = page.Items[i];
                    if (item.Object == null) {
                        continue;
                    }

                    GUIContent content = m_CachedContent;
                    string buttonName = item.CustomName;
                    if (string.IsNullOrEmpty(buttonName)) {
                        buttonName = item.Object.name;
                    }
                    content.text = buttonName;
                    bool isScene = true;
                    if (item.Object is not SceneAsset) {
                        var objContent = EditorGUIUtility.ObjectContent(item.Object, item.Object.GetType());
                        content.image = objContent.image;
                        isScene = false;
                    } else {
                        if (Event.current.shift) {
                            content.image = EditorGUIUtility.LoadRequired("d_CreateAddNew") as Texture;
                        } else {
                            content.image = EditorGUIUtility.LoadRequired("d_Scene") as Texture;
                        }
                    }

                    bool wasGUIEnabled = GUI.enabled;
                    GUI.enabled = !isScene || canLoadScenes;

                    if (GUILayout.Button(content, m_ButtonStyle)) {
                        if (Event.current.type == EventType.ContextClick) { // right-click remove
                            if (!page.IsLocked) {
                                GenericMenu menu = new GenericMenu();
                                menu.AddItem(new GUIContent("Remove from Page?"), false, () => {
                                    BookmarksUtility.RemoveReference(page, item.Object);
                                });
                                menu.ShowAsContext();
                            }
                        } else {
                            if (item.Object is not SceneAsset) {
                                Selection.activeObject = item.Object;
                                EditorGUIUtility.PingObject(item.Object);
                                AssetDatabase.OpenAsset(item.Object);
                            } else {
                                if (EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) {
                                    string path = AssetDatabase.GetAssetOrScenePath(item.Object);
                                    if (Event.current.shift) {
                                        EditorSceneManager.OpenScene(path, OpenSceneMode.Additive);
                                    } else {
                                        EditorSceneManager.OpenScene(path, OpenSceneMode.Single);
                                    }
                                }
                            }
                        }
                    }

                    GUI.enabled = wasGUIEnabled;
                }

                if (page.Items.Length > 0) {
                    if (m_HorizontalView) {
                        EditorGUILayout.EndHorizontal();
                    }
                }

                EditorGUILayout.EndVertical();

                Rect boxRect = GUILayoutUtility.GetLastRect();

                if (!page.IsLocked) {
                    EventType evtType = Event.current.type;
                    if (evtType == EventType.DragUpdated || evtType == EventType.DragPerform) {
                        if (boxRect.Contains(Event.current.mousePosition)) {
                            DragAndDrop.visualMode = DragAndDropVisualMode.Copy;
                            if (evtType == EventType.DragPerform) {
                                DragAndDrop.AcceptDrag();
                                bool displayedNotPersistentNotification = false;
                                foreach(var obj in DragAndDrop.objectReferences) {
                                    if (!EditorUtility.IsPersistent(obj)) {
                                        if (!displayedNotPersistentNotification) {
                                            displayedNotPersistentNotification = true;
                                            ShowNotification(new GUIContent("Cannot add non-persistent objects to a Bookmark page!"), 1.0);
                                        }
                                    }
                                    BookmarksUtility.AddReference(page, obj);
                                }
                            }
                        }
                    }
                }
            }
        }

        [MenuItem("Field Day/Open Bookmarks Window %[", priority = -1000)]
        static private void OpenWindow() {
            if (!EditorWindow.HasOpenInstances<BookmarksWindow>()) {
                EditorWindow.GetWindow<BookmarksWindow>().Show();
            }
        }

        [MenuItem("Field Day/Open Bookmarks Window %[", priority = -1000, validate = true)]
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
                if (EditorUtility.DisplayDialog("Bookmarks", "The \"Bookmarks\" tab is now available! Would you like to open it?", "Yes", "No")) {
                    OpenWindow();
                }
            }
        }
    }
}