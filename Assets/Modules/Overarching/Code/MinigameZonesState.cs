using BeauUtil;
using FieldDay;
using FieldDay.SharedState;
using FieldDay.Systems;
using System.Collections;
using System.Collections.Generic;
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

        public static void CancelHover(MinigameZonesState state, int indexToCancel)
        {
            if (state.CurrHoverIndex == indexToCancel)
            {
                state.CurrHoverIndex = -1;

                if (state.CurrSelectedIndex != indexToCancel)
                {
                    SetHighlightEmpty(state.Zones[indexToCancel].HighlightRenderer);
                }
            }
        }

        public static void BeginHover(MinigameZonesState state, PaletteState palette, int indexToHover)
        {
            if (state.CurrHoverIndex != indexToHover)
            {
                // Cancel previous hover. if any
                if (state.CurrHoverIndex != -1)
                {
                    CancelHover(state, state.CurrHoverIndex);
                }

                state.CurrHoverIndex = indexToHover;
            }

            if (state.CurrSelectedIndex != indexToHover)
            {
                SetHighlightColor(state.Zones[indexToHover].HighlightRenderer, palette.CurrPalette.SoftHighlight);
            }
        }

        public static void CancelSelected(MinigameZonesState state, PaletteState palette)
        {
            if (state.CurrHoverIndex == state.CurrSelectedIndex)
            {
                // replace selected with hover
                SetHighlightColor(state.Zones[state.CurrSelectedIndex].HighlightRenderer, palette.CurrPalette.SoftHighlight);
            }
            else
            {
                // set previous selected to empty
                SetHighlightEmpty(state.Zones[state.CurrSelectedIndex].HighlightRenderer);
            }

            state.CurrSelectedIndex = -1;

        }

        public static void ClickZone(MinigameZonesState state, PaletteState palette, int indexToClick)
        {
            if (state.CurrSelectedIndex == indexToClick)
            {
                // second click: treat as double click (confirm)
                ConfirmEnterMinigame(state);
            }
            else
            {
                if (state.CurrSelectedIndex != -1)
                {
                    CancelSelected(state, palette);
                }
                state.CurrSelectedIndex = indexToClick;
                SetHighlightColor(state.Zones[indexToClick].HighlightRenderer, palette.CurrPalette.HardHighlight);
            }
        }

        #region Zone Visuals

        public static void SetHighlightEmpty(SpriteRenderer highlight)
        {
            highlight.enabled = false;
        }

        public static void SetHighlightColor(SpriteRenderer highlight, Color color, bool enable = true)
        {
            highlight.enabled = enable;
            highlight.color = color;
        }

        #endregion // Zone Visuals

        public static void ConfirmEnterMinigame(MinigameZonesState state)
        {
            GameLoop.ResumeUpdates(UpdateMasks.ShutdownMask);
            Find.State<OverarchingShutdownSequenceState>().Phase = OverarchingShutdownPhase.ShuttingDown;
        }
    }
}