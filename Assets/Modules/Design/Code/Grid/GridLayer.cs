using FieldDay;
using SpaceFab.Design.Visuals;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace SpaceFab.Design
{
    [Serializable]
    public struct Dimensions
    {
        public int X;
        public int Y;

        public Dimensions(int x, int y)
        {
            X = x;
            Y = y;
        }
    }

    /// <summary>
    /// Underlying data representation of a grid layer
    /// </summary>
    public class GridLayer
    {
        public GridCell[] Cells; // accessed in row, col order

        #region Constructor

        public GridLayer(int xDim, int yDim)
        {
            Cells = new GridCell[yDim * xDim];
            for (int row = 0; row < yDim; row++)
            {
                for (int col = 0; col < xDim; col++)
                {
                    var newCell = new GridCell();
                    newCell.InitEdges();
                    GridLayerUtility.SetCell(this, col, row, newCell);
                }
            }
        }

        #endregion // Constructor
    }

    public static class GridLayerUtility
    {
        #region Gets & Sets

        /// <summary>
        /// Access x, y in row, col order
        /// </summary>
        /// <param name="x"></param>
        /// <param name="y"></param>
        /// <returns></returns>
        public static GridCell GetCell(GridLayer layer, int x, int y)
        {
            return GetCellInternal(layer, x, y);
        }

        /// <summary>
        /// Access x, y in row, col order
        /// </summary>
        /// <param name="x"></param>
        /// <param name="y"></param>
        /// <returns></returns>
        public static GridCell GetCell(GridLayer layer, Vector2Int coord)
        {
            return GetCellInternal(layer, coord.x, coord.y);
        }

        private static GridCell GetCellInternal(GridLayer layer, int coordX, int coordY)
        {
            return layer.Cells[coordY * DesignConsts.NUM_GRID_COLS + coordX];
        }

        // Set cell at x, y in row, col order
        // Typically called from setup and configs
        public static void SetCell(GridLayer layer, int x, int y, GridCell cell)
        {
            SetCellInternal(layer, x, y, cell);
        }

        // Set cell at x, y in row, col order
        // Typically called from player interactions
        public static void SetCell(GridLayer layer, Vector2Int coord, GridCell cell)
        {
            SetCellInternal(layer, coord.x, coord.y, cell);
        }

        private static void SetCellInternal(GridLayer layer, int coordX, int coordY, GridCell cell)
        {
            layer.Cells[coordY * DesignConsts.NUM_GRID_COLS + coordX] = cell;
        }

        #endregion // Gets & Sets

        #region Queries 

        public static bool IsCellEmpty(GridLayer layer, int x, int y)
        {
            return GetCell(layer, x, y).CellType == CellType.NONE;
        }

        public static bool IsCellEmpty(GridLayer layer, Vector2Int coord)
        {
            return GetCell(layer, coord.x, coord.y).CellType == CellType.NONE;
        }

        #endregion // Queries
    }
}