using BeauUtil.Debugger;
using FieldDay;
using FieldDay.Components;
using FieldDay.Systems;
using SpaceFab.Design.Visuals;
using UnityEngine;

namespace SpaceFab.Design
{
    /// <summary>
    /// Per-frame visibility + flow visuals refresh for every OutputTagVisual in the scene. Mirrors
    /// InputToggleSystem, but outputs are driven by the simulated flow at their cell (read from
    /// SimulateRunScratch) rather than by a toggle state, and aren't gated on UseToggleInputMode —
    /// outputs are always shown once spawned:
    ///   - Hidden when the visual hasn't been coord-stamped yet (SpawnOutputOverlays runs once in
    ///     GridStackLoadSystem.SetupBaseLevel).
    ///   - Hidden when its cell is no longer an Output (player erased it).
    ///   - Otherwise visible; the background / arrow / subtype label recolor to the cell's flow state.
    /// Common sprites (background frame, arrow) and the subtype label are set once on spawn by
    /// OutputTagUtility; this system only changes what depends on flow state.
    /// </summary>
    public class OutputTagSystem : SystemComponent
    {
        public override unsafe void RegisterSystems(ref SystemRegistrationTable ecs)
        {
            ecs.Register(&ProcessWork,
                new SysUpdate(GameLoopPhase.LateUpdate, 5, UpdateMasks.DesignMask),
                new SysPermissions()
                    .ReadShared<GridStackState>()
                    .ReadShared<SimulateRunScratch>()
                    .Read<OutputTagVisual>()
            );
        }

        static private void ProcessWork(float deltaTime)
        {
            GridStackState gridStackState = Find.State<GridStackState>();
            SimulateRunScratch scratch = Find.State<SimulateRunScratch>();
            GridSpriteDB spriteDB = Find.GlobalAsset<GridSpriteDB>();

            var visuals = Find.Components<OutputTagVisual>();
            for (int i = 0; i < visuals.Count; i++)
            {
                OutputTagVisual visual = visuals[i];
                if (visual == null) { continue; }

                // Pre-stamp visuals stay hidden until SpawnOutputOverlays assigns the cell index.
                if (!visual.CellIndexStamped)
                {
                    SetActiveIfChanged(visual, false);
                    continue;
                }

                // Hide if the stamped cell is no longer an Output (player erased it).
                if (!IsOutputCell(gridStackState, visual.CellIndex))
                {
                    SetActiveIfChanged(visual, false);
                    continue;
                }

                SetActiveIfChanged(visual, true);
                FlowState flow = SimulateRunScratchUtility.GetCellFlow(scratch, visual.CellIndex);
                ApplyFlowVisuals(visual, flow, spriteDB);
            }
        }

        // Resolves whether the cell at the given flat index is currently an Output. Decodes the
        // flat (layer, col, row) index against the grid dims so we can read the cell back.
        static private bool IsOutputCell(GridStackState gridStackState, int cellIndex)
        {
            Assert.False(gridStackState.GridStack == null, "Null GridStack");

            GridStack stack = gridStackState.GridStack;
            int numCols = stack.LayerDims.X;
            int cellsPerLayer = numCols * stack.LayerDims.Y;
            if (cellsPerLayer <= 0) { return false; }

            int layer = cellIndex / cellsPerLayer;
            int withinLayer = cellIndex % cellsPerLayer;
            int row = withinLayer / numCols;
            int col = withinLayer % numCols;

            if (layer < 0 || layer >= stack.GridLayers.Length) { return false; }

            GridCell cell = GridStackUtility.GetCellDirect(gridStackState, layer, col, row);
            return cell != null && cell.CellType == CellType.Output;
        }

        // SetActive is a virtual call; avoid it when the state already matches to keep this loop
        // cheap on the typical "no change" frame.
        static private void SetActiveIfChanged(OutputTagVisual visual, bool active)
        {
            if (visual.gameObject.activeSelf != active)
            {
                visual.gameObject.SetActive(active);
            }
        }

        // Recolors the overlay to the cell's flow state: the background takes the fill color, the
        // arrow + subtype label take the text color. All four flow states (Hi / Lo / Empty /
        // Unstable) resolve via the GridSpriteDB lookups. Skip writes that wouldn't change anything
        // so a still frame stays a no-op.
        static private void ApplyFlowVisuals(OutputTagVisual visual, FlowState flow, GridSpriteDB spriteDB)
        {
            if (visual.BackgroundRenderer != null)
            {
                Color fill = GridSpriteDBUtility.LookupOutputFlowColor(spriteDB, flow);
                if (visual.BackgroundRenderer.color != fill)
                {
                    visual.BackgroundRenderer.color = fill;
                }
            }

            Color textColor = GridSpriteDBUtility.LookupOutputFlowTextColor(spriteDB, flow);

            if (visual.ArrowRenderer != null && visual.ArrowRenderer.color != textColor)
            {
                visual.ArrowRenderer.color = textColor;
            }

            if (visual.SubtypeText != null && visual.SubtypeText.color != textColor)
            {
                visual.SubtypeText.color = textColor;
            }
        }
    }
}
