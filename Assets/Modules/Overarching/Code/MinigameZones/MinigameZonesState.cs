using BeauUtil;
using FieldDay;
using FieldDay.SharedState;
using FieldDay.Systems;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

namespace SpaceFab.Overarching
{
    public class MinigameZonesState : SharedStateComponent, IRegistrationCallbacks
    {
        public MinigameZone[] Zones;
        public int CurrSelectedIndex;
        public int CurrHoverIndex;

        public void OnDeregister()
        {
        }

        public void OnRegister()
        {
            MinigameZonesUtility.AssignIndices(this);
            CurrSelectedIndex = -1;
            CurrHoverIndex = -1;
        }
    }

    public static partial class MinigameZonesUtility
    {
        public static void AssignIndices(MinigameZonesState state)
        {
            for (int i = 0; i < state.Zones.Length; i++)
            {
                state.Zones[i].ZoneIndex = i;
            }
        }

        // Hover is now just index bookkeeping; the sprite/label swap happens declaratively in
        // RefreshZoneVisuals based on the current hover index.
        public static void CancelHover(MinigameZonesState state, int indexToCancel)
        {
            if (state.CurrHoverIndex == indexToCancel)
            {
                state.CurrHoverIndex = -1;
            }
        }

        public static void BeginHover(MinigameZonesState state, int indexToHover)
        {
            state.CurrHoverIndex = indexToHover;
        }

        // A click records which zone is being entered (read by OverarchingToMinigameSequenceSystem
        // to resolve the scene) and starts the enter-minigame transition. There's no separate
        // selection highlight — the focus sprite is driven entirely by hover.
        public static void ClickZone(MinigameZonesState state, int indexToClick)
        {
            state.CurrSelectedIndex = indexToClick;
            ConfirmEnterMinigame(state);
        }

        #region Zone Visuals

        // Recomputes every zone's overlay + label from current state, as a pure function of
        // (locked, hovered, needs-attention). Locked zones hide everything; unlocked zones show
        // their non-focus overlay, swapping to the focus sprite while hovered. The station label
        // (with its background + dot) shows while hovered or while the zone wants attention. The
        // dot always carries the zone's color; the background takes the zone's color while focused
        // and the shared default otherwise. Overlay sprites are self-colored (renderer untinted).
        public static void RefreshZoneVisuals(MinigameZonesState state, OverarchingAlertState alertState, MinigameZoneOverlayDB overlayDB)
        {
            for (int i = 0; i < state.Zones.Length; i++)
            {
                MinigameZone zone = state.Zones[i];

                bool locked = OverarchingAlertUtility.HasAlert(alertState, zone.Minigame, AlertType.Locked);

                // Locked zones offer no interaction affordance: disable the hover CursorHint so the
                // pointer doesn't change over them (their overlay and click are already suppressed).
                if (zone.Cursor != null)
                {
                    zone.Cursor.enabled = !locked;
                }

                if (locked)
                {
                    SetZoneVisualsEmpty(zone);
                    continue;
                }

                bool focused = state.CurrHoverIndex == zone.ZoneIndex;
                Sprite sprite = MinigameZoneOverlayDBUtility.LookupOverlaySprite(overlayDB, zone.Minigame, focused);
                if (sprite == null)
                {
                    SetZoneVisualsEmpty(zone);
                    continue;
                }

                zone.HighlightRenderer.sprite = sprite;
                zone.HighlightRenderer.color = Color.white;
                zone.HighlightRenderer.enabled = true;

                // Label (with its background + dot) stays visible for any unlocked, not-yet-completed
                // zone; a completed zone shows it only while hovered. (Locked zones already returned.)
                bool completed = OverarchingAlertUtility.HasAlert(alertState, zone.Minigame, AlertType.Complete);
                bool labelVisible = focused || !completed;
                Color zoneColor = MinigameZoneOverlayDBUtility.LookupZoneColor(overlayDB, zone.Minigame);

                // Color the label's parts (valid even while LabelGroup is inactive), then toggle the
                // whole group's visibility.
                if (zone.StationLabelBackground != null)
                {
                    zone.StationLabelBackground.color = focused ? zoneColor : overlayDB.LabelBackgroundColor;
                }
                if (zone.StationLabelDot != null)
                {
                    zone.StationLabelDot.color = zoneColor;
                }
                if (zone.LabelGroup != null) { zone.LabelGroup.SetActive(labelVisible); }
            }
        }

        // Hides every per-zone visual: the overlay highlight and the whole label group (text,
        // background, dot). Used for locked zones and zones with no authored overlay sprite.
        public static void SetZoneVisualsEmpty(MinigameZone zone)
        {
            if (zone.HighlightRenderer != null) { zone.HighlightRenderer.enabled = false; }
            if (zone.LabelGroup != null) { zone.LabelGroup.SetActive(false); }
        }

        #endregion // Zone Visuals

        public static void ConfirmEnterMinigame(MinigameZonesState state)
        {
            GameLoop.ResumeUpdates(UpdateMasks.ShutdownMask);
            Find.State<OverarchingToMinigameSequenceState>().Phase = OverarchingToMinigamePhase.Starting;
        }
    }
}