using NUnit.Framework;
using System.Collections.Generic;
using System.Linq.Expressions;
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
                AdjustSettingsBuildProcessor.Execute();
                BakeAssetsBuildPreprocessor.Execute();
                StripEditorDataBuildPreprocessor.Execute();
                StreamedPacks.ExecuteFullRebuild();
                BuildReport report = GeneratePlayerBuild();
                // TODO: report out
            } finally {
                IsAutomated = false;
                ManualStepControl = false;
            }
        }

        static public BuildPlayerOptions GetPlayerBuildOptions() {
            BuildPlayerOptions options = new BuildPlayerOptions();
            
            var editorScenes = EditorBuildSettings.scenes;
            List<string> scenes = new List<string>(editorScenes.Length);
            for(int i = 0; i < editorScenes.Length; i++) {
                var scene = editorScenes[i];
                if (scene.guid != default && scene.enabled) {
                    scenes.Add(scene.path);
                }
            }
            //options.target = 
            options.scenes = scenes.ToArray();

            return options;
        }

        static public BuildReport GeneratePlayerBuild() {
            BuildPlayerOptions buildOptions = GetPlayerBuildOptions();
            return BuildPipeline.BuildPlayer(buildOptions);
        }
    }
}