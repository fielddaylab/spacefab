using FieldDay.Data;
using SpaceFab.Save;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace SpaceFab.Save
{
    public class FabricationSaveState : IMinigameSaveState, ISaveStateChunkObject
    {
        public int FinalizedTotalCycles;
        public float FinalizedPrecision;

        public bool FoundValidSolution;

        #region Interfaces

        // ISaveStateChunkObject

        public void Read(object self, ref ByteReader reader, SaveStateChunkConsts consts)
        {
            FinalizedTotalCycles = reader.Read<int>();
            FinalizedPrecision = reader.Read<float>();

            FoundValidSolution = reader.Read<bool>();
        }

        public void Write(object self, ref ByteWriter writer, SaveStateChunkConsts consts)
        {
            writer.Write(FinalizedTotalCycles);
            writer.Write(FinalizedPrecision);

            writer.Write(FoundValidSolution);
        }

        // IMinigameSaveState

        public void SetDefaults()
        {
            FinalizedTotalCycles = -1;
            FinalizedPrecision = -1;
        }

        #endregion // Interfaces
    }
}