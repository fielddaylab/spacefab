using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using BeauPools;
using BeauUtil;
using BeauUtil.Editor;
using FieldDay.Assets;
using FieldDay.Data;
using ScriptableBake;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace FieldDay.Editor.Tests {
    static public class DependencyAnalyzer {
        [MenuItem("Field Day/Testing/Analyze Dependencies For All Scenes")]
        static private void Test() {
            string[] sceneGuids = AssetDatabase.FindAssets("t:Scene");
            string[] scenePaths = ArrayUtils.MapFrom<string, string>(sceneGuids, AssetDatabase.GUIDToAssetPath);

            Directory.CreateDirectory("Temp/SceneAnalysis");
            StringBuilder builder = new StringBuilder(ushort.MaxValue);
            int count = 0;
            try {
                foreach (var scenePath in scenePaths) {
                    EditorUtility.DisplayProgressBar("Building scene dependency graph...", "Scene " + scenePath, count / (float)scenePaths.Length);
                    string[] dependencies = AssetDatabase.GetDependencies(scenePath, true);
                    Array.Sort(dependencies);
                    builder.Append("Scene ").Append(scenePath);
                    foreach (var dependency in dependencies) {
                        if (dependency.EndsWith(".cs")) {
                            continue;
                        }
                        builder.Append("\n - ").Append(dependency);
                    }
                    builder.Append("\n\n");
                    count++;
                }

                builder.TrimEnd(StringUtils.DefaultNewLineChars);

                File.WriteAllText("Temp/SceneAnalysis/Dependencies.txt", builder.Flush());
                EditorUtility.OpenWithDefaultApp("Temp/SceneAnalysis/Dependencies.txt");
            } finally {
                EditorUtility.ClearProgressBar();
            }
        }
    }
}