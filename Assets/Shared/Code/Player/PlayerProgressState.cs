using BeauUtil;
using FieldDay.Data;
using FieldDay.SharedState;
using Spacefab.Save;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Spacefab
{
    public class PlayerProgressState : SharedStateComponent, ISaveStateChunkObject
    {
        public HashSet<StringHash32> AvailableMaterials;
        public HashSet<StringHash32> ResearchedMaterials;

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