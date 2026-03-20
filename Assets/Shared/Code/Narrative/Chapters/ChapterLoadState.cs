using BeauUtil;
using FieldDay.Assets;
using FieldDay.SharedState;
using System;
using System.Collections;
using System.Collections.Generic;
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
}