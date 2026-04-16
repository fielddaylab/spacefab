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
        public GridStack GridStack;

        #region Interfaces


        // ISaveStateChunkObject

        public void Read(object self, ref ByteReader reader, SaveStateChunkConsts consts)
        {

        }

        public void Write(object self, ref ByteWriter writer, SaveStateChunkConsts consts)
        {
            // writer.Write(GridStack);
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