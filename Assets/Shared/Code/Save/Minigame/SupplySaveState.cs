using FieldDay.Data;
using Spacefab.Save;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace SpaceFab.Save
{
    public class SupplySaveState : IMinigameSaveState, ISaveStateChunkObject
    {
        #region Interfaces


        // ISaveStateChunkObject

        public void Read(object self, ref ByteReader reader, SaveStateChunkConsts consts)
        {

        }

        public void Write(object self, ref ByteWriter writer, SaveStateChunkConsts consts)
        {

        }

        #endregion // Interfaces
    }
}