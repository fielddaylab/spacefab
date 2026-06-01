using System.Collections.Generic;
using UnityEngine;

namespace SpaceFab.Design
{
    /// <summary>
    /// Physical-connectivity queries over GridStackState: does a wire/cell path exist between two
    /// kinds of node? Operates purely on Connected grid edges between participating cells — it does
    /// NOT model transistor gating or signal values, so it answers "are these wired together" rather
    /// than "does signal propagate." Self-contained from the simulate eval-graph, so it works in any
    /// mode (Tool or Simulate) without the graph being built. Paired with GridStackState.
    /// </summary>
    public static class GridConnectivityUtility
    {
        // Reused BFS scratch. These queries are infrequent (Leaf onboarding hooks), but reusing the
        // buffers across calls keeps them allocation-free after warmup. Cleared at the start of each
        // search.
        private static readonly Queue<GridCoord> s_Frontier = new Queue<GridCoord>(64);
        private static readonly HashSet<int> s_Visited = new HashSet<int>();

        // True if any Input cell labelled `inputLabel` has a physical path to any Output cell.
        public static bool IsInputConnectedToAnyOutput(GridStackState gridStackState, InputOutputNodeTypeFlags inputLabel)
        {
            return SearchFromInputLabel(gridStackState, inputLabel, requireOutputLabel: false, default);
        }

        // True if any Input cell labelled `inputLabel` has a physical path to an Output cell
        // labelled `outputLabel`.
        public static bool IsInputConnectedToOutput(GridStackState gridStackState, InputOutputNodeTypeFlags inputLabel, InputOutputNodeTypeFlags outputLabel)
        {
            return SearchFromInputLabel(gridStackState, inputLabel, requireOutputLabel: true, outputLabel);
        }

        // BFS from every Input cell carrying inputLabel. Seeds the frontier with all matching inputs
        // (any-cell-with-label semantics), then walks Connected edges through participating cells.
        // Succeeds the moment it reaches a qualifying Output cell.
        private static bool SearchFromInputLabel(GridStackState gridStackState, InputOutputNodeTypeFlags inputLabel, bool requireOutputLabel, InputOutputNodeTypeFlags outputLabel)
        {
            if (gridStackState == null || gridStackState.GridStack == null) { return false; }

            GridStack gridStack = gridStackState.GridStack;
            int numLayers = gridStack.GridLayers.Length;
            int numCols = gridStack.LayerDims.X;
            int numRows = gridStack.LayerDims.Y;

            s_Frontier.Clear();
            s_Visited.Clear();

            // 1. Seed the frontier with every Input cell that carries the requested label.
            for (int layer = 0; layer < numLayers; layer++)
            {
                for (int row = 0; row < numRows; row++)
                {
                    for (int col = 0; col < numCols; col++)
                    {
                        GridCell cell = GridStackUtility.GetCellDirect(gridStackState, layer, col, row);
                        if (cell.CellType == CellType.Input && (cell.SubtypeLabel & inputLabel) != 0)
                        {
                            GridCoord coord = new GridCoord(layer, col, row);
                            if (s_Visited.Add(FlatIndex(layer, col, row, numCols, numRows)))
                            {
                                s_Frontier.Enqueue(coord);
                            }
                        }
                    }
                }
            }

            // 2. BFS over Connected edges. An Output cell reached along the way ends the search;
            //    a seed Input is never itself an Output, so the start cells can't trivially match.
            while (s_Frontier.Count > 0)
            {
                GridCoord coord = s_Frontier.Dequeue();
                GridCell cell = GridStackUtility.GetCellDirect(gridStackState, coord);

                if (cell.CellType == CellType.Output && OutputMatches(cell, requireOutputLabel, outputLabel))
                {
                    return true;
                }

                // Follow every Connected edge to its in-bounds, participating neighbor.
                for (int dir = 0; dir < 6; dir++)
                {
                    if (cell.Edges[dir].EdgeState != EdgeState.Connected) { continue; }

                    GridStackUtility.GetOffsetOfDir((EdgeDir)dir, out Vector2Int gridOffset, out int layerOffset);
                    int adjLayer = coord.Layer + layerOffset;
                    int adjCol = coord.Col + gridOffset.x;
                    int adjRow = coord.Row + gridOffset.y;

                    if (adjLayer < 0 || adjLayer >= numLayers) { continue; }
                    if (adjCol < 0 || adjCol >= numCols) { continue; }
                    if (adjRow < 0 || adjRow >= numRows) { continue; }

                    // Same "does this cell participate" rule the eval-graph build uses: a cell with
                    // no CellType and no GateAbove transfer is empty and carries no connection.
                    GridCell adjCell = GridStackUtility.GetCellDirect(gridStackState, adjLayer, adjCol, adjRow);
                    bool adjParticipates = adjCell.CellType != CellType.NONE
                        || adjCell.TransferType == TransferType.GateAbove;
                    if (!adjParticipates) { continue; }

                    if (s_Visited.Add(FlatIndex(adjLayer, adjCol, adjRow, numCols, numRows)))
                    {
                        s_Frontier.Enqueue(new GridCoord(adjLayer, adjCol, adjRow));
                    }
                }
            }

            return false;
        }

        // An Output cell qualifies if we're matching any output, or its label includes the
        // requested output label.
        private static bool OutputMatches(GridCell cell, bool requireOutputLabel, InputOutputNodeTypeFlags outputLabel)
        {
            if (!requireOutputLabel) { return true; }
            return (cell.SubtypeLabel & outputLabel) != 0;
        }

        // Flatten (layer, col, row) to a unique int for the visited set.
        private static int FlatIndex(int layer, int col, int row, int numCols, int numRows)
        {
            return layer * (numCols * numRows) + row * numCols + col;
        }
    }
}
