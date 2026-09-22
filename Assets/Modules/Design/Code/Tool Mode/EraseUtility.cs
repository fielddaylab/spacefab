using FieldDay;
using SpaceFab.Design.Visuals;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace SpaceFab.Design
{
    public static class EraseUtility
    {
        public static void EraseCellBothLayers(ToolModeState toolModeState, GridStackState gridState, Vector2Int gridPos)
        {
            int currLayer = (int)toolModeState.ActiveLayer;
            var layer = gridState.GridStack.GridLayers[currLayer];
            var cell = GridLayerUtility.GetCell(layer, gridPos);

            EraseCellOneLayer(toolModeState, gridState, cell, gridPos, currLayer);

            currLayer = (int)GridStackUtility.GetOppositeLayer(toolModeState.ActiveLayer);
            var twinLayer = gridState.GridStack.GridLayers[currLayer];
            var twinCell = GridLayerUtility.GetCell(twinLayer, gridPos);
            EraseCellOneLayer(toolModeState, gridState, twinCell, gridPos, currLayer);
        }

        public static void EraseCellOneLayer(ToolModeState toolModeState, GridStackState gridState, GridCell cell, Vector2Int gridPos, int currLayer)
        {
            // erase cell
            cell.Erase(out List<EdgeDir> danglingEdges);

            // erase dangling edges
            foreach (var dangling in danglingEdges)
            {
                // get adj cell
                var adjCell = GridStackUtility.GetAdjCell(gridState, gridPos, dangling, currLayer);

                // erase opposite edge
                adjCell.EraseEdge(GridStackUtility.GetOppositeDir(dangling));
            }

            // update visuals
            var visuals = Find.State<VisualGridStackState>();
            if (visuals != null)
            {
                visuals.VisualsNeedRefreshing = true;
            }
        }

        public static void EraseTransferData(ToolModeState toolModeState, GridStackState gridState, GridCell cell, Vector2Int gridPos, int currLayer)
        {
            StackLayer linkedLayerType = toolModeState.ActiveLayer == StackLayer.Metal ? StackLayer.Transistor : StackLayer.Metal;
            var linkedLayer = gridState.GridStack.GridLayers[(int)linkedLayerType];
            var linkedCell = GridLayerUtility.GetCell(linkedLayer, gridPos);

            if (cell.TransferEraseable)
            {
                if (cell.TransferType != TransferType.Implicit)
                {
                    cell.TransferType = TransferType.NONE;
                }

                int cellEdgeIndex = toolModeState.ActiveLayer == StackLayer.Metal ? (int)EdgeDir.DESCEND : (int)EdgeDir.ASCEND;
                cell.Edges[cellEdgeIndex].EdgeState = EdgeState.Disconnected;
            }

            if (linkedCell.TransferEraseable)
            {
                if (linkedCell.TransferType != TransferType.Implicit)
                {
                    linkedCell.TransferType = TransferType.NONE;
                }

                int linkedEdgeIndex = toolModeState.ActiveLayer == StackLayer.Metal ? (int)EdgeDir.ASCEND : (int)EdgeDir.DESCEND;
                linkedCell.Edges[linkedEdgeIndex].EdgeState = EdgeState.Disconnected;
            }
        }
    }
}
