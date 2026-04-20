using BeauUtil.Debugger;
using FieldDay;
using FieldDay.Systems;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using SpaceFab.Design.Visuals;

namespace SpaceFab.Design
{
    /// <summary>
    /// Manages Tool mode interactions, in which the player is actively shaping the grid.
    /// Delegates to DrawUtility or EraseUtility depending on the currently selected tool type.
    /// </summary>
    [SysUpdate(FieldDay.GameLoopPhaseMask.Update, 0, UpdateMasks.ToolModeMask)]
    public class ToolInteractSystem : SharedStateSystemBehaviour<ToolModeState, GridStackState, VisualGridStackState>
    {
        public override bool HasWork()
        {
            return base.HasWork() && m_StateA.ActiveTool != ToolType.None;
        }

        public override void ProcessWork(float deltaTime)
        {
            base.ProcessWork(deltaTime);

            ProcessInputs();
        }

        #region Coordinate Inputs

        private void ProcessInputs()
        {
            if (EventSystem.current.IsPointerOverGameObject()) { return; }
            // if (!InteractInputsEnabled) { return; }

            if (Input.GetMouseButtonDown(0))
            {
                HandleLeftMouseDown();
            }
            if (Input.GetMouseButton(0))
            {
                HandleLeftMouseDrag();
            }
            if (Input.GetMouseButtonUp(0))
            {
                HandleLeftMouseUp();
            }
        }

        private void HandleLeftMouseDown()
        {
            // get mouse position in world space
            var worldPos = Game.Rendering.PrimaryCamera.ScreenToWorldPoint(Input.mousePosition);
            var gridPos = new Vector2Int(Mathf.FloorToInt(worldPos.x), Mathf.FloorToInt(worldPos.y));

            // if grid cell is out of bounds:
            if (!GridStackUtility.InBounds(m_StateB, gridPos.x, gridPos.y))
            {
                // do nothing
                return;
            }

            var layer = m_StateB.GridStack.GridLayers[(int)m_StateA.ActiveLayer];
            // if grid cell is empty:
            if (GridLayerUtility.IsCellEmpty(layer, gridPos))
            {
                if (m_StateA.ActiveLayer == StackLayer.Metal)
                {
                    ClickEmptyMLayerCell(gridPos);
                }
                else
                {
                    ClickEmptyTLayerCell(gridPos);
                }
            }
            // if grid cell is full:
            else
            {
                if (m_StateA.ActiveLayer == StackLayer.Metal)
                {
                    ClickOccupiedMLayerCell(gridPos);
                }
                else
                {
                    ClickOccupiedTLayerCell(gridPos);
                }
            }
            Log.Msg("[InteractMgr] Click Coords: (x: " + gridPos.x + " , y: " + gridPos.y + ")");

            // begin dragging
            m_StateA.LastKnownDragCoord = gridPos;
        }

        private void HandleLeftMouseDrag()
        {
            var worldPos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
            var gridPos = new Vector2Int(Mathf.FloorToInt(worldPos.x), Mathf.FloorToInt(worldPos.y));

            if (gridPos == m_StateA.LastKnownDragCoord)
            {
                // no change in drag position
                return;
            }

            if (m_StateA.LastTerminatedDragCoord != -Vector2Int.one)
            {
                // no change from last terminated drag position (player needs to release mouse before new drag can begin)
                return;
            }

            var dif = gridPos - m_StateA.LastKnownDragCoord;
            if (dif.x != 0 && dif.y != 0)
            {
                // only orthogonal movement allowed; collapse to one dimension (x)
                gridPos.y = m_StateA.LastKnownDragCoord.y;
            }

            // if dragging too quickly
            if (Math.Abs(dif.x) > 1 || Math.Abs(dif.y) > 1)
            {
                // terminate drag
                ToolModeUtility.TerminateDrag(m_StateA);
                return;
            }

            // if out of bounds:
            if (!GridStackUtility.InBounds(m_StateB, gridPos.x, gridPos.y))
            {
                // terminate drag
                ToolModeUtility.TerminateDrag(m_StateA);
                return;
            }

            // if within bounds:
            Log.Msg("[InteractMgr] Dragging to (x: " + gridPos.x + " , y: " + gridPos.y + ")");

            var layer = m_StateB.GridStack.GridLayers[(int)m_StateA.ActiveLayer];
            // if grid cell is empty:
            if (GridLayerUtility.IsCellEmpty(layer, gridPos))
            {
                if (m_StateA.ActiveLayer == StackLayer.Metal)
                {
                    DragEmptyMLayerCell(gridPos);
                }
                else
                {
                    DragEmptyTLayerCell(gridPos);
                }
            }
            // if grid cell is full:
            else
            {
                if (m_StateA.ActiveLayer == StackLayer.Metal)
                {
                    DragOccupiedMLayerCell(gridPos);
                }
                else
                {
                    DragOccupiedTLayerCell(gridPos);
                }
            }

            // continue dragging
            if (gridPos != m_StateA.LastTerminatedDragCoord)
            {
                m_StateA.LastKnownDragCoord = gridPos;
            }
        }

        private void HandleLeftMouseUp()
        {
            ToolModeUtility.TerminateDrag(m_StateA, true);
        }

        #endregion // Coordinate Inputs

        #region Clicks

        private void ClickEmptyMLayerCell(Vector2Int gridPos)
        {
            var layer = m_StateB.GridStack.GridLayers[(int)m_StateA.ActiveLayer];
            var cell = GridLayerUtility.GetCell(layer, gridPos);

            // check tool
            switch (m_StateA.ActiveTool)
            {
                case ToolType.Erase:
                    EraseUtility.EraseCellBothLayers(m_StateA, m_StateB, gridPos);
                    break;
                case ToolType.DrawMetal:
                    DrawUtility.DrawMetal(m_StateB, ref cell, gridPos);
                    break;
                case ToolType.DrawVia:
                    DrawUtility.DrawVia(m_StateA, m_StateB, ref cell, gridPos);
                    break;
                case ToolType.DrawGate:
                    DrawUtility.DrawGate(m_StateA, m_StateB, ref cell, gridPos);
                    break;
                default:
                    break;
            }

            SetCellAndUpdateVisuals(layer, gridPos, cell);
        }

        private void ClickEmptyTLayerCell(Vector2Int gridPos)
        {
            var layer = m_StateB.GridStack.GridLayers[(int)m_StateA.ActiveLayer];
            var cell = GridLayerUtility.GetCell(layer, gridPos);

            // check tool
            switch (m_StateA.ActiveTool)
            {
                case ToolType.Erase:
                    EraseUtility.EraseCellBothLayers(m_StateA, m_StateB, gridPos);
                    break;
                case ToolType.DrawNNodes:
                    cell.CellType = CellType.NTransistor;
                    break;
                case ToolType.DrawPNodes:
                    cell.CellType = CellType.PTransistor;
                    break;
                case ToolType.DrawVia:
                    DrawUtility.DrawVia(m_StateA, m_StateB, ref cell, gridPos);
                    break;
                case ToolType.DrawGate:
                    DrawUtility.DrawGate(m_StateA, m_StateB, ref cell, gridPos);
                    break;
                default:
                    break;
            }

            SetCellAndUpdateVisuals(layer, gridPos, cell);
        }

        private void ClickOccupiedMLayerCell(Vector2Int gridPos)
        {
            var layer = m_StateB.GridStack.GridLayers[(int)m_StateA.ActiveLayer];
            var cell = GridLayerUtility.GetCell(layer, gridPos);

            // check tool
            switch (m_StateA.ActiveTool)
            {
                case ToolType.Erase:
                    EraseUtility.EraseCellBothLayers(m_StateA, m_StateB, gridPos);
                    break;
                case ToolType.DrawMetal:
                    // Do nothing. Click only matters if node is empty.
                    break;
                case ToolType.DrawVia:
                    // place a via if metal
                    if (cell.CellType == CellType.Metal)
                    {
                        DrawUtility.DrawVia(m_StateA, m_StateB, ref cell, gridPos);
                    }
                    break;
                case ToolType.DrawGate:
                    // place a gate if metal
                    if (cell.CellType == CellType.Metal)
                    {
                        DrawUtility.DrawGate(m_StateA, m_StateB, ref cell, gridPos);
                    }
                    break;
                default:
                    break;
            }

            SetCellAndUpdateVisuals(layer, gridPos, cell);
        }

        private void ClickOccupiedTLayerCell(Vector2Int gridPos)
        {
            var layer = m_StateB.GridStack.GridLayers[(int)m_StateA.ActiveLayer];
            var cell = GridLayerUtility.GetCell(layer, gridPos);

            // check tool
            switch (m_StateA.ActiveTool)
            {
                case ToolType.Erase:
                    EraseUtility.EraseCellBothLayers(m_StateA, m_StateB, gridPos);
                    break;
                case ToolType.DrawNNodes:
                    // only relevant if the occupied cell is a transistor
                    if (cell.CellType == CellType.NTransistor || cell.CellType == CellType.PTransistor)
                    {
                        cell.CellType = CellType.NTransistor;
                        // note: preserves edge connections
                    }
                    break;
                case ToolType.DrawPNodes:
                    // only relevant if the occupied cell is a transistor
                    if (cell.CellType == CellType.NTransistor || cell.CellType == CellType.PTransistor)
                    {
                        cell.CellType = CellType.PTransistor;
                        // note: preserves edge connections
                    }
                    break;
                case ToolType.DrawVia:
                    // place a via if transistor
                    if (cell.CellType == CellType.NTransistor || cell.CellType == CellType.PTransistor)
                    {
                        DrawUtility.DrawVia(m_StateA, m_StateB, ref cell, gridPos);
                    }
                    break;
                case ToolType.DrawGate:
                    // place a gate if transistor
                    if (cell.CellType == CellType.NTransistor || cell.CellType == CellType.PTransistor)
                    {
                        DrawUtility.DrawGate(m_StateA, m_StateB, ref cell, gridPos);
                    }
                    break;
                default:
                    break;
            }

            SetCellAndUpdateVisuals(layer, gridPos, cell);
        }

        #endregion // Clicks

        #region Dragging

        private void DragEmptyMLayerCell(Vector2Int gridPos)
        {
            var layer = m_StateB.GridStack.GridLayers[(int)m_StateA.ActiveLayer];
            var cell = GridLayerUtility.GetCell(layer, gridPos);

            // check tool
            switch (m_StateA.ActiveTool)
            {
                case ToolType.Erase:
                    EraseUtility.EraseCellBothLayers(m_StateA, m_StateB, gridPos);
                    break;
                case ToolType.DrawMetal:
                    DrawUtility.DragDrawNodeOfType(m_StateA, m_StateB, CellType.Metal, gridPos);
                    break;
                default:
                    break;
            }
        }

        private void DragEmptyTLayerCell(Vector2Int gridPos)
        {
            var layer = m_StateB.GridStack.GridLayers[(int)m_StateA.ActiveLayer];
            var cell = GridLayerUtility.GetCell(layer, gridPos);

            // check tool
            switch (m_StateA.ActiveTool)
            {
                case ToolType.Erase:
                    EraseUtility.EraseCellBothLayers(m_StateA, m_StateB, gridPos);
                    break;
                case ToolType.DrawNNodes:
                    DrawUtility.DragDrawNodeOfType(m_StateA, m_StateB, CellType.NTransistor, gridPos);
                    break;
                case ToolType.DrawPNodes:
                    DrawUtility.DragDrawNodeOfType(m_StateA, m_StateB, CellType.PTransistor, gridPos);
                    break;
                default:
                    break;
            }
        }

        private void DragOccupiedMLayerCell(Vector2Int gridPos)
        {
            var layer = m_StateB.GridStack.GridLayers[(int)m_StateA.ActiveLayer];
            var cell = GridLayerUtility.GetCell(layer, gridPos);

            // check tool
            switch (m_StateA.ActiveTool)
            {
                case ToolType.Erase:
                    EraseUtility.EraseCellBothLayers(m_StateA, m_StateB, gridPos);
                    break;
                case ToolType.DrawMetal:
                    DrawUtility.DragDrawNodeOfType(m_StateA, m_StateB, CellType.Metal, gridPos);
                    break;
                default:
                    break;
            }
        }

        private void DragOccupiedTLayerCell(Vector2Int gridPos)
        {
            var layer = m_StateB.GridStack.GridLayers[(int)m_StateA.ActiveLayer];
            var cell = GridLayerUtility.GetCell(layer, gridPos);

            // check tool
            switch (m_StateA.ActiveTool)
            {
                case ToolType.Erase:
                    EraseUtility.EraseCellBothLayers(m_StateA, m_StateB, gridPos);
                    break;
                case ToolType.DrawNNodes:
                    // do not allow dragging onto inputs/outputs
                    if (cell.CellType == CellType.Input || cell.CellType == CellType.Output)
                    {
                        ToolModeUtility.TerminateDrag(m_StateA);
                        return;
                    }
                    else
                    {
                        if (cell.CellType == CellType.PTransistor)
                        {
                            // draw connection, preserve type
                            DrawUtility.DragDrawNodeOfType(m_StateA, m_StateB, CellType.PTransistor, gridPos);
                        }
                        else
                        {
                            // override
                            DrawUtility.DragDrawNodeOfType(m_StateA, m_StateB, CellType.NTransistor, gridPos);
                        }
                    }
                    break;
                case ToolType.DrawPNodes:
                    // do not allow dragging onto inputs/outputs
                    if (cell.CellType == CellType.Input || cell.CellType == CellType.Output)
                    {
                        ToolModeUtility.TerminateDrag(m_StateA);
                        return;
                    }
                    else
                    {
                        if (cell.CellType == CellType.NTransistor)
                        {
                            // draw connection, preserve type
                            DrawUtility.DragDrawNodeOfType(m_StateA, m_StateB, CellType.NTransistor, gridPos);
                        }
                        else
                        {
                            // override
                            DrawUtility.DragDrawNodeOfType(m_StateA, m_StateB, CellType.PTransistor, gridPos);
                        }
                    }
                    break;
                default:
                    break;
            }
        }

        #endregion // Dragging

        #region Helpers

        private void SetCellAndUpdateVisuals(GridLayer layer, Vector2Int gridPos, GridCell cell)
        {
            GridLayerUtility.SetCell(layer, gridPos, cell);
            m_StateC.VisualsNeedRefreshing = true;
        }

        #endregion // Helpers
    }
}