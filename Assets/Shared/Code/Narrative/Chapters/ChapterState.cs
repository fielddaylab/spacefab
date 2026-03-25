using BeauUtil;
using FieldDay;
using FieldDay.Assets;
using FieldDay.Data;
using FieldDay.SharedState;
using SpaceFab.Design;
using SpaceFab.Save;
using System.Collections;
using UnityEngine;

namespace SpaceFab
{
    public class ChapterState : SharedStateComponent, ISaveStateChunkObject, IRegistrationCallbacks
    {
        public int CurrChapterIndex;
        public int LastSelectedContractIndex;

        [HideInInspector] public ContractsBundle CurrAvailableContractsBundle;

        public void OnDeregister()
        {
        }

        public void OnRegister()
        {
            LastSelectedContractIndex = -1;
            SpacefabGame.SaveBuffer.RegisterHandler("ChapterState", this);
        }


        #region Interfaces

        // ISaveStateChunkObject

        public void Read(object self, ref ByteReader reader, SaveStateChunkConsts consts)
        {
            reader.Read(ref CurrChapterIndex);
            reader.Read(ref LastSelectedContractIndex);
        }

        public void Write(object self, ref ByteWriter writer, SaveStateChunkConsts consts)
        {
            writer.Write(CurrChapterIndex);
            writer.Write(LastSelectedContractIndex);
        }

        #endregion // Interfaces
    }

    public static class ChapterUtility
    {
        public static void LoadNextChapter(ChapterState state)
        {
            // TODO
            state.CurrChapterIndex++;
        }

        /*
        public static void LoadPreviousState(ChapterState chapterState, PlayerProgressState progressState, int selectedContractIndex)
        {
            // load contract asset from previous chapter
            // ChapterLoadUtility.OnCurrContractKnown(chapterState, progressState, selectedContractIndex)
            ChapterLoadUtility.LoadAvailableContracts(chapterState);

            chapterState.CurrSelectedContractAssetPack = chapterState.CurrAvailableContractsBundle.AvailableContracts[selectedContractIndex].ContractAssets();
            Game.Assets.LoadPackage(chapterState.CurrSelectedContractAssetPack);

            // Unpack further
            StringHash32 assetsWrapperId = chapterState.CurrAvailableContractsBundle.AvailableContracts[selectedContractIndex].ContractAssetsWrapperId;
            var contractAssets = Find.NamedAsset<ContractAssetsWrapper>(assetsWrapperId);
        }

        public static void MoveFromPreviousState(ChapterState chapterState)
        {
            chapterState.PrevSelectedContractAssetPack = chapterState.CurrSelectedContractAssetPack;

            if (chapterState.CurrChapterAssetPack != null)
            {
                Game.Assets.UnloadPackage(chapterState.CurrChapterAssetPack);
                // Unload PrevSelectedContractAsset AFTER ContractCompletionSystem
            }

            chapterState.CurrChapterAssetPack = null;
            chapterState.CurrAvailableContractAssetsPack = null;
            // m_StateA.CurrSelectedContractAssetPack = null;
        }
        */
    }
}