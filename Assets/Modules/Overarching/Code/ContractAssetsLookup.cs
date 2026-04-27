using BeauUtil;
using FieldDay;
using FieldDay.Assets;
using FieldDay.Scenes;
using FieldDay.SharedState;
using SpaceFab.Design;
using SpaceFab.Save;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace SpaceFab.Overarching
{
    [Serializable]
    public struct ContractAssetsLookupData
    {
        public SceneReference Scene;
        [AssetName(typeof(ContractAssetsWrapper))] [SerializeField] public StringHash32 AssetsWrapperId;
    }

    [Serializable]
    public struct ContractAssetsLookupEntry
    {
        [AssetName(typeof(ContractDef))] [SerializeField] public StringHash32 ContractId;
        public ContractAssetsLookupData Data;
    }

    public class ContractAssetsLookup : SharedStateComponent, IRegistrationCallbacks
    {
        public ContractAssetsLookupEntry[] Entries;

        public Dictionary<StringHash32, ContractAssetsLookupData> Map = null;

        public void OnDeregister()
        {
        }

        public void OnRegister()
        {
            ContractsLookupUtility.ConstructMap(this);
        }
    }

    public static partial class ContractsLookupUtility
    {
        public static void ConstructMap(ContractAssetsLookup lookup)
        {
            if (lookup.Map != null)
            {
                return;
            }

            lookup.Map = new Dictionary<StringHash32, ContractAssetsLookupData>();
            foreach (var entry in lookup.Entries)
            {
                if (lookup.Map.ContainsKey(entry.ContractId))
                {
                    Debug.LogError("[ContractAssetsLookup] Duplicate key found when constructing map!");
                    continue;
                }

                lookup.Map.Add(entry.ContractId, entry.Data);
            }
        }

        public static void Lookup(ContractAssetsLookup lookup, StringHash32 contractId, out SceneReference sceneRef, out StringHash32 assetsWrapperId)
        {
            if (lookup.Map == null)
            {
                ConstructMap(lookup);
            }

            sceneRef = lookup.Map[contractId].Scene;
            assetsWrapperId = lookup.Map[contractId].AssetsWrapperId;
        }

        public static IEnumerator LoadContract(ContractAssetsLookup lookup, PlayerProgressState playerProgress, StringHash32 contractId)
        {
            Lookup(lookup, contractId, out SceneReference contractAssetsScene, out StringHash32 assetsWrapperId);

            playerProgress.ContractAssetsWrapperId = assetsWrapperId;

            if (Game.Scenes.IsLoaded(contractAssetsScene))
            {
                yield break;
            }

            Game.Scenes.LoadPersistentScene(contractAssetsScene);

            while (Game.Scenes.IsLoading(contractAssetsScene) || Game.Scenes.IsLoadingAnyScene())
            {
                yield return null;
            }
        }

        // TODO: call this on chapter end
        public static IEnumerator UnloadContract(ContractAssetsLookup lookup, StringHash32 contractId)
        {
            Lookup(lookup, contractId, out SceneReference contractAssetsScene, out StringHash32 assetsWrapperId);

            if (!Game.Scenes.IsLoaded(contractAssetsScene))
            {
                yield break;
            }

            Game.Scenes.UnloadScene(contractAssetsScene);

            while (Game.Scenes.IsUnloading())
            {
                yield return null;
            }
        }
    }
}
