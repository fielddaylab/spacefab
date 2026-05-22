using FieldDay.Data;
using SpaceFab.Save;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace SpaceFab.Save
{
    public class SupplySaveState : MinigameSaveStateBase, ISaveStateChunkObject
    {
        // outputs
        public int FinalizedReliability;
        public int FinalizedTotalCycles;
        public int FinalizedCost;

        // layout
        // TODO: paths

        #region Interfaces

        // ISaveStateChunkObject

        public override void Read(object self, ref ByteReader reader, SaveStateChunkConsts consts)
        {
            base.Read(self, ref reader, consts);

            FinalizedReliability = reader.Read<int>();
            FinalizedTotalCycles = reader.Read<int>();
            FinalizedCost = reader.Read<int>();
        }

        public override void Write(object self, ref ByteWriter writer, SaveStateChunkConsts consts)
        {
            base.Write(self, ref writer, consts);

            writer.Write(FinalizedReliability);
            writer.Write(FinalizedTotalCycles);
            writer.Write(FinalizedCost);
        }

        // IMinigameSaveState

        public override void SetDefaults()
        {
            base.SetDefaults();

            FinalizedReliability = -1;
            FinalizedTotalCycles = -1;
            FinalizedCost = -1;
        }

        #endregion // Interfaces
    }
}