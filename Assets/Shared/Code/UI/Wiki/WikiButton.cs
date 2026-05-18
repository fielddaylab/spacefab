using FieldDay;
using FieldDay.Components;
using UnityEngine;

namespace SpaceFab.UI {
    /// <summary>
    /// Discriminator for what a wiki button does when clicked. Tab is the only kind that carries
    /// per-button data (TabIndex); the others are chrome and have no additional state.
    /// </summary>
    public enum WikiButtonKind {
        Tab,
        PageThumb,
        PageNext,
        PagePrev,
        Exit,
        CollapsedIcon,
    }

    /// <summary>
    /// Per-button input surface for the wiki UI. Carries one-frame pointer flags that
    /// WikiSelectSystem consumes on PreUpdate and WikiRefreshSystem clears on Update. Mirrors
    /// ToolbarButton — the UnityUI DynamicButton on the same GameObject drives the flags via
    /// its onClick / onPointerEnter / onPointerExit events.
    /// </summary>
    public class WikiButton : BatchedComponent, IRegistrationCallbacks {
        public WikiButtonKind Kind;

        // Index into WikiContent.Tabs. Only meaningful when Kind == Tab. Baked into each tab
        // button's prefab instance.
        public int TabIndex = -1;

        // Index into the active tab's Pages array (raw, not unlocked-filtered). Only meaningful
        // when Kind == PageThumb. The visuals system uses this to decide visibility (in-window
        // or not), and WikiSelectSystem routes clicks into WikiUtility.SelectPage(PageIndex).
        public int PageIndex = -1;

        // DynamicButton on this GameObject. Assigned in inspector. Its three UnityEvents drive
        // the one-frame flags below via the handlers at the bottom of this file.
        public DynamicButton DynamicButton;

        [HideInInspector] public bool ClickedThisFrame;
        [HideInInspector] public bool PointerEnterThisFrame;
        [HideInInspector] public bool PointerExitThisFrame;

        // Set by WikiAvailabilityUtility on unlock-state resolution. Locked tabs have
        // Available=false, gameObject.SetActive(false), and DynamicButton disabled. Non-Tab
        // buttons are chrome and remain Available=true regardless.
        [HideInInspector] public bool Available = true;

        public void OnRegister() {
            Debug.Log($"WikiButton.OnRegister: {gameObject.name}");
            if (DynamicButton == null) { return; }
            DynamicButton.onClick.AddListener(HandleClick);
            DynamicButton.onPointerEnter.AddListener(HandlePointerEnter);
            DynamicButton.onPointerExit.AddListener(HandlePointerExit);
        }

        public void OnDeregister() {
            if (DynamicButton == null) { return; }
            DynamicButton.onClick.RemoveListener(HandleClick);
            DynamicButton.onPointerEnter.RemoveListener(HandlePointerEnter);
            DynamicButton.onPointerExit.RemoveListener(HandlePointerExit);
        }

        #region Pointer Handlers

        // Pointer events route into WikiUtility so the mutation point is a single static
        // surface — matches ToolbarButton pattern and gives Leaf a clean hook later.

        private void HandleClick() {
            Debug.Log($"WikiButton.HandleClick: {this.gameObject.name} (kind={Kind}, tabIndex={TabIndex}, pageIndex={PageIndex})");
            WikiUtility.OnClick(this);
        }

        private void HandlePointerEnter() {
            WikiUtility.OnPointerEnter(this);
        }

        private void HandlePointerExit() {
            WikiUtility.OnPointerExit(this);
        }

        #endregion // Pointer Handlers
    }

    /// <summary>
    /// Pointer-event mutation surface for WikiButton. Intentionally trivial — the one-frame
    /// flags here are consumed by WikiSelectSystem, which holds the interesting logic.
    /// Declared as partial so WikiState.cs can extend WikiUtility with the command surface
    /// (Open/Close/OpenTo, SelectTab, NextPage, ExpandRoutine, …) without a separate utility
    /// class name.
    /// </summary>
    public static partial class WikiUtility {
        public static void OnClick(WikiButton button) {
            button.ClickedThisFrame = true;
        }

        public static void OnPointerEnter(WikiButton button) {
            button.PointerEnterThisFrame = true;
        }

        public static void OnPointerExit(WikiButton button) {
            button.PointerExitThisFrame = true;
        }
    }
}
