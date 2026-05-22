using FieldDay.Data;
using SpaceFab.Save;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace SpaceFab.Save
{
    public class ResearchSaveState : IMinigameSaveState, ISaveStateChunkObject
    {
        public bool FoundValidSolution;

        #region Interfaces

        // ISaveStateChunkObject

        public void Read(object self, ref ByteReader reader, SaveStateChunkConsts consts)
        {
            FoundValidSolution = reader.Read<bool>();
        }

        public void Write(object self, ref ByteWriter writer, SaveStateChunkConsts consts)
        {
            writer.Write(FoundValidSolution);
        }

        // IMinigameSaveState

        public void SetDefaults()
        {

        }

        #endregion // Interfaces
    }
}