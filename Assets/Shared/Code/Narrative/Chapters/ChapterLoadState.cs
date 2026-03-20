using BeauUtil;
using FieldDay;
using FieldDay.Assets;
using FieldDay.SharedState;
using SpaceFab.Design;
using SpaceFab.Save;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEditor.MPE;
using UnityEngine;

namespace SpaceFab
{
    public enum ChapterLoadPhase
    {
        Waiting,
        Loading,
        Completed
    }

    [Serializable]
    public struct ChapterLoadBundle
    {
        [AssetName(typeof(ChapterDef))][SerializeField] public StringHash32 ChapterDefId;
        public AssetPack ChapterAssetPack;
    }

    public class ChapterLoadState : SharedStateComponent
    {
        public ChapterLoadPhase Phase;
        public ChapterLoadBundle[] Chapters;
    }

    public static class ChapterLoadUtility
    {
        public static void LoadAvailableContracts(ChapterState chapterState)
        {
            // loaded whenever entering overarching scene
            Game.Assets.LoadPackage(chapterState.CurrAvailableContractAssetsPack);
            chapterState.CurrAvailableContractsBundle = Find.NamedAsset<ContractsBundle>(chapterState.CurrChapterDef.AvailableContractsBundleId);
        }

        public static void OnCurrContractKnown(ChapterState chapterState, PlayerProgressState progressState, int selectedContractIndex)
        {
            chapterState.CurrSelectedContractAssetPack = chapterState.CurrAvailableContractsBundle.AvailableContracts[selectedContractIndex].ContractAssets();
            Game.Assets.LoadPackage(chapterState.CurrSelectedContractAssetPack);

            // Unpack further
            StringHash32 assetsWrapperId = chapterState.CurrAvailableContractsBundle.AvailableContracts[selectedContractIndex].ContractAssetsWrapperId;
            var contractAssets = Find.NamedAsset<ContractAssetsWrapper>(assetsWrapperId);
            // design level starts as initial config by default
            var minigameSaveState = Find.State<MinigameSaveStates>();
            minigameSaveState.Design.GridStack = new GridStack();
            GridStackUtility.LoadConfig(ref minigameSaveState.Design.GridStack, contractAssets.DesignLevelData.GetGridConfig());

            progressState.LastSelectedContract = chapterState.CurrAvailableContractsBundle.AvailableContracts[selectedContractIndex].AssetId;
        }
    }
}