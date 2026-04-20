using FieldDay;
using FieldDay.SharedState;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace SpaceFab.Design
{
    public class GridStackState : SharedStateComponent, IRegistrationCallbacks
    {
        public GridStack GridStack;

        public void OnDeregister()
        {
        }

        public void OnRegister()
        {
            GridStackUtility.InitEmptyGridStack(ref GridStack, DesignConsts.NUM_GRID_COLS, DesignConsts.NUM_GRID_ROWS);
            // LoadConfig(LevelMgr.Instance.CurrLevelData.GetGridConfig());
            // Game.Events.Dispatch(GameEvents.NewGridStackCreated);
        }
    }

    public static class GridStackUtility
    {
        public static void InitEmptyGridStack(ref GridStack gridStack, int numCols, int numRows)
        {
            gridStack = new GridStack();
            gridStack.LayerDims = new Dimensions(numCols, numRows);
            gridStack.GridLayers = new GridLayer[2]
            {
                new GridLayer(gridStack.LayerDims.X, gridStack.LayerDims.Y),  // metal layer (highest)
                new GridLayer(gridStack.LayerDims.X, gridStack.LayerDims.Y)   // transistor layer (lowest)
            };
        }

        #region Loading

        public static void LoadConfig(ref GridStack gridStack, GridStackConfig config)
        {
            InitEmptyGridStack(ref gridStack, config.LayerDims.X, config.LayerDims.Y);
            for (int i = 0; i < config.Cells.Length; i++)
            {
                LoadCellConfig(ref gridStack, config.Cells[i]);
            }
        }

        public static void LoadCellConfig(ref GridStack gridStack, GridCellConfig config)
        {
            var cell = GridLayerUtility.GetCell(gridStack.GridLayers[(int)config.LayerIndex], config.ColumnIndex, config.RowIndex);
            cell.LoadCellConfig(config);
            GridLayerUtility.SetCell(gridStack.GridLayers[(int)config.LayerIndex], config.ColumnIndex, config.RowIndex, cell);
        }

        #endregion // Loading

        #region Queries

        public static bool InBounds(GridStackState state, int x, int y)
        {
            if (x < 0 || y < 0) { return false; }
            if (x >= state.GridStack.LayerDims.X || y >= state.GridStack.LayerDims.Y) { return false; }

            return true;
        }

        public static GridCell GetCellDirect(GridStackState state, int layer, int col, int row)
        {
            return GridLayerUtility.GetCell(state.GridStack.GridLayers[layer], col, row);
        }

        public static GridCell GetCellDirect(GridStackState state, GridCoord coord)
        {
            return GridLayerUtility.GetCell(state.GridStack.GridLayers[coord.Layer], coord.Col, coord.Row);
        }

        public static void SetCellDirect(GridStackState state, int layer, int col, int row, GridCell cell)
        {
            GridLayerUtility.SetCell(state.GridStack.GridLayers[layer], col, row, cell);
        }

        public static void SetCellDirect(GridStackState state, GridCoord coord, GridCell cell)
        {
            GridLayerUtility.SetCell(state.GridStack.GridLayers[coord.Layer], coord.Col, coord.Row, cell);
        }

        public static EdgeDir DirFromToCell(Vector2Int fromPos, Vector2Int toPos)
        {
            var dif = toPos - fromPos;

            if (dif.x == 1)
            {
                return EdgeDir.EAST;
            }
            else if (dif.x == -1)
            {
                return EdgeDir.WEST;
            }
            else if (dif.y == 1)
            {
                return EdgeDir.NORTH;
            }
            else // (dif.y == 0)
            {
                return EdgeDir.SOUTH;
            }
        }

        public static void GetOffsetOfDir(EdgeDir dir, out Vector2Int gridOffset, out int layerOffset)
        {
            layerOffset = 0;
            gridOffset = Vector2Int.zero;

            switch (dir)
            {
                case EdgeDir.NORTH:
                    gridOffset.y = 1;
                    break;
                case EdgeDir.EAST:
                    gridOffset.x = 1;
                    break;
                case EdgeDir.ASCEND:
                    layerOffset = -1;
                    break;
                case EdgeDir.SOUTH:
                    gridOffset.y = -1;
                    break;
                case EdgeDir.WEST:
                    gridOffset.x = -1;
                    break;
                case EdgeDir.DESCEND:
                    layerOffset = 1;
                    break;
                default:
                    break;
            }
        }

        public static StackLayer GetOppositeLayer(StackLayer layer)
        {
            if (layer == StackLayer.Metal) { return StackLayer.Transistor; }
            else { return StackLayer.Metal; }
        }

        public static GridCell GetAdjCell(GridStackState gridState, Vector2Int gridPos, EdgeDir dir, int currLayer)
        {
            int layerOffset = 0;
            Vector2Int gridOffset = Vector2Int.zero;

            GridStackUtility.GetOffsetOfDir(dir, out gridOffset, out layerOffset);

            var adjGridPos = gridPos + gridOffset;
            var adjLayerIndex = currLayer + layerOffset;

            var adjLayer = gridState.GridStack.GridLayers[adjLayerIndex];

            GridCell adjCell = GridLayerUtility.GetCell(adjLayer, adjGridPos);

            return adjCell;
        }

        public static EdgeDir GetOppositeDir(EdgeDir original)
        {
            EdgeDir opposite = (EdgeDir)(((int)original + Enum.GetValues(typeof(EdgeDir)).Length / 2) % Enum.GetValues(typeof(EdgeDir)).Length);

            return opposite;
        }

        #endregion // Queries
    }
}