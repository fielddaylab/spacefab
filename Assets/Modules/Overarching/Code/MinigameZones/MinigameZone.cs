using BeauUtil;
using BeauUtil.UI;
using FieldDay;
using FieldDay.Components;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

namespace SpaceFab.Overarching
{
    public class MinigameZone : BatchedComponent, IRegistrationCallbacks
    {
        public int ZoneIndex = -1;
        public PointerListener PointerListener;
        public SceneReference MinigameScene;

        public bool ClickedThisFrame;
        public bool PointerEnterThisFrame;
        public bool PointerExitThisFrame;

        [Header("Visuals")]
        public Sprite NormalHighlight;
        public Sprite EmphasisHighlight;
        public SpriteRenderer HighlightRenderer;
        public TMP_Text StationLabel;

        public void OnRegister()
        {
            PointerListener.onClick.AddListener(HandleClick);
            PointerListener.onPointerEnter.AddListener(HandlePointerEnter);
            PointerListener.onPointerExit.AddListener(HandlePointerExit);

            HighlightRenderer.enabled = false;
            StationLabel.enabled = false;
        }

        public void OnDeregister()
        {
            PointerListener.onClick.RemoveListener(HandleClick);
            PointerListener.onPointerEnter.RemoveListener(HandlePointerEnter);
            PointerListener.onPointerExit.RemoveListener(HandlePointerExit);
        }

        #region Pointer Handlers

        private void HandleClick()
        {
            MinigameZonesUtility.OnClick(this);
        }

        private void HandlePointerEnter()
        {
            MinigameZonesUtility.OnPointerEnter(this);
        }

        private void HandlePointerExit()
        {
            MinigameZonesUtility.OnPointerExit(this);
        }

        #endregion // Pointer Handlers
    }

    public static partial class MinigameZonesUtility
    {
        public static void OnClick(MinigameZone zone)
        {
            zone.ClickedThisFrame = true;
        }

        public static void OnPointerEnter(MinigameZone zone)
        {
            zone.PointerEnterThisFrame = true;
        }

        public static void OnPointerExit(MinigameZone zone)
        {
            zone.PointerExitThisFrame = true;
        }
    }
}