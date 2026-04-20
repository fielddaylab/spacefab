using FieldDay.Data;
using SpaceFab.Save;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace SpaceFab.Save
{
    public class ResearchSaveState : IMinigameSaveState, ISaveStateChunkObject
    {
        #region Interfaces

        // ISaveStateChunkObject

        public void Read(object self, ref ByteReader reader, SaveStateChunkConsts consts)
        {

        }

        public void Write(object self, ref ByteWriter writer, SaveStateChunkConsts consts)
        {

        }

        // IMinigameSaveState

        public void SetDefaults()
        {

        }

        #endregion // Interfaces
    }
}