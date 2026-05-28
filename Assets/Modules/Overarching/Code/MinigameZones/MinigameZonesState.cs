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

        public static void CancelHover(MinigameZonesState state, int indexToCancel)
        {
            if (state.CurrHoverIndex == indexToCancel)
            {
                state.CurrHoverIndex = -1;

                if (state.CurrSelectedIndex != indexToCancel)
                {
                    SetHighlightEmpty(state.Zones[indexToCancel].HighlightRenderer, state.Zones[indexToCancel].StationLabel);
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
                SetHighlightSprite(state.Zones[indexToHover].HighlightRenderer, state.Zones[indexToHover].NormalHighlight, state.Zones[indexToHover].StationLabel);
                SetHighlightColor(state.Zones[indexToHover].HighlightRenderer, palette.CurrPalette.SoftHighlight);
            }
        }

        public static void CancelSelected(MinigameZonesState state, PaletteState palette)
        {
            if (state.CurrHoverIndex == state.CurrSelectedIndex)
            {
                // replace selected with hover
                SetHighlightSprite(state.Zones[state.CurrSelectedIndex].HighlightRenderer, state.Zones[state.CurrSelectedIndex].NormalHighlight, state.Zones[state.CurrSelectedIndex].StationLabel);
                SetHighlightColor(state.Zones[state.CurrSelectedIndex].HighlightRenderer, palette.CurrPalette.SoftHighlight);
            }
            else
            {
                // set previous selected to empty
                SetHighlightEmpty(state.Zones[state.CurrSelectedIndex].HighlightRenderer, state.Zones[state.CurrSelectedIndex].StationLabel);
                state.Zones[state.CurrSelectedIndex].StationLabel.enabled = false;
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

                // Click immediately enters minigame now
                state.CurrSelectedIndex = indexToClick;
                SetHighlightSprite(state.Zones[indexToClick].HighlightRenderer, state.Zones[indexToClick].EmphasisHighlight, state.Zones[indexToClick].StationLabel);
                SetHighlightColor(state.Zones[indexToClick].HighlightRenderer, palette.CurrPalette.HardHighlight);
                ConfirmEnterMinigame(state);
            }
        }

        #region Zone Visuals

        public static void SetHighlightEmpty(SpriteRenderer highlight, TMP_Text label)
        {
            highlight.enabled = false;
            label.enabled = false;
        }

        public static void SetHighlightSprite(SpriteRenderer highlight, Sprite sprite, TMP_Text label)
        {
            highlight.sprite = sprite;
            highlight.enabled = true;
            label.enabled = true;
        }

        public static void SetHighlightColor(SpriteRenderer highlight, Color color, bool enable = true)
        {
            highlight.color = color;
        }

        #endregion // Zone Visuals

        public static void ConfirmEnterMinigame(MinigameZonesState state)
        {
            GameLoop.ResumeUpdates(UpdateMasks.ShutdownMask);
            Debug.Log("Start minigame: " + state.CurrSelectedIndex);
            SpacefabGame.Events.Dispatch(GameEvents.StartMinigame, state.CurrSelectedIndex);
            Find.State<OverarchingToMinigameSequenceState>().Phase = OverarchingToMinigamePhase.Starting;
        }
    }
}