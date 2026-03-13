using FieldDay;
using FieldDay.Systems;
using SpaceFab.Overarching;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace SpaceFab
{
    [SysUpdate(GameLoopPhase.Update, 0, UpdateMasks.ChapterMask)]
    public class ChapterLoadSystem : SharedStateSystemBehaviour<ChapterLoadState>
    {
        public override void ProcessWork(float deltaTime)
        {
            base.ProcessWork(deltaTime);

            // TODO: load
        }
    }
}