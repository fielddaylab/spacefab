using FieldDay.Data;
using SpaceFab.Save;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace SpaceFab.Save
{
    public class FabricationSaveState : MinigameSaveStateBase, ISaveStateChunkObject
    {
        public int FinalizedTotalCycles;
        public float FinalizedPrecision;

        #region Interfaces

        // ISaveStateChunkObject

        public override void Read(object self, ref ByteReader reader, SaveStateChunkConsts consts)
        {
            base.Read(self, ref reader, consts);

            FinalizedTotalCycles = reader.Read<int>();
            FinalizedPrecision = reader.Read<float>();
        }

        public override void Write(object self, ref ByteWriter writer, SaveStateChunkConsts consts)
        {
            base.Write(self, ref writer, consts);

            writer.Write(FinalizedTotalCycles);
            writer.Write(FinalizedPrecision);
        }

        // IMinigameSaveState

        public override void SetDefaults()
        {
            base.SetDefaults();

            FinalizedTotalCycles = -1;
            FinalizedPrecision = -1;
        }

        #endregion // Interfaces
    }
}