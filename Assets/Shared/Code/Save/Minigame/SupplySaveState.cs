using FieldDay.Data;
using SpaceFab.Save;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace SpaceFab.Save
{
    public class SupplySaveState : IMinigameSaveState, ISaveStateChunkObject
    {
        // outputs
        public int FinalizedReliability;
        public int FinalizedTotalCycles;
        public int FinalizedCost;

        // layout
        // TODO: paths

        #region Interfaces

        // ISaveStateChunkObject

        public void Read(object self, ref ByteReader reader, SaveStateChunkConsts consts)
        {
            FinalizedReliability = reader.Read<int>();
            FinalizedTotalCycles = reader.Read<int>();
            FinalizedCost = reader.Read<int>();
        }

        public void Write(object self, ref ByteWriter writer, SaveStateChunkConsts consts)
        {
            writer.Write(FinalizedReliability);
            writer.Write(FinalizedTotalCycles);
            writer.Write(FinalizedCost);
        }

        // IMinigameSaveState

        public void SetDefaults()
        {
            FinalizedReliability = -1;
            FinalizedTotalCycles = -1;
            FinalizedCost = -1;
        }

        #endregion // Interfaces
    }
}