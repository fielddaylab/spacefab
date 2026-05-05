using FieldDay.Assets;
using FieldDay.Data;
using ScriptableBake;
using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace FieldDay.Editor {
    static public class StreamedPacks {
        static public BuildAssetBundlesParameters GenerateBaseBuildParameters() {
            BuildAssetBundlesParameters parameters = new BuildAssetBundlesParameters();
            parameters.outputPath = Path.Combine(Application.streamingAssetsPath, AssetMgr.StreamedPackagePath);
            parameters.options = BuildAssetBundleOptions.AssetBundleStripUnityVersion | BuildAssetBundleOptions.StrictMode | BuildAssetBundleOptions.StripUnatlasedSpriteCopies;
            return parameters;
        }

        static public AssetBundleBuild GeneratePackageBuildParameters(IAssetPackage package, string name) {
            AssetBundleBuild buildData = new AssetBundleBuild();
            buildData.assetBundleName = name;
            buildData.assetNames = new string[1] { AssetDatabase.GetAssetPath(package as UnityEngine.Object) };
            buildData.addressableNames = new string[1] { AssetMgr.StreamedRootAddressableName };
            return buildData;
        }
    }
}