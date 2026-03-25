using BeauUtil;
using FieldDay;
using FieldDay.Assets;
using FieldDay.Data;
using FieldDay.SharedState;
using SpaceFab.Save;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace SpaceFab
{
    public class PlayerProgressState : SharedStateComponent, ISaveStateChunkObject, IRegistrationCallbacks
    {
        public HashSet<StringHash32> AvailableMaterials;
        public HashSet<StringHash32> ResearchedMaterials;

        public bool RecentlyCompletedChapter;

        public void OnDeregister()
        {
        }

        public void OnRegister()
        {
            SpacefabGame.SaveBuffer.RegisterHandler("PlayerProgressState", this);
        }

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