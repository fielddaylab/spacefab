using BeauUtil;
using BeauUtil.UI;
using FieldDay;
using FieldDay.Components;
using FieldDay.UI;
using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

namespace SpaceFab.Overarching
{
    public class MinigameZone : BatchedComponent, IRegistrationCallbacks
    {
        // Identifies which minigame this zone represents. Drives the per-zone alert mask lookup
        // in OverarchingAlertState and the FoundValidSolution auto-rule.
        public MinigameId Minigame;
        public int AnalyticsId;

        [Header("Components")]
        [Required] public CursorHint Cursor;
        [Required] public MinigameZoneOverlay Overlay;

        public void OnRegister() {
            Cursor.onClick.AddListener(HandleClick);
            Cursor.onPointerEnter.AddListener(HandlePointerEnter);
            Cursor.onPointerExit.AddListener(HandlePointerExit);
        }

        public void OnDeregister() {
        }

        #region Pointer Handlers

        private void HandleClick()
        {
            MinigameZonesUtility.OnClick(this);
        }

        private void HandlePointerEnter()
        {
            MinigameZonesUtility.SetHoverState(this, true);
        }

        private void HandlePointerExit()
        {
            MinigameZonesUtility.SetHoverState(this, false);
        }

        #endregion // Pointer Handlers
    }

    public static partial class MinigameZonesUtility
    {
        public static void OnClick(MinigameZone zone)
        {
            //zone.ClickedThisFrame = true;
        }

        static public void SetHoverState(MinigameZone zone, bool hoverActive) {
            zone.Overlay.HighlightOutline.color = hoverActive ? Color.white : Color.black;
            zone.Overlay.NameFill.color = hoverActive ? zone.Overlay.ThemeColor : Color.black;
        }
    }
}