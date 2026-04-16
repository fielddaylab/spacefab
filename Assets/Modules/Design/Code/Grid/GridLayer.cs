using FieldDay;
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
        private int m_XDim; // cols per row
        private GridCell[] m_Cells; // accessed in row, col order

        #region Constructor

        public GridLayer(int xDim, int yDim)
        {
            m_XDim = xDim;
            m_Cells = new GridCell[yDim * xDim];
            for (int row = 0; row < yDim; row++)
            {
                for (int col = 0; col < xDim; col++)
                {
                    var newCell = new GridCell();
                    newCell.InitEdges();
                    SetCell(col, row, newCell);
                }
            }
        }

        #endregion // Constructor

        #region Gets & Sets

        /// <summary>
        /// Access x, y in row, col order
        /// </summary>
        /// <param name="x"></param>
        /// <param name="y"></param>
        /// <returns></returns>
        public GridCell GetCell(int x, int y)
        {
            return m_Cells[y * m_XDim + x];
        }

        /// <summary>
        /// Access x, y in row, col order
        /// </summary>
        /// <param name="x"></param>
        /// <param name="y"></param>
        /// <returns></returns>
        public GridCell GetCell(Vector2Int coord)
        {
            return m_Cells[coord.y * m_XDim + coord.x];
        }

        // Set cell at x, y in row, col order
        public void SetCell(int x, int y, GridCell cell)
        {
            m_Cells[y * m_XDim + x] = cell;

            //Game.Events.Dispatch(GameEvents.OnLayoutChanged);
        }

        // Set cell at x, y in row, col order
        public void SetCell(Vector2Int coord, GridCell cell)
        {
            m_Cells[coord.y * m_XDim + coord.x] = cell;

            //Game.Events.Dispatch(GameEvents.OnLayoutChanged);
        }

        #endregion // Gets & Sets

        #region Queries 

        public bool IsCellEmpty(int x, int y)
        {
            return GetCell(x, y).CellType == CellType.NONE;
        }

        public bool IsCellEmpty(Vector2Int coord)
        {
            return GetCell(coord.x, coord.y).CellType == CellType.NONE;
        }

        #endregion // Queries
    }
}