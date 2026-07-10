using FieldDay.Assets;
using UnityEditor;
using UnityEngine;

namespace FieldDay.Editor {
    [CustomEditor(typeof(AssetPack), true), CanEditMultipleObjects]
    public class AssetPackEditor : UnityEditor.Editor {
        private SerializedProperty m_IncludeModeProp;
        private SerializedProperty m_GlobalAssetsProp;
        private SerializedProperty m_NamedAssetsProp;
        private SerializedProperty m_LiteAssetsProp;

        private void OnEnable() {
            m_IncludeModeProp = serializedObject.FindProperty("m_IncludeMode");
            m_GlobalAssetsProp = serializedObject.FindProperty("m_GlobalAssets");
            m_NamedAssetsProp = serializedObject.FindProperty("m_NamedAssets");
            m_LiteAssetsProp = serializedObject.FindProperty("m_LiteAssets");
        }

        private void OnDisable() {
            m_IncludeModeProp = null;
            m_GlobalAssetsProp = null;
            m_NamedAssetsProp = null;
            m_LiteAssetsProp = null;
        }

        public override void OnInspectorGUI() {
            serializedObject.UpdateIfRequiredOrScript();
            
            AssetPack.IncludeBehavior folderMode = (AssetPack.IncludeBehavior) m_IncludeModeProp.intValue;
            bool hasSingleMode = !m_IncludeModeProp.hasMultipleDifferentValues;

            EditorGUILayout.PropertyField(m_IncludeModeProp);

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Assets");
            using (new EditorGUI.DisabledScope(!hasSingleMode || folderMode != AssetPack.IncludeBehavior.Manual)) {
                EditorGUILayout.PropertyField(m_GlobalAssetsProp);
                EditorGUILayout.PropertyField(m_NamedAssetsProp);
                EditorGUILayout.PropertyField(m_LiteAssetsProp);
            }

            EditorGUILayout.Space();

            if (!hasSingleMode) {
                EditorGUILayout.HelpBox("Multiple folder modes detected.", MessageType.Warning);
            } else if (folderMode != AssetPack.IncludeBehavior.Manual) {
                EditorGUILayout.HelpBox("Asset packs with the 'IncludeSubfolders' or 'DirectoryOnly' modes cannot be manually modified.\nEnsure the assets you want in the package are located in the directory or its subdirectories.", MessageType.Info);
            }

            if (GUILayout.Button("Reload From Directory")) {
                foreach (AssetPack pack in targets) {
                    AssetPack.ReadFromEditorDirectory(pack);
                }
            }

            serializedObject.ApplyModifiedProperties();
        }
    }
}