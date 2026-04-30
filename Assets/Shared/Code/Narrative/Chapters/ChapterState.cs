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
        [HideInInspector] public ChapterDef CurrChapterDef;

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
        public static void LoadNextChapter(ChapterState chapterState, PlayerProgressState progressState)
        {
            chapterState.CurrChapterIndex++;
            progressState.RecentlyCompletedChapter = true;
            SaveUtility.Save(SaveSlot.Main);
            Game.Scenes.ReloadMainScene();
        }
    }
}