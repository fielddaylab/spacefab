using FieldDay;
using FieldDay.Components;
using System;
using UnityEngine;

namespace SpaceFab.Research
{
    /// <summary>
    /// World-space UI for the Battery chamber's voltage dial. Three sprite
    /// buttons step the voltage index up/down and flip across the center.
    /// Holds the current index + voltage; mutation goes through VoltageUtility,
    /// and a button press flips OwningChamber.VoltageChangedThisFrame so the
    /// Battery system recomputes on the next tick.
    /// </summary>
    public class VoltageControl : BatchedComponent, IRegistrationCallbacks
    {
        public ResearchSpriteButton IncreaseButton;
        public ResearchSpriteButton DecreaseButton;
        public ResearchSpriteButton FlipButton;
        public SpriteRenderer VoltageIcon;
        public Transform BatteryFlip;

        public BatteryChamberState OwningChamber;
        public bool CanAdjust = true;

        [NonSerialized] public int VoltageIndex;
        [NonSerialized] public float CurrentVoltage;

        public void OnRegister()
        {
            if (IncreaseButton != null && IncreaseButton.Cursor != null)
            {
                IncreaseButton.Cursor.onClick.Register(HandleIncrease);
            }
            if (DecreaseButton != null && DecreaseButton.Cursor != null)
            {
                DecreaseButton.Cursor.onClick.Register(HandleDecrease);
            }
            if (FlipButton != null && FlipButton.Cursor != null)
            {
                FlipButton.Cursor.onClick.Register(HandleFlip);
            }
        }

        public void OnDeregister()
        {
            if (IncreaseButton != null && IncreaseButton.Cursor != null)
            {
                IncreaseButton.Cursor.onClick.Deregister(HandleIncrease);
            }
            if (DecreaseButton != null && DecreaseButton.Cursor != null)
            {
                DecreaseButton.Cursor.onClick.Deregister(HandleDecrease);
            }
            if (FlipButton != null && FlipButton.Cursor != null)
            {
                FlipButton.Cursor.onClick.Deregister(HandleFlip);
            }
        }

        private void HandleIncrease()
        {
            VoltageUtility.Increase(this, Find.GlobalAsset<ResearchVoltageConfig>());
        }

        private void HandleDecrease()
        {
            VoltageUtility.Decrease(this, Find.GlobalAsset<ResearchVoltageConfig>());
        }

        private void HandleFlip()
        {
            VoltageUtility.Flip(this, Find.GlobalAsset<ResearchVoltageConfig>());
        }
    }

    /// <summary>
    /// Mutates VoltageControl in response to player input. Each mutator
    /// updates VoltageIndex + CurrentVoltage, refreshes the icon and flip
    /// transform, and signals OwningChamber.VoltageChangedThisFrame so the
    /// Battery system recomputes on its next ProcessWork.
    /// </summary>
    public static class VoltageUtility
    {
        // Bumps the voltage index up by one. No-op at the high end or when
        // the control is locked.
        public static void Increase(VoltageControl control, ResearchVoltageConfig config)
        {
            if (control == null || config == null || !control.CanAdjust) return;
            if (control.VoltageIndex >= config.Voltages.Length - 1) return;
            control.VoltageIndex++;
            ApplyChange(control, config);
        }

        // Bumps the voltage index down by one. No-op at the low end or when
        // the control is locked.
        public static void Decrease(VoltageControl control, ResearchVoltageConfig config)
        {
            if (control == null || config == null || !control.CanAdjust) return;
            if (control.VoltageIndex <= 0) return;
            control.VoltageIndex--;
            ApplyChange(control, config);
        }

        // Mirrors the index across CenterIndex so positive flips negative
        // and vice versa. Center stays put.
        public static void Flip(VoltageControl control, ResearchVoltageConfig config)
        {
            if (control == null || config == null || !control.CanAdjust) return;
            int mirror = config.CenterIndex + (config.CenterIndex - control.VoltageIndex);
            if (mirror < 0 || mirror >= config.Voltages.Length) return;
            control.VoltageIndex = mirror;
            ApplyChange(control, config);
        }

        // Snaps back to the configured default index. Used when the control
        // is locked while not at default.
        public static void Reset(VoltageControl control, ResearchVoltageConfig config)
        {
            if (control == null || config == null) return;
            if (control.VoltageIndex == config.DefaultIndex) return;
            control.VoltageIndex = config.DefaultIndex;
            ApplyChange(control, config);
        }

        // Toggles whether the player can change voltage. Locking while not at
        // default snaps back; both states refresh button visibility.
        public static void SetAdjustable(VoltageControl control, ResearchVoltageConfig config, bool canAdjust)
        {
            if (control == null) return;
            control.CanAdjust = canAdjust;
            if (!canAdjust)
            {
                Reset(control, config);
            }
            RefreshButtonVisibility(control, config);
        }

        // Recomputes derived state after an index change and signals the
        // owning chamber to recompute current.
        private static void ApplyChange(VoltageControl control, ResearchVoltageConfig config)
        {
            RefreshVisualState(control, config);
            if (control.OwningChamber != null)
            {
                control.OwningChamber.VoltageChangedThisFrame = true;
            }
        }

        // Public so OnRegister can prime the visual state without going
        // through a mutator (which would set VoltageChangedThisFrame).
        public static void RefreshVisualState(VoltageControl control, ResearchVoltageConfig config)
        {
            if (control == null || config == null) return;
            if (control.VoltageIndex < 0 || control.VoltageIndex >= config.Voltages.Length) return;

            control.CurrentVoltage = config.Voltages[control.VoltageIndex];

            if (control.VoltageIcon != null && control.VoltageIndex < config.VoltageIcons.Length)
            {
                control.VoltageIcon.sprite = config.VoltageIcons[control.VoltageIndex];
            }

            if (control.BatteryFlip != null)
            {
                bool flipped = control.VoltageIndex < config.CenterIndex;
                control.BatteryFlip.localRotation = Quaternion.Euler(0f, 0f, flipped ? 180f : 0f);
            }

            RefreshButtonVisibility(control, config);
        }

        // Hides increase/decrease at the array bounds and hides everything
        // when the control is locked.
        private static void RefreshButtonVisibility(VoltageControl control, ResearchVoltageConfig config)
        {
            if (control == null || config == null) return;

            bool atLow = control.VoltageIndex <= 0;
            bool atHigh = control.VoltageIndex >= config.Voltages.Length - 1;
            bool show = control.CanAdjust;

            if (control.IncreaseButton != null)
            {
                control.IncreaseButton.gameObject.SetActive(show && !atHigh);
            }
            if (control.DecreaseButton != null)
            {
                control.DecreaseButton.gameObject.SetActive(show && !atLow);
            }
            if (control.FlipButton != null)
            {
                control.FlipButton.gameObject.SetActive(show);
            }
        }
    }
}
