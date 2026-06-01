using BeauUtil;
using FieldDay;
using FieldDay.Components;
using SpaceFab.Onboarding;
using System;
using TMPro;
using UnityEngine;

namespace SpaceFab.Design
{
    /// <summary>
    /// Worldspace overlay parked on an Output cell's VisualGridCell. Unlike InputToggleVisual it
    /// has no toggle / click affordance — outputs are read-only — but it carries the same base
    /// frame, arrow, and subtype label, and recolors itself per-frame to the cell's simulated flow
    /// state (driven by OutputTagSystem). It also hosts the onboarding ElementTag the tutorial can
    /// address ("design:output-x", ...). Allocated from DesignPools.OutputTagOverlayPool by
    /// OutputTagUtility.SpawnOutputOverlays (called from GridStackLoadSystem once the visual grid is
    /// built), positioned at the matching Output cell, and stamped with its id + cell index. The id
    /// is cleared on deregister / free so the lookup never retains a stale entry for a pooled overlay.
    /// </summary>
    public class OutputTagVisual : BatchedComponent, IRegistrationCallbacks
    {
        // Onboarding tag stamped per spawn by OutputTagUtility (id derived from the cell's output
        // subtype label, e.g. "design:output-x"). Pre-wired on the prefab — runtime only writes
        // its Id via ElementTag.SetId.
        public ElementTag Tag;

        [Header("Common (sprites set from GridSpriteDB on spawn)")]
        // Square frame behind the whole overlay. Sprite assigned from GridSpriteDB.InputToggleBackground
        // (shared with the input overlay); the renderer's color is tinted per-frame by OutputTagSystem
        // to convey the cell's current flow state.
        public SpriteRenderer BackgroundRenderer;
        // Arrow icon. Sprite assigned from GridSpriteDB.InputToggleArrow; recolored per-frame with flow.
        public SpriteRenderer ArrowRenderer;

        [Header("Dynamic (set per-output on spawn / per-frame on flow change)")]
        // Short identifier label for the output subtype ("OUT", "X", "Y", "Z"). Set once on spawn
        // from the cell's SubtypeLabel; recolored per-frame with flow.
        public TMP_Text SubtypeText;

        // Flat (layer, col, row) index per SimulateRunScratchUtility.CellIndex. Identifies which
        // Output cell this overlay was spawned for and drives the per-frame flow lookup.
        [NonSerialized] public int CellIndex;

        // True once OutputTagUtility.SpawnOutputOverlays has assigned CellIndex (pool residue
        // otherwise reads as unstamped).
        [NonSerialized] public bool CellIndexStamped;

        public void OnRegister()
        {
        }

        public void OnDeregister()
        {
            // Clear the onboarding tag id so the lookup doesn't retain a stale entry if this
            // overlay is destroyed outside the normal FreeAllOutputOverlays path (e.g. scene
            // unload while overlays are still active).
            if (Tag != null) { Tag.SetId(default(StringHash32)); }
        }
    }
}
