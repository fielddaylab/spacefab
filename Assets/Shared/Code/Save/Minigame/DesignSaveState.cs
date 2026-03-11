using FieldDay.Data;
using SpaceFab.Save;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace SpaceFab.Save
{
    public class DesignSaveState : IMinigameSaveState, ISaveStateChunkObject
    {
        #region Interfaces


        // ISaveStateChunkObject

        public void Read(object self, ref ByteReader reader, SaveStateChunkConsts consts)
        {
            /*
            float volume = reader.Read<float>();
            SettingsUtility.SetMasterVolume(this, volume);
            */
        }

        public void Write(object self, ref ByteWriter writer, SaveStateChunkConsts consts)
        {
            /*
            writer.Write((float)MasterVolume);
            */
        }

        #endregion // Interfaces
    }
}