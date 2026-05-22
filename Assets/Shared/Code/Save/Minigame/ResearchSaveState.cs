using FieldDay.Data;
using SpaceFab.Save;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace SpaceFab.Save
{
    public class ResearchSaveState : MinigameSaveStateBase, ISaveStateChunkObject
    {
        #region Interfaces

        // ISaveStateChunkObject

        public override void Read(object self, ref ByteReader reader, SaveStateChunkConsts consts)
        {
            base.Read(self, ref reader, consts);
        }

        public override void Write(object self, ref ByteWriter writer, SaveStateChunkConsts consts)
        {
            base.Write(self, ref writer, consts);
        }

        // IMinigameSaveState

        public override void SetDefaults()
        {
            base.SetDefaults();
        }

        #endregion // Interfaces
    }
}