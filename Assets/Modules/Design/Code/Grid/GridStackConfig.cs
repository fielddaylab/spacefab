using SpaceFab.Design;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace SpaceFab.Design
{
    [Serializable]
    public struct GridCellConfig
    {
        public StackLayer LayerIndex;
        public int RowIndex;
        public int ColumnIndex;

        public CellType CellType;
        public InputOutputNodeTypeFlags SubtypeLabel;
        public EdgeStateData[] Edges;
        public TransferType TransferType; // informs how data is transferred between layers when either ASCEND or DESCEND edges are connected

        // For CellType.Input cells: the default Lo/Hi toggle state when the level loads (toggle-input mode only).
        // Ignored for non-input cells.
        public FlowState DefaultInputState;
    }

    [CreateAssetMenu(menuName = "SpaceFab/Design/Grid Stack Config")]
    public class GridStackConfig : ScriptableObject
    {
        public Dimensions LayerDims;
        public GridCellConfig[] Cells;
    }
}