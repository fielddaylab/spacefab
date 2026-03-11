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
        [AssetName(typeof(ChapterAsset))] public StringHash32[] Chapters;

        public int CurrChapterIndex;

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