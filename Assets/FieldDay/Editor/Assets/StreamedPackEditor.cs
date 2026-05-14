using FieldDay.Assets;
using UnityEditor;
using UnityEngine;
using System.Collections.Generic;
using System;
using BeauUtil.Debugger;

namespace FieldDay.Editor {
    [CustomEditor(typeof(StreamedPack), true), CanEditMultipleObjects]
    public class StreamedPackEditor : UnityEditor.Editor {
        public override void OnInspectorGUI() {
            base.OnInspectorGUI();

            EditorGUILayout.Space();

            if (GUILayout.Button("Export Packs")) {
                List<AssetBundleBuild> bundleBuilds = new List<AssetBundleBuild>(targets.Length);
                foreach(StreamedPack pack in targets) {
                    pack.EditorRebuild();
                    bundleBuilds.Add(StreamedPacks.GeneratePackageBuildParameters(pack));
                }

                BuildAssetBundlesParameters buildParams = StreamedPacks.GenerateBaseBuildParameters();
                buildParams.bundleDefinitions = bundleBuilds.ToArray();

                StreamedPacks.ExecuteBuild(buildParams);
            }
        }
    }
}