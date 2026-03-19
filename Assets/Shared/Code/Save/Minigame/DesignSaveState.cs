using FieldDay.Data;
using SpaceFab.Design;
using SpaceFab.Save;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace SpaceFab.Save
{
    public class DesignSaveState : IMinigameSaveState, ISaveStateChunkObject
    {
        public GridStack GridStack; // TODO: load from config as soon as contract is selected!

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
            // writer.Write(GridStack);
            /*
            writer.Write((float)MasterVolume);
            */
        }

        /*
        static private void WriteGridLayer(GridLayer layer, ref ByteWriter writer, SaveStateChunkConsts consts)
        {
            writer.Write(layer.LayerIndex);
            for(int y = 0; y < layer.Dimensions.Y; y++)
            {
                WriteGridCell(layer.)
            }
        }

        static private void WriteGridCell(GridCell cell, ref ByteWriter writer)
        {

        }
        */

        #endregion // Interfaces
    }
}