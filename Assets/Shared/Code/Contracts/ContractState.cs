using BeauRoutine;
using BeauUtil;
using BeauUtil.Debugger;
using FieldDay;
using FieldDay.Assets;
using FieldDay.Data;
using FieldDay.Scenes;
using FieldDay.Scripting;
using FieldDay.SharedState;
using SpaceFab.Design;
using SpaceFab.Save;
using System;
using System.Collections;
using UnityEngine;

namespace SpaceFab
{
    public class ContractState : SharedStateComponent, IRegistrationCallbacks, ISceneLoadDependency
    {
        [NonSerialized] public StringHash32 ContractId;
        [NonSerialized] public ContractDef ContractDefinition;
        [NonSerialized] public ContractAssetSet ContractAssets;
        [NonSerialized] public UniqueId16 ContractScriptHandle;

        [NonSerialized] public Routine LoadRoutine;

        public bool IsLoaded(SceneLoadFence fence) {
            return !LoadRoutine;
        }

        public void OnDeregister() {
            Game.Scenes.DeregisterLoadDependency(this);
        }

        public void OnRegister()
        {
            Game.Scenes.RegisterLoadDependency(this);
        }
    }

    public static partial class ContractUtility {
        #region Data Load/Unload

        static public bool LoadContractData(ContractState contractState, int contractIndex) {
            return LoadContractData(contractState, GetInfo(contractIndex));
        }

        static public bool LoadContractData(ContractState contractState, StringHash32 contractId) {
            return LoadContractData(contractState, GetDefinition(contractId));
        }

        static public bool LoadContractData(ContractState contractState, ContractDef definition) {
            if (contractState.ContractId == definition.AssetId) {
                return false;
            }

            UnloadContractData(contractState);
            contractState.ContractId = definition.AssetId;
            contractState.LoadRoutine.Replace(contractState, LoadContractProcess(contractState, definition));
            return true;
        }

        static private IEnumerator LoadContractProcess(ContractState contractState, ContractDef definition) {
            Game.Assets.LoadStreamedPackage(definition.StreamedPack);
            while (Game.Assets.IsLoadingStreamedPackage(definition.StreamedPack)) {
                yield return null;
            }
            ContractAssetSet assetSet = Find.NamedAsset<ContractAssetSet>(definition.AssetSet);

            contractState.ContractDefinition = definition;
            contractState.ContractAssets = assetSet;
            contractState.ContractScriptHandle = ScriptDBUtility.Load(assetSet.Script);
        }

        static public bool UnloadContractData(ContractState contractState) {
            if (contractState.ContractId.IsEmpty) {
                return false;
            }

            ScriptDBUtility.Unload(contractState.ContractScriptHandle);
            Game.Assets.UnloadStreamedPackage(contractState.ContractDefinition.StreamedPack);

            contractState.LoadRoutine.Stop();
            contractState.ContractDefinition = null;
            contractState.ContractAssets = null;
            contractState.ContractScriptHandle = default;
            contractState.ContractId = default;
            return true;
        }

        #endregion // Data Load/Unload
    }
}