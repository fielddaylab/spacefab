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
    }
}
