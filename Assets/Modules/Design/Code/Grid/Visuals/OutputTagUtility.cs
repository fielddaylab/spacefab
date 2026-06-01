using BeauPools;
using BeauUtil;
using FieldDay;
using SpaceFab.Design.Visuals;

namespace SpaceFab.Design
{
    /// <summary>
    /// Spawns and frees the per-Output onboarding-tag overlays (OutputTagVisual). Mirrors the input
    /// path in InputToggleUtility, minus the interaction: outputs are passive, so these overlays
    /// exist only to host an ElementTag ("design:output-x", ...) the tutorial can address. Paired
    /// with DesignPools.OutputTagOverlayPool.
    /// </summary>
    public static class OutputTagUtility
    {
        // Returns every active output-tag overlay to the pool. Called as the first step of
        // SpawnOutputOverlays (clean slate before re-allocating for the freshly-loaded grid).
        public static void FreeAllOutputOverlays(DesignPools pools)
        {
            if (pools == null || pools.ActiveOutputTagOverlays == null) { return; }
            int n = pools.ActiveOutputTagOverlays.Count;
            for (int i = n - 1; i >= 0; i--)
            {
                OutputTagVisual overlay = pools.ActiveOutputTagOverlays[i];
                if (overlay != null)
                {
                    // Clear the onboarding tag id before pooling so the lookup doesn't carry stale
                    // entries pointing at parked overlays across level reloads.
                    if (overlay.Tag != null) { overlay.Tag.SetId(default(StringHash32)); }
                    Pool.TryFree(overlay);
                }
            }
            pools.ActiveOutputTagOverlays.Clear();
        }

        // Walks the loaded grid, allocs one tag overlay from the pool per Output cell, positions it
        // at the matching VisualGridCell's worldspace location, stamps its CellIndex, and registers
        // it in the pool's Active list. Frees any previously-active overlays first so a level
        // transition leaves a clean set. Called from GridStackLoadSystem alongside SpawnInputOverlays.
        public static void SpawnOutputOverlays(GridStackState gridStackState, VisualGridStackState visualState, DesignPools pools)
        {
            if (pools == null) { return; }
            FreeAllOutputOverlays(pools);

            if (gridStackState == null || gridStackState.GridStack == null || gridStackState.GridStack.GridLayers == null) { return; }
            if (visualState == null || visualState.VisualGridStack == null || visualState.VisualGridStack.GridLayers == null) { return; }

            GridStack stack = gridStackState.GridStack;
            int layerLimit = stack.GridLayers.Length;
            if (visualState.VisualGridStack.GridLayers.Length < layerLimit) { layerLimit = visualState.VisualGridStack.GridLayers.Length; }

            int numCols = stack.LayerDims.X;
            int cellsPerLayer = numCols * stack.LayerDims.Y;

            GridSpriteDB spriteDB = Find.GlobalAsset<GridSpriteDB>();

            for (int layer = 0; layer < layerLimit; layer++)
            {
                VisualGridLayer visualLayer = visualState.VisualGridStack.GridLayers[layer];
                if (visualLayer == null) { continue; }

                for (int col = 0; col < stack.LayerDims.X; col++)
                {
                    for (int row = 0; row < stack.LayerDims.Y; row++)
                    {
                        GridCell cell = GridStackUtility.GetCellDirect(gridStackState, layer, col, row);
                        if (cell == null || cell.CellType != CellType.Output) { continue; }

                        VisualGridCell visualCell = visualLayer.GetCell(col, row);
                        if (visualCell == null) { continue; }

                        OutputTagVisual overlay = pools.OutputTagOverlayPool.Alloc();
                        if (overlay == null) { continue; }

                        overlay.transform.position = visualCell.transform.position;

                        ApplyOverlayCommonVisuals(overlay, spriteDB);
                        ApplyOverlaySubtypeLabel(overlay, cell.SubtypeLabel);
                        ApplyOverlayTag(overlay, cell.SubtypeLabel);

                        overlay.CellIndex = SimulateRunScratchUtility.CellIndex(layer, col, row, numCols, cellsPerLayer);
                        overlay.CellIndexStamped = true;

                        pools.ActiveOutputTagOverlays.Add(overlay);
                    }
                }
            }
        }

        // Assigns the shared (flow-independent) sprites onto a freshly-spawned overlay. Reuses the
        // input-toggle frame + arrow sprites from GridSpriteDB. Per-frame flow tinting lives in
        // OutputTagSystem.
        private static void ApplyOverlayCommonVisuals(OutputTagVisual overlay, GridSpriteDB spriteDB)
        {
            if (spriteDB == null) { return; }
            if (overlay.BackgroundRenderer != null && spriteDB.OutputToggleBackground != null)
            {
                overlay.BackgroundRenderer.sprite = spriteDB.OutputToggleBackground;
            }
            if (overlay.ArrowRenderer != null && spriteDB.InputToggleArrow != null)
            {
                overlay.ArrowRenderer.sprite = spriteDB.InputToggleArrow;
            }
        }

        // Writes the per-output subtype label ("OUT", "X", "Y", "Z") once on spawn — the output
        // cell's SubtypeLabel doesn't change after the grid is loaded.
        private static void ApplyOverlaySubtypeLabel(OutputTagVisual overlay, InputOutputNodeTypeFlags subtype)
        {
            if (overlay.SubtypeText != null)
            {
                overlay.SubtypeText.SetText(GetOutputSubtypeShortLabel(subtype));
            }
        }

        // Stamps the onboarding ElementTag id ("design:output-x", "design:output-out", ...) from the
        // output cell's subtype label. Passes the full source string so the SerializedHash32 keeps
        // it readable in the inspector rather than storing only the hash. Mirrors the
        // "module:kebab-case-name" id format used elsewhere in the project.
        private static void ApplyOverlayTag(OutputTagVisual overlay, InputOutputNodeTypeFlags subtype)
        {
            if (overlay.Tag == null) { return; }

            string shortLabel = GetOutputSubtypeShortLabel(subtype);
            string tagId = string.IsNullOrEmpty(shortLabel)
                ? null
                : "design:output-" + shortLabel.ToLowerInvariant();
            overlay.Tag.SetId(tagId);
        }

        // Short identifier for an output subtype: "out", "x", "y", "z". Mirrors
        // InputToggleUtility.GetInputSubtypeShortLabel for the output labels.
        public static string GetOutputSubtypeShortLabel(InputOutputNodeTypeFlags id)
        {
            if ((id & InputOutputNodeTypeFlags.OUTX) != 0) { return "X"; }
            if ((id & InputOutputNodeTypeFlags.OUTY) != 0) { return "Y"; }
            if ((id & InputOutputNodeTypeFlags.OUTZ) != 0) { return "Z"; }
            if ((id & InputOutputNodeTypeFlags.OUT) != 0) { return "OUT"; }
            return string.Empty;
        }
    }
}
