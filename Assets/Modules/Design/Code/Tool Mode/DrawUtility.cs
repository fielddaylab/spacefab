using SpaceFab.Design.Visuals;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using static BeauRoutine.Future;

namespace SpaceFab.Design
{
    public static class DrawUtility
    {
        public static void DrawMetal(ref GridCell cell)
        {
            if (cell.CellType == CellType.Input || cell.CellType == CellType.Output) return;

            cell.CellType = CellType.Metal;
        }

        public static void DrawVia(ToolModeState toolModeState, GridStackState gridState, ref GridCell cell, Vector2Int gridPos)
        {
            Debug.Log($"<color=red>TransferEraseable {cell.TransferEraseable}</color>");//TODO
            if (cell.CellType == CellType.Input || cell.CellType == CellType.Output) { return; }

            StackLayer linkedLayerType = toolModeState.ActiveLayer == StackLayer.Metal ? StackLayer.Transistor : StackLayer.Metal;
            var linkedLayer = gridState.GridStack.GridLayers[(int)linkedLayerType];
            var linkedCell = GridLayerUtility.GetCell(linkedLayer, gridPos);

            if (linkedCell.CellType == CellType.Input || linkedCell.CellType == CellType.Output) { return; }

            cell.TransferType = TransferType.Via;
            //linkedCell.TransferType = TransferType.Via;

            int cellEdgeIndex = toolModeState.ActiveLayer == StackLayer.Metal ? (int)EdgeDir.DESCEND : (int)EdgeDir.ASCEND;
            int linkedEdgeIndex = toolModeState.ActiveLayer == StackLayer.Metal ? (int)EdgeDir.ASCEND : (int)EdgeDir.DESCEND;
            cell.Edges[cellEdgeIndex].EdgeState = EdgeState.Connected;
            linkedCell.Edges[linkedEdgeIndex].EdgeState = EdgeState.Connected;

            Debug.Log("<color=cyan>Via drawn</color>");//TODO
        }

        public static void DrawGate(ToolModeState toolModeState, GridStackState gridState, ref GridCell cell, Vector2Int gridPos)
        {
            if (cell.CellType == CellType.Input || cell.CellType == CellType.Output || !cell.TransferEraseable) { return; }

            StackLayer linkedLayerType = toolModeState.ActiveLayer == StackLayer.Metal ? StackLayer.Transistor : StackLayer.Metal;
            var linkedLayer = gridState.GridStack.GridLayers[(int)linkedLayerType];
            var linkedCell = GridLayerUtility.GetCell(linkedLayer, gridPos);

            if (linkedCell.CellType == CellType.Input || linkedCell.CellType == CellType.Output) { return; }

            cell.TransferType = toolModeState.ActiveLayer == StackLayer.Metal ? TransferType.GateAbove : TransferType.GateBelow;
            linkedCell.TransferType = toolModeState.ActiveLayer == StackLayer.Metal ? TransferType.GateBelow : TransferType.GateAbove;

            /*
            int cellEdgeIndex = toolModeState.ActiveLayer == StackLayer.Metal ? (int)EdgeDir.DESCEND : (int)EdgeDir.ASCEND;
            int linkedEdgeIndex = toolModeState.ActiveLayer == StackLayer.Metal ? (int)EdgeDir.ASCEND : (int)EdgeDir.DESCEND;
            cell.Edges[cellEdgeIndex] = EdgeState.Connected;
            linkedCell.Edges[linkedEdgeIndex] = EdgeState.Connected;
            */

            Debug.Log("<color=cyan>Gate drawn</color>");//TODO
        }

        public static void ConnectToMetalLayer(GridStackState gridState, ref GridCell cell, Vector2Int gridPos)
        {
            // in there's metal above, connect edge
            StackLayer linkedLayerType = StackLayer.Metal;
            var linkedLayer = gridState.GridStack.GridLayers[(int)linkedLayerType];
            var linkedCell = GridLayerUtility.GetCell(linkedLayer, gridPos);

            if (linkedCell.CellType == CellType.Metal)
            {
                cell.TransferType = TransferType.Implicit;
                linkedCell.TransferType = TransferType.Implicit;

                int cellEdgeIndex = (int)EdgeDir.ASCEND;
                int linkedEdgeIndex = (int)EdgeDir.DESCEND;
                cell.Edges[cellEdgeIndex].EdgeState = EdgeState.Connected;
                linkedCell.Edges[linkedEdgeIndex].EdgeState = EdgeState.Connected;
            }
        }

        public static void DragDrawNodeOfType(ToolModeState toolModeState, GridStackState gridState, VisualGridStackState visualState, CellType type, Vector2Int gridPos)
        {
            // create edge between last known pos and curr pos
            var layer = gridState.GridStack.GridLayers[(int)toolModeState.ActiveLayer];
            var fromCell = GridLayerUtility.GetCell(layer, toolModeState.LastKnownDragCoord);
            var toCell = GridLayerUtility.GetCell(layer, gridPos);
            var fromDir = GridStackUtility.DirFromToCell(toolModeState.LastKnownDragCoord, gridPos);
            var reverseDir = GridStackUtility.GetOppositeDir(fromDir);

            // disallow drag from inputs/outputs on transistor layer
            if (type == CellType.NTransistor || type == CellType.PTransistor)
            {
                if (fromCell.CellType == CellType.Input || fromCell.CellType == CellType.Input)
                {
                    ToolModeUtility.TerminateDrag(toolModeState);
                    return;
                }
            }

            fromCell.Edges[(int)fromDir].EdgeState = EdgeState.Connected;
            toCell.Edges[(int)reverseDir].EdgeState = EdgeState.Connected;

            // set properties
            if (type == CellType.Metal)
            {
                DrawMetal(ref toCell);
            }
            else
            {
                toCell.CellType = type;
            }

            // save changes
            GridLayerUtility.SetCellAndUpdateVisuals(visualState, layer, toolModeState.LastKnownDragCoord, fromCell);
            GridLayerUtility.SetCellAndUpdateVisuals(visualState, layer, gridPos, toCell);
            
            Debug.Log($"<color=cyan>{type}</color> drawn");//TODO
        }
    }
}
