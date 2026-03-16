using FieldDay.Assets;
using FieldDay.SharedState;
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
    public class ChapterLoadState : SharedStateComponent
    {
        public ChapterLoadPhase Phase;
        public AssetPack[] Chapters;
    }
}