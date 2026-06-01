using FieldDay;
using FieldDay.Data;
using SpaceFab.Design;
using SpaceFab.Save;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace SpaceFab.Save
{
    /// <summary>
    /// Save state for the Design minigame. Holds per-level grids, input toggles, and solved flags
    /// for the ordered list of Design levels under the active contract. The active level the player
    /// works on is derived (first unsolved); see DesignSaveUtility.FirstUnsolvedIndex. The
    /// inherited FoundValidSolution flag is the aggregate "all levels solved" signal, recomputed
    /// on export from FoundValidSolutionForLevel.
    /// </summary>
    public class DesignSaveState : MinigameSaveStateBase, ISaveStateChunkObject
    {
        // Number of Design levels under the active contract. All three arrays below are sized to
        // this count after a contract is applied (see ContractConfirmUtility.ApplyContractByIndex).
        public int LevelCount;

        // Per-level grid topology. GridStacks[i] is level i's grid.
        public GridStack[] GridStacks;

        // Per-level Lo/Hi input toggle states for the toggle-input flow. InputToggles[i] pairs with
        // GridStacks[i]. Written as count-prefixed chunks per level so old/empty saves read clean.
        public InputToggleSaveData[] InputToggles;

        // Per-level solved flag. The contract is complete only when every entry is true; that
        // aggregate is mirrored into the inherited FoundValidSolution on export.
        public bool[] FoundValidSolutionForLevel;

        #region Interfaces

        // ISaveStateChunkObject

        public override void Read(object self, ref ByteReader reader, SaveStateChunkConsts consts)
        {
            base.Read(self, ref reader, consts);

            DesignSaveUtility.ReadLevels(ref reader, consts, this);
        }

        public override void Write(object self, ref ByteWriter writer, SaveStateChunkConsts consts)
        {
            base.Write(self, ref writer, consts);

            DesignSaveUtility.WriteLevels(ref writer, consts, this);
        }

        // IMinigameSaveState

        public override void SetDefaults()
        {
            base.SetDefaults();

            // No contract applied yet — zero levels. Arrays are (re)allocated when a contract is
            // confirmed; nothing reads a level slot before then.
            LevelCount = 0;
            GridStacks = null;
            InputToggles = null;
            FoundValidSolutionForLevel = null;
        }

        #endregion // Interfaces
    }

    public static class DesignSaveUtility
    {
        #region Level Indexing

        // Index of the first level whose solved flag is false — i.e. the level the player should
        // resume on. Clamped to the last level when every level is already solved so callers always
        // get a valid index. Returns 0 for an unseeded (zero-level) save.
        public static int FirstUnsolvedIndex(DesignSaveState saveState)
        {
            if (saveState.LevelCount <= 0) { return 0; }
            for (int i = 0; i < saveState.LevelCount; i++)
            {
                if (!saveState.FoundValidSolutionForLevel[i]) { return i; }
            }
            return saveState.LevelCount - 1;
        }

        // True only when every level under the contract is solved. Mirrored into the inherited
        // FoundValidSolution so overarching's contract-completion check stays correct.
        public static bool AllLevelsSolved(DesignSaveState saveState)
        {
            if (saveState.LevelCount <= 0) { return false; }
            for (int i = 0; i < saveState.LevelCount; i++)
            {
                if (!saveState.FoundValidSolutionForLevel[i]) { return false; }
            }
            return true;
        }

        // Allocates (or resizes) the per-level arrays to hold `count` levels and clears their
        // solved flags. Grid/toggle contents are seeded separately by the contract-apply loop.
        public static void AllocLevels(DesignSaveState saveState, int count)
        {
            saveState.LevelCount = count;
            saveState.GridStacks = new GridStack[count];
            saveState.InputToggles = new InputToggleSaveData[count];
            saveState.FoundValidSolutionForLevel = new bool[count];
        }

        #endregion // Level Indexing

        #region Write

        // Count-prefixed list of levels. Per level: solved flag, grid, then input toggles. The grid
        // and toggle writers are unchanged from the single-level format — only the surrounding loop
        // is new — so a 1-level contract serializes the same bytes it did before (after the count).
        public static void WriteLevels(ref ByteWriter writer, SaveStateChunkConsts consts, DesignSaveState saveState)
        {
            int count = saveState.LevelCount;
            writer.Write(count);
            for (int i = 0; i < count; i++)
            {
                writer.Write(saveState.FoundValidSolutionForLevel[i]);

                GridStack gridStack = saveState.GridStacks[i];
                if (gridStack == null)
                {
                    GridStackUtility.InitEmptyGridStack(ref gridStack, DesignConsts.NUM_GRID_COLS, DesignConsts.NUM_GRID_ROWS);
                    saveState.GridStacks[i] = gridStack;
                }
                WriteGridStack(ref writer, consts, saveState, ref gridStack);

                WriteInputToggles(ref writer, consts, saveState, ref saveState.InputToggles[i]);
            }
        }

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

        // Symmetric reader for WriteLevels. Reads the level count, allocates the per-level arrays,
        // then reads each level's solved flag, grid, and toggles. Tolerates a missing/truncated
        // chunk (legacy save written before this section existed) by reading zero levels.
        public static void ReadLevels(ref ByteReader reader, SaveStateChunkConsts consts, DesignSaveState saveState)
        {
            if (reader.Remaining < sizeof(int))
            {
                AllocLevels(saveState, 0);
                return;
            }

            int count = reader.Read<int>();
            AllocLevels(saveState, count);
            for (int i = 0; i < count; i++)
            {
                saveState.FoundValidSolutionForLevel[i] = reader.Read<bool>();
                ReadGridStack(ref reader, consts, saveState, ref saveState.GridStacks[i]);
                ReadInputToggles(ref reader, consts, saveState, ref saveState.InputToggles[i]);
            }
        }

        public static void ReadGridStack(ref ByteReader reader, SaveStateChunkConsts consts, DesignSaveState saveState, ref GridStack gridStack)
        {
            GridStackUtility.InitEmptyGridStack(ref gridStack, DesignConsts.NUM_GRID_COLS, DesignConsts.NUM_GRID_ROWS);

            // read both layers
            ReadGridLayer(ref reader, consts, saveState, ref gridStack.GridLayers[0]);
            ReadGridLayer(ref reader, consts, saveState, ref gridStack.GridLayers[1]);
        }

        public static void ReadGridLayer(ref ByteReader reader, SaveStateChunkConsts consts, DesignSaveState saveState, ref GridLayer gridLayer)
        {
            GridCell currCell = null;
            for (int row = 0; row < DesignConsts.NUM_GRID_ROWS; row++)
            {
                for (int col = 0; col < DesignConsts.NUM_GRID_COLS; col++)
                {
                    currCell = GridLayerUtility.GetCell(gridLayer, col, row);
                    ReadGridCell(ref reader, consts, saveState, ref currCell);
                    GridLayerUtility.SetCell(gridLayer, col, row, currCell);
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
