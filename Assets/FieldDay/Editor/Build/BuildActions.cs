using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEditorInternal;

namespace FieldDay.Editor {
    static public class BuildActions {
        static public bool IsAutomated { get; private set; }
        static public bool ManualStepControl { get; private set; }

        static public bool IsBatchMode {
            get {
                return IsAutomated || IsBatchModeRaw;
            }
        }

        static public bool IsBatchModeRaw {
            get {
                return InternalEditorUtility.inBatchMode || !InternalEditorUtility.isHumanControllingUs;
            }
        }

        static public void AutomatedBuild() {
            IsAutomated = true;
            ManualStepControl = true;
            try {
                LocateBuildConfiguration();
                ProcessAssets(BuildTarget.WebGL);
                BuildReport report = GeneratePlayerBuild(BuildTarget.WebGL);
                // TODO: report out
            } finally {
                IsAutomated = false;
                ManualStepControl = false;
            }
        }

        static public BuildPlayerOptions GetPlayerBuildOptions(BuildTarget buildTarget) {
            BuildPlayerOptions options = new BuildPlayerOptions();

            options.target = buildTarget;
            options.scenes = GetBuildScenes();

            return options;
        }

        static public string[] GetBuildScenes() {
            var editorScenes = EditorBuildSettings.scenes;
            List<string> scenes = new List<string>(editorScenes.Length);
            for (int i = 0; i < editorScenes.Length; i++) {
                var scene = editorScenes[i];
                if (scene.guid != default && scene.enabled) {
                    scenes.Add(scene.path);
                }
            }
            return scenes.ToArray();
        }

        static public BuildReport GeneratePlayerBuild(BuildTarget buildTarget) {
            BuildPlayerOptions buildOptions = GetPlayerBuildOptions(buildTarget);
            return BuildPipeline.BuildPlayer(buildOptions);
        }

        static public void LocateBuildConfiguration() {
            AdjustSettingsBuildProcessor.Execute();
        }

        static public void ProcessAssets(BuildTarget buildTarget) {
            BakeAssetsBuildPreprocessor.Execute();
            StripEditorDataBuildPreprocessor.Execute();
            StreamedPacks.ExecuteCleanRebuildForPlatform(buildTarget);
        }
    }
}