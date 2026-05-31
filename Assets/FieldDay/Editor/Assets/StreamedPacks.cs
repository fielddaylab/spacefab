using BeauPools;
using BeauUtil;
using BeauUtil.Debugger;
using BeauUtil.Editor;
using FieldDay.Assets;
using FieldDay.Data;
using FieldDay.Files;
using ScriptableBake;
using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditorInternal;
using UnityEngine;

namespace FieldDay.Editor {
    static public class StreamedPacks {
        static private string GetBuildPath() {
            return Path.Combine(Application.streamingAssetsPath, AssetMgr.StreamedPackagePath);
        }

        static private BuildAssetBundleOptions GetBuildOptions() {
            BuildAssetBundleOptions options = BuildAssetBundleOptions.AssetBundleStripUnityVersion | BuildAssetBundleOptions.StrictMode | BuildAssetBundleOptions.RecurseDependencies;

            options |= BuildAssetBundleOptions.StripUnatlasedSpriteCopies;
            options |= BuildAssetBundleOptions.DisableLoadAssetByFileNameWithExtension | BuildAssetBundleOptions.DisableLoadAssetByFileName;

            if (BuildActions.IsBatchMode) {
                options |= BuildAssetBundleOptions.ForceRebuildAssetBundle;
            }

            return options;
        }

        static public BuildAssetBundlesParameters GenerateBaseBuildParameters() {
            BuildAssetBundlesParameters parameters = new BuildAssetBundlesParameters();
            parameters.outputPath = GetBuildPath();
            parameters.options = GetBuildOptions();
            return parameters;
        }

        static public BuildAssetBundlesParameters GenerateFullBuildParameters(BuildTarget target, bool clean) {
            BuildAssetBundlesParameters parameters = new BuildAssetBundlesParameters();
            parameters.outputPath = GetBuildPath();
            parameters.options = GetBuildOptions();
            parameters.targetPlatform = target;
            if (clean) {
                parameters.options |= BuildAssetBundleOptions.ForceRebuildAssetBundle;
            }

            StreamedPack[] allPacks = AssetDBUtils.FindAssets<StreamedPack>();
            AssetBundleBuild[] bundleBuilds = new AssetBundleBuild[allPacks.Length];
            for(int i = 0; i < allPacks.Length; i++) {
                bundleBuilds[i] = GeneratePackageBuildParameters(allPacks[i]);
            }
            parameters.bundleDefinitions = bundleBuilds;
            return parameters;
        }

        static public AssetBundleBuild GeneratePackageBuildParameters(StreamedPack package) {
            AssetBundleBuild buildData = new AssetBundleBuild();
            buildData.assetBundleName = package.name + ".pak";
            buildData.assetNames = new string[1] { AssetDatabase.GetAssetPath(package) };
            buildData.addressableNames = new string[1] { AssetMgr.StreamedRootAddressableName };
            return buildData;
        }

        static public bool ExecuteBuild(BuildAssetBundlesParameters parameters) {
            Directory.CreateDirectory(parameters.outputPath);
            Log.Msg("[StreamedPacks] Building {0} packs...", parameters.bundleDefinitions.Length);
            AssetBundleManifest manifest = BuildPipeline.BuildAssetBundles(parameters);
            if (manifest) {
                Log.Msg("[StreamedPacks] Built all packs successfully!");
                RebuildBinaryManifest();
                AssetBundleManifest.DestroyImmediate(manifest);
                return true;
            }

            return false;
        }

        [MenuItem("Field Day/Rebuild All Streamed Packs", priority = 500)]
        static public void ExecuteFullRebuild() {
            var build = GenerateFullBuildParameters(0, false);
            var result = ExecuteBuild(build);
        }

        [MenuItem("Field Day/Rebuild All Streamed Packs", priority = 500, validate = true)]
        static private bool ExecuteFullRebuild_Validate() {
            return !EditorApplication.isPlayingOrWillChangePlaymode;
        }

        [MenuItem("Field Day/Rebuild All Streamed Packs (Clean)", priority = 501)]
        static public void ExecuteCleanRebuild() {
            var build = GenerateFullBuildParameters(0, true);
            var result = ExecuteBuild(build);
        }

        [MenuItem("Field Day/Rebuild All Streamed Packs (Clean)", priority = 501, validate = true)]
        static private bool ExecuteCleanRebuild_Validate() {
            return !EditorApplication.isPlayingOrWillChangePlaymode;
        }

        static public void ExecuteCleanRebuildForPlatform(BuildTarget target) {
            var build = GenerateFullBuildParameters(target, true);
            var result = ExecuteBuild(build);
        }

        static public void RebuildBinaryManifest() {
            string buildPath = GetBuildPath();
            DirectoryInfo dirInfo = Directory.CreateDirectory(buildPath);
            IEnumerable<FileInfo> files = dirInfo.EnumerateFiles("*.pak", SearchOption.TopDirectoryOnly);
            Dictionary<StringHash32, string> map = new Dictionary<StringHash32, string>();
            StreamedPack[] allPacks = AssetDBUtils.FindAssets<StreamedPack>();
            foreach(var file in files) {
                string idStr = Path.GetFileNameWithoutExtension(file.Name);
                string path = FileSystem.SanitizePath(Path.Combine(AssetMgr.StreamedPackagePath, file.Name));
                
                foreach(var pack in allPacks) {
                    if (pack.name.ToLower() == idStr) {
                        idStr = pack.name;
                        break;
                    }
                }

                Log.Trace("[StreamedPacks] Found pack '{0}' at path '{1}'", idStr, path);
                map.Add(idStr, path);
            }

            WriteBinaryManifest(map);
        }

        static private unsafe void WriteBinaryManifest(Dictionary<StringHash32, string> entries) {
            byte* writerData = (byte*) Frame.Alloc(Unsafe.KiB * 64);
            ByteWriter writer = new ByteWriter(writerData, Unsafe.KiB * 64);

            writer.Write((ushort) entries.Count);
            foreach(var entry in entries) {
                writer.Write(entry.Key);
                writer.WriteUTF8(entry.Value);
            }

            File.WriteAllBytes(Path.Combine(Application.streamingAssetsPath, AssetMgr.StreamedManifestPath), writer.GetDataCopy());
        }
    }
}