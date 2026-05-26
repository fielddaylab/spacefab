using FieldDay;
using FieldDay.Components;
using FieldDay.UI;
using System;
using TMPro;
using UnityEngine;

namespace SpaceFab.Design
{
    /// <summary>
    /// Worldspace clickable overlay on an Input cell's VisualGridCell. The player clicks this
    /// to flip the cell's Lo/Hi toggle in toggle-input mode. Mirrors the ResearchSpriteButton
    /// shape (SpriteRenderer + Collider2D + CursorHint) so clicks route through Unity's
    /// EventSystem via the Design camera's Physics2DRaycaster.
    ///
    /// Allocated from DesignPools.InputToggleOverlayPool by InputToggleUtility.SpawnInputOverlays
    /// (called from GridStackLoadSystem once the visual grid is built), positioned at the matching
    /// Input cell, and stamped with its CellIndex. The click handler reads CellIndex to dispatch
    /// into the utility.
    /// </summary>
    public class InputToggleVisual : BatchedComponent, IRegistrationCallbacks
    {
        [Header("Interaction")]
        public Collider2D Collider;
        public CursorHint Cursor;

        [Header("Common (sprites set from GridSpriteDB on spawn)")]
        // Square frame behind the whole overlay. Sprite assigned from GridSpriteDB.InputToggleBackground;
        // the renderer's color is tinted per-frame by InputToggleSystem to convey the current Lo/Hi state.
        public SpriteRenderer BackgroundRenderer;
        // Arrow icon. Sprite assigned from GridSpriteDB.InputToggleArrow; no per-state change.
        public SpriteRenderer ArrowRenderer;
        // Knob sprite inside the toggle pill. Slides between the two anchored positions below
        // based on the current Lo/Hi state.
        public SpriteRenderer ToggleHandleRenderer;
        // Local position of ToggleHandleRenderer's transform when the input is in the Lo state
        // (knob sits at the left side of the pill).
        public Vector3 ToggleHandleLoLocalPosition;
        // Local position when in the Hi state (knob sits at the right side of the pill).
        public Vector3 ToggleHandleHiLocalPosition;

        [Header("Dynamic (set per-input on spawn / per-frame on state change)")]
        // Short identifier label for the input subtype ("A", "B", "C", "IN"). Set once on spawn
        // from the cell's SubtypeLabel.
        public TMP_Text SubtypeText;
        // "LO" / "HI" indicator inside the toggle pill. Updated per-frame by InputToggleSystem
        // based on the current state. Optional — leave null if the prefab handles state via colors only.
        public TMP_Text StateText;

        // Flat (layer, col, row) index per SimulateRunScratchUtility.CellIndex. Same encoding the
        // rest of the grid uses, so a single int identifies the cell unambiguously.
        [NonSerialized] public int CellIndex;

        // True once InputToggleUtility.SpawnInputOverlays has assigned CellIndex. The refresh
        // system uses this to skip overlays that aren't actively assigned to a cell (pool residue).
        [NonSerialized] public bool CellIndexStamped;

        public void OnRegister()
        {
            if (Cursor != null)
            {
                Cursor.onClick.Register(HandleClick);
            }
        }

        public void OnDeregister()
        {
            if (Cursor != null)
            {
                Cursor.onClick.Deregister(HandleClick);
            }
        }

        private void HandleClick()
        {
            if (!CellIndexStamped) { return; }
            InputToggleUtility.HandleToggleClick(CellIndex);
        }
    }
}
