using FieldDay;
using FieldDay.Data;
using SpaceFab.Design;
using SpaceFab.Save;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace SpaceFab.Save
{
    public class DesignSaveState : MinigameSaveStateBase, ISaveStateChunkObject
    {
        public GridStack GridStack;

        // Per-input Lo/Hi toggle states for the new toggle-input flow. Written as a separate
        // count-prefixed chunk after the grid so old save files that lack this section read as
        // empty (count = 0) when reader.Remaining drops to 0 inside ReadInputToggles.
        public InputToggleSaveData InputToggles;

        #region Interfaces

        // ISaveStateChunkObject

        public override void Read(object self, ref ByteReader reader, SaveStateChunkConsts consts)
        {
            base.Read(self, ref reader, consts);

            DesignSaveUtility.ReadGridStack(ref reader, consts, this, ref GridStack);
            DesignSaveUtility.ReadInputToggles(ref reader, consts, this, ref InputToggles);
        }

        public override void Write(object self, ref ByteWriter writer, SaveStateChunkConsts consts)
        {
            base.Write(self, ref writer, consts);

            if (GridStack == null)
            {
                GridStackUtility.InitEmptyGridStack(ref GridStack, DesignConsts.NUM_GRID_COLS, DesignConsts.NUM_GRID_ROWS);
            }

            DesignSaveUtility.WriteGridStack(ref writer, consts, this, ref GridStack);
            DesignSaveUtility.WriteInputToggles(ref writer, consts, this, ref InputToggles);
        }

        // IMinigameSaveState

        public override void SetDefaults()
        {
            base.SetDefaults();

            GridStackUtility.InitEmptyGridStack(ref GridStack, DesignConsts.NUM_GRID_COLS, DesignConsts.NUM_GRID_ROWS);
            InputToggles = default;
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
                    currCell = GridLayerUtility.GetCell(gridLayer, col, row);
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

        // Count-prefixed list of input toggles. Each entry is a flat cell index (encoded by
        // SimulateRunScratchUtility.CellIndex from (layer, col, row)), the input subtype label,
        // and the player's chosen Lo/Hi state. Decoupled from the grid-cell chunk so the structural
        // grid topology stays clean.
        public static void WriteInputToggles(ref ByteWriter writer, SaveStateChunkConsts consts, DesignSaveState saveState, ref InputToggleSaveData data)
        {
            int count = data.Count;
            writer.Write(count);
            for (int i = 0; i < count; i++)
            {
                InputToggleSaveEntry entry = data.Entries[i];
                writer.Write(entry.CellIndex);
                writer.Write(entry.Subtype);
                writer.Write(entry.State);
            }
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
                    currCell = GridLayerUtility.GetCell(saveState.GridStack.GridLayers[layerIndex], col, row);
                    ReadGridCell(ref reader, consts, saveState, ref currCell);
                    GridLayerUtility.SetCell(saveState.GridStack.GridLayers[layerIndex], col, row, currCell);
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

        // Symmetric reader for WriteInputToggles. Tolerates an empty / missing chunk on saves
        // written before this section existed by checking reader.Remaining before each int.
        public static void ReadInputToggles(ref ByteReader reader, SaveStateChunkConsts consts, DesignSaveState saveState, ref InputToggleSaveData data)
        {
            if (reader.Remaining < sizeof(int))
            {
                data.Count = 0;
                return;
            }

            int count = reader.Read<int>();
            if (count <= 0)
            {
                data.Count = 0;
                return;
            }

            if (data.Entries == null || data.Entries.Length < count)
            {
                data.Entries = new InputToggleSaveEntry[count];
            }

            for (int i = 0; i < count; i++)
            {
                data.Entries[i].CellIndex = reader.Read<int>();
                data.Entries[i].Subtype = reader.Read<InputOutputNodeTypeFlags>();
                data.Entries[i].State = reader.Read<FlowState>();
            }
            data.Count = count;
        }

        #endregion // Read
    }
}