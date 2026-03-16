using BeauUtil;
using FieldDay.Assets;
using FieldDay.Data;
using FieldDay.SharedState;
using SpaceFab.Save;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace SpaceFab
{
    public class ChapterState : SharedStateComponent, ISaveStateChunkObject
    {
        public int CurrChapterIndex;

        public ChapterDef PrevChapterDef;
        public ChapterDef CurrChapterDef;
        public ContractsBundle CurrAvailableContractsBundle;

        public AssetPack PrevSelectedContractAssetPack;     // unloaded in ContractCompletionSystem
        public AssetPack CurrSelectedContractAssetPack;     // assigned to PrevSelectedContractAssetPack, then unloaded in ContractCompletionSystem

        public AssetPack CurrChapterAssetPack;              // unloaded in ChapterLoadSystem
        public AssetPack CurrAvailableContractAssetsPack;   // TODO: unloaded at end of OverarchingScene


        #region Interfaces

        // ISaveStateChunkObject

        public void Read(object self, ref ByteReader reader, SaveStateChunkConsts consts)
        {

        }

        public void Write(object self, ref ByteWriter writer, SaveStateChunkConsts consts)
        {

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
    }
}