using BeauUtil;
using FieldDay;
using FieldDay.SharedState;
using FieldDay.Systems;
using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

namespace SpaceFab.Overarching
{
    public class MinigameZonesState : SharedStateComponent
    {
        [NonSerialized] public MinigameZone HoverZone;
        [NonSerialized] public MinigameZone QueuedZone;
    }

    public static partial class MinigameZonesUtility
    {
        //// Hover is now just index bookkeeping; the sprite/label swap happens declaratively in
        //// RefreshZoneVisuals based on the current hover index.
        //public static void CancelHover(MinigameZonesState state, int indexToCancel)
        //{
        //    if (state.CurrHoverIndex == indexToCancel)
        //    {
        //        state.CurrHoverIndex = -1;
        //    }
        //}

        //public static void BeginHover(MinigameZonesState state, int indexToHover)
        //{
        //    state.CurrHoverIndex = indexToHover;
        //}

        //// A click records which zone is being entered (read by OverarchingToMinigameSequenceSystem
        //// to resolve the scene) and starts the enter-minigame transition. There's no separate
        //// selection highlight — the focus sprite is driven entirely by hover.
        //public static void ClickZone(MinigameZonesState state, int indexToClick)
        //{
        //    state.CurrSelectedIndex = indexToClick;
        //    ConfirmEnterMinigame(state);
        //}

        //#region Zone Visuals

        //// Recomputes every zone's overlay + label from current state, as a pure function of
        //// (locked, hovered, needs-attention). Locked zones hide everything; unlocked zones show
        //// their non-focus overlay, swapping to the focus sprite while hovered. The station label
        //// (with its background + dot) shows while hovered or while the zone wants attention. The
        //// dot always carries the zone's color; the background takes the zone's color while focused
        //// and the shared default otherwise. Overlay sprites are self-colored (renderer untinted).
        //public static void RefreshZoneVisuals(MinigameZonesState state, OverarchingAlertState alertState)
        //{
        //    //for (int i = 0; i < state.Zones.Length; i++)
        //    //{
        //    //    MinigameZone zone = state.Zones[i];

        //    //    bool locked = OverarchingAlertUtility.HasAlert(alertState, zone.Minigame, AlertType.Locked);

        //    //    // Locked zones offer no interaction affordance: disable the hover CursorHint so the
        //    //    // pointer doesn't change over them (their overlay and click are already suppressed).
        //    //    if (zone.Cursor != null)
        //    //    {
        //    //        zone.Cursor.enabled = !locked;
        //    //    }

        //    //    if (locked)
        //    //    {
        //    //        SetZoneVisualsEmpty(zone);
        //    //        continue;
        //    //    }

        //    //    bool focused = state.CurrHoverIndex == zone.ZoneIndex;

        //    //    zone.Overlay.HighlightFill.enabled = true;
        //    //    zone.Overlay.HighlightOutline.enabled = true;

        //    //    zone.Overlay.HighlightOutline.color = focused ? Color.white : Color.black;

        //    //    // Label (with its background + dot) stays visible for any unlocked, not-yet-completed
        //    //    // zone; a completed zone shows it only while hovered. (Locked zones already returned.)
        //    //    bool completed = OverarchingAlertUtility.HasAlert(alertState, zone.Minigame, AlertType.Complete);
        //    //    bool labelVisible = focused || !completed;

        //    //    zone.Overlay.NameFill.color = focused ? zone.Overlay.ThemeColor : Color.black;
        //    //    zone.Overlay.NameBadge.SetActive(labelVisible);
        //    //    zone.Overlay.CompletedBadge.SetActive(completed);
        //    //}
        //}

        //// Hides every per-zone visual: the overlay highlight and the whole label group (text,
        //// background, dot). Used for locked zones and zones with no authored overlay sprite.
        //public static void SetZoneVisualsEmpty(MinigameZone zone)
        //{
        //    zone.Overlay.HighlightFill.enabled = false;
        //    zone.Overlay.HighlightOutline.enabled = false;
        //    zone.Overlay.CompletedBadge.SetActive(false);
        //    zone.Overlay.NameBadge.SetActive(false);
        //}

        //#endregion // Zone Visuals

        //public static void ConfirmEnterMinigame(MinigameZonesState state)
        //{
        //    Debug.Log("Start minigame: " + state.CurrSelectedIndex);
        //    SpacefabGame.Events.Dispatch(GameEvents.StartMinigame, state.CurrSelectedIndex);
        //    OverarchingTransitions.ToMinigame();
        //}
    }
}