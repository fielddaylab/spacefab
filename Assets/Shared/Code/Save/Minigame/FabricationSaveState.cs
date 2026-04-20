using FieldDay.Data;
using SpaceFab.Save;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace SpaceFab.Save
{
    public class FabricationSaveState : IMinigameSaveState, ISaveStateChunkObject
    {
        public int TotalCycles;
        public float Precision;

        #region Interfaces

        // ISaveStateChunkObject

        public void Read(object self, ref ByteReader reader, SaveStateChunkConsts consts)
        {
            TotalCycles = reader.Read<int>();
            Precision = reader.Read<float>();
        }

        public void Write(object self, ref ByteWriter writer, SaveStateChunkConsts consts)
        {
            writer.Write(TotalCycles);
            writer.Write(Precision);
        }

        // IMinigameSaveState

        public void SetDefaults()
        {
            TotalCycles = -1;
            Precision = -1;
        }

        #endregion // Interfaces
    }
}