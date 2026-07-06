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
    public class ChapterState : SharedStateComponent, ISaveStateChunkObject, IRegistrationCallbacks, ISceneLoadDependency
    {
        [NonSerialized] public int ChapterIndex;
        [NonSerialized] public int LastSelectedContractIndex;

        [NonSerialized] public StringHash32 ChapterId;
        [NonSerialized] public ChapterDef ChapterDefinition;
        [NonSerialized] public UniqueId16 ChapterScriptHandle;

        [NonSerialized] public Routine LoadRoutine;

        public bool IsLoaded(SceneLoadFence fence) {
            return !LoadRoutine;
        }

        public void OnDeregister() {
            Game.Scenes.DeregisterLoadDependency(this);
        }

        public void OnRegister()
        {
            LastSelectedContractIndex = -1;
            SpacefabGame.SaveBuffer.RegisterHandler("ChapterState", this);
            Game.Scenes.RegisterLoadDependency(this);
        }


        #region Interfaces

        // ISaveStateChunkObject

        public void Read(object self, ref ByteReader reader, SaveStateChunkConsts consts)
        {
            reader.Read(ref ChapterIndex);
            reader.Read(ref LastSelectedContractIndex);
        }

        public void Write(object self, ref ByteWriter writer, SaveStateChunkConsts consts)
        {
            writer.Write(ChapterIndex);
            writer.Write(LastSelectedContractIndex);
        }

        #endregion // Interfaces
    }

    public static partial class ChapterUtility {
        #region Data Load/Unload

        static public bool LoadChapterData(ChapterState chapterState, int chapterIndex) {
            return LoadChapterData(chapterState, GetLoadInfo(chapterIndex));
        }

        static public bool LoadChapterData(ChapterState chapterState, StringHash32 chapterId) {
            return LoadChapterData(chapterState, GetLoadInfo(chapterId));
        }

        static public bool LoadChapterData(ChapterState chapterState, ChapterManifest.Entry loadInfo) {
            if (chapterState.ChapterId == loadInfo.ChapterId) {
                return false;
            }

            UnloadChapterData(chapterState);
            chapterState.ChapterId = loadInfo.ChapterId;
            chapterState.LoadRoutine.Replace(chapterState, LoadChapterProcess(chapterState, loadInfo));
            return true;
        }

        static private IEnumerator LoadChapterProcess(ChapterState chapterState, ChapterManifest.Entry loadInfo) {
            Game.Assets.LoadStreamedPackage(loadInfo.PackageId);
            while (Game.Assets.IsLoadingStreamedPackage(loadInfo.PackageId)) {
                yield return null;
            }
            ChapterDef chapterAsset = Find.NamedAsset<ChapterDef>(loadInfo.ChapterId);
            chapterState.ChapterDefinition = chapterAsset;
            chapterState.ChapterScriptHandle = ScriptDBUtility.Load(chapterAsset.Script);
        }

        static public bool UnloadChapterData(ChapterState chapterState) {
            if (chapterState.ChapterId.IsEmpty) {
                return false;
            }

            ScriptDBUtility.Unload(chapterState.ChapterScriptHandle);

            var loadInfo = GetLoadInfo(chapterState.ChapterId);
            Game.Assets.UnloadStreamedPackage(loadInfo.PackageId);

            chapterState.LoadRoutine.Stop();
            chapterState.ChapterDefinition = null;
            chapterState.ChapterScriptHandle = default;
            chapterState.ChapterId = default;
            return true;
        }

        #endregion // Data Load/Unload

        static public StringHash32 SelectedContractId(ChapterState chapterState) {
            if (chapterState.LastSelectedContractIndex < 0 || !chapterState.ChapterDefinition) {
                return null;
            }

            return chapterState.ChapterDefinition.AvailableContracts[chapterState.LastSelectedContractIndex];
        }

        public static void LoadNextChapter(ChapterState chapterState, PlayerProgressState progressState, ContractState contractState, MinigameSaveStates saveStates)
        {
            // save elapsed cycles and funds
            progressState.ElapsedCycles += saveStates.Fabrication.FinalizedTotalCycles;
            progressState.ElapsedCycles += saveStates.Supply.FinalizedTotalCycles;

            int contractPayout = 0;
            if (contractState.ContractDefinition) {
                contractPayout = contractState.ContractDefinition.Payout();
            }
            progressState.Funds += contractPayout - saveStates.Supply.FinalizedCost;

            // advance chapter
            chapterState.ChapterIndex++;
            progressState.RecentlyCompletedChapter = true;
            SaveUtility.Save(SaveSlot.Main);
            Game.Scenes.ReloadMainScene();
        }
    }
}