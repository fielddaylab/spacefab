using FieldDay.Data;
using ScriptableBake;
using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace FieldDay.Editor {
    public sealed class StreamingPackageManifest : ScriptableObject, IBaked {
        [Serializable]
        private struct Data {
            public ScriptableObject Pack;
            public bool IsEnabled;
            // TODO: platform-specific?
        }

        [SerializeField] private string m_StreamingFolder = "packs";
        [SerializeField] private bool m_HashFileNames;
        [SerializeField] private Data[] m_Packs;

        private BuildAssetBundlesParameters CreateBuildParameters() {
            BuildAssetBundlesParameters parameters = new BuildAssetBundlesParameters();
            parameters.outputPath = Path.Combine(Application.streamingAssetsPath, m_StreamingFolder);
            parameters.options = BuildAssetBundleOptions.AssetBundleStripUnityVersion | BuildAssetBundleOptions.StrictMode | BuildAssetBundleOptions.StripUnatlasedSpriteCopies;
            parameters.bundleDefinitions = CreateBuildSettings();
            return parameters;
        }

        private AssetBundleBuild[] CreateBuildSettings() {
            List<AssetBundleBuild> builds = new List<AssetBundleBuild>(m_Packs.Length);
            foreach(var data in m_Packs) {
                if (!data.IsEnabled || !data.Pack) {
                    continue;
                }

                AssetBundleBuild buildData = new AssetBundleBuild();
                buildData.assetBundleName = data.Pack.name;
            }
            return builds.ToArray();
        }

        #region IBaked

        int IBaked.Order { get { return 10000; } }

        bool IBaked.Bake(BakeFlags flags, BakeContext context) {
            BuildAssetBundlesParameters buildParams = CreateBuildParameters();
            Directory.CreateDirectory(buildParams.outputPath);
            BuildPipeline.BuildAssetBundles(buildParams);

            if ((flags & BakeFlags.IsBatchMode) != 0) {
                Baking.Destroy(this, true);
            }

            return true;
        }

        #endregion // IBaked
    }
}