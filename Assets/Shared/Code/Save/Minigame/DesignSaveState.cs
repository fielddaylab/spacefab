using FieldDay;
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
            DesignSaveUtility.ReadGridStack(ref reader, consts, this, ref GridStack);
        }

        public void Write(object self, ref ByteWriter writer, SaveStateChunkConsts consts)
        {
            if (GridStack == null)
            {
                GridStackUtility.InitEmptyGridStack(ref GridStack, DesignConsts.NUM_GRID_COLS, DesignConsts.NUM_GRID_ROWS);
            }

            DesignSaveUtility.WriteGridStack(ref writer, consts, this, ref GridStack);
        }

        #endregion // Interfaces
    }

    public static class DesignSaveUtility
    {
        #region Write

        public static void WriteGridStack(ref ByteWriter writer, SaveStateChunkConsts consts, DesignSaveState saveState, ref GridStack gridStack)
        {
            // write both layers
            WriteGridLayer(ref writer, consts, saveState, ref gridStack.GridLayers[0]);
            WriteGridLayer(ref writer, consts, saveState, ref gridStack.GridLayers[1]);
        }

        public static void WriteGridLayer(ref ByteWriter writer, SaveStateChunkConsts consts, DesignSaveState saveState, ref GridLayer gridLayer)
        {
            // within each layer, write 6x8 cells
            GridCell currCell = null;
            for (int row = 0; row < DesignConsts.NUM_GRID_ROWS; row++)
            {
                for (int col = 0; col < DesignConsts.NUM_GRID_COLS; col++)
                {
                    currCell = gridLayer.GetCell(col, row);
                    WriteGridCell(ref writer, consts, saveState, ref currCell);
                }
            }
        }

        public static void WriteGridCell(ref ByteWriter writer, SaveStateChunkConsts consts, DesignSaveState saveState, ref GridCell gridCell)
        {
            writer.Write(gridCell.CellType);
            writer.Write(gridCell.SubtypeLabel);
            WriteGridCellEdges(ref writer, consts, saveState, ref gridCell);
            writer.Write(gridCell.TransferType);
            writer.Write(gridCell.NodeEraseable);
            writer.Write(gridCell.TransferEraseable);
        }

        public static void WriteGridCellEdges(ref ByteWriter writer, SaveStateChunkConsts consts, DesignSaveState saveState, ref GridCell gridCell)
        {
            writer.Write(gridCell.Edges[0]);
            writer.Write(gridCell.Edges[1]);
            writer.Write(gridCell.Edges[2]);
            writer.Write(gridCell.Edges[3]);
            writer.Write(gridCell.Edges[4]);
            writer.Write(gridCell.Edges[5]);
        }

        #endregion // Write

        #region Read

        public static void ReadGridStack(ref ByteReader reader, SaveStateChunkConsts consts, DesignSaveState saveState, ref GridStack gridStack)
        {
            GridStackUtility.InitEmptyGridStack(ref gridStack, DesignConsts.NUM_GRID_COLS, DesignConsts.NUM_GRID_ROWS);

            // read both layers
            ReadGridLayer(ref reader, consts, saveState, 0);
            ReadGridLayer(ref reader, consts, saveState, 1);
        }

        public static void ReadGridLayer(ref ByteReader reader, SaveStateChunkConsts consts, DesignSaveState saveState, int layerIndex)
        {
            GridCell currCell = null;
            for (int row = 0; row < DesignConsts.NUM_GRID_ROWS; row++)
            {
                for (int col = 0; col < DesignConsts.NUM_GRID_COLS; col++)
                {
                    currCell = saveState.GridStack.GridLayers[layerIndex].GetCell(col, row);
                    ReadGridCell(ref reader, consts, saveState, ref currCell);
                    saveState.GridStack.GridLayers[layerIndex].SetCell(col, row, currCell);
                }
            }
        }

        public static void ReadGridCell(ref ByteReader reader, SaveStateChunkConsts consts, DesignSaveState saveState, ref GridCell gridCell)
        {
            gridCell.CellType = reader.Read<CellType>();
            gridCell.SubtypeLabel = reader.Read<InputOutputNodeTypeFlags>();
            ReadGridCellEdges(ref reader, consts, saveState, ref gridCell);
            gridCell.TransferType = reader.Read<TransferType>();
            gridCell.NodeEraseable = reader.Read<bool>();
            gridCell.TransferEraseable = reader.Read<bool>();
        }

        public static void ReadGridCellEdges(ref ByteReader reader, SaveStateChunkConsts consts, DesignSaveState saveState, ref GridCell gridCell)
        {
            gridCell.Edges[0] = reader.Read<EdgeStateData>();
            gridCell.Edges[1] = reader.Read<EdgeStateData>();
            gridCell.Edges[2] = reader.Read<EdgeStateData>();
            gridCell.Edges[3] = reader.Read<EdgeStateData>();
            gridCell.Edges[4] = reader.Read<EdgeStateData>();
            gridCell.Edges[5] = reader.Read<EdgeStateData>();
        }

        #endregion // Read
    }
}