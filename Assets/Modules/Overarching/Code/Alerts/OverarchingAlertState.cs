using FieldDay;
using FieldDay.SharedState;
using SpaceFab.Save;
using UnityEngine;

namespace SpaceFab.Overarching
{
    /// <summary>
    /// Per-minigame alert mask storage. One AlertType value per MinigameId, indexed by
    /// (int)MinigameId. Mutation, query, and the scene-entry auto-rule all live in
    /// OverarchingAlertUtility — this class is data-only.
    ///
    /// Lifecycle: scene-scoped (Overarching). OnRegister allocates Masks and flags the visuals
    /// dirty so OverarchingAlertSystem renders an initial pass; AutoRuleApplied is false so the
    /// system also runs the FoundValidSolution → NeedsAttention / Complete derivation on the
    /// first tick that has MinigameSaveStates available.
    ///
    /// MinigameId covers the four save-backed minigames (Design / Research / Fabrication / Supply);
    /// the auto-rule maps each to its save state. If a future minigame is added, extend MinigameId
    /// AND MinigameSaveStates AND the switch in ApplyAutoRuleFromSaveStates together.
    /// </summary>
    public class OverarchingAlertState : SharedStateComponent, IRegistrationCallbacks
    {
        // Flat mask per minigame, indexed by (int)MinigameId. Sized to (int)MinigameId.COUNT.
        [HideInInspector] public AlertType[] Masks;

        // Raised by any mask mutation (SetAlertBit / ClearAlertBit) and on first registration.
        // Consumed by OverarchingAlertSystem to rebuild the per-zone icon stacks.
        [HideInInspector] public bool AlertVisualsDirty;

        // False until OverarchingAlertSystem has run the one-shot scene-entry auto-rule that
        // derives NeedsAttention / Complete bits from each minigame's FoundValidSolution. After
        // it flips to true, Leaf and other callers can mutate masks freely without the system
        // overwriting them on subsequent ticks.
        [HideInInspector] public bool AutoRuleApplied;

        public void OnRegister()
        {
            Masks = new AlertType[(int)MinigameId.COUNT];
            AlertVisualsDirty = true;
            AutoRuleApplied = false;
        }

        public void OnDeregister()
        {
        }
    }

    /// <summary>
    /// Query / mutation / scene-entry derivation helpers for OverarchingAlertState. Set + clear
    /// methods raise AlertVisualsDirty so the system repaints on the next tick. The Leaf-callable
    /// surface is annotated with comments — actual [LeafMember] attributes go in when Leaf is
    /// integrated (per the project's "not yet integrated" convention; see StationControlState.cs).
    /// </summary>
    public static class OverarchingAlertUtility
    {
        public static AlertType GetMask(OverarchingAlertState state, MinigameId mg)
        {
            if (state == null || state.Masks == null) { return AlertType.None; }
            int idx = (int)mg;
            if (idx < 0 || idx >= state.Masks.Length) { return AlertType.None; }
            return state.Masks[idx];
        }

        public static bool HasAlert(OverarchingAlertState state, MinigameId mg, AlertType bit)
        {
            return (GetMask(state, mg) & bit) != 0;
        }

        // [LeafMember("SetMinigameAlert")] when Leaf wires in
        public static void SetAlertBit(OverarchingAlertState state, MinigameId mg, AlertType bit)
        {
            if (state == null || state.Masks == null) { return; }
            int idx = (int)mg;
            if (idx < 0 || idx >= state.Masks.Length) { return; }
            AlertType prev = state.Masks[idx];
            AlertType next = prev | bit;
            if (next == prev) { return; }
            state.Masks[idx] = next;
            state.AlertVisualsDirty = true;
        }

        // [LeafMember("ClearMinigameAlert")] when Leaf wires in
        public static void ClearAlertBit(OverarchingAlertState state, MinigameId mg, AlertType bit)
        {
            if (state == null || state.Masks == null) { return; }
            int idx = (int)mg;
            if (idx < 0 || idx >= state.Masks.Length) { return; }
            AlertType prev = state.Masks[idx];
            AlertType next = prev & ~bit;
            if (next == prev) { return; }
            state.Masks[idx] = next;
            state.AlertVisualsDirty = true;
        }

        // One-shot derivation. For each minigame that has a save state, set NeedsAttention if
        // FoundValidSolution is false, or Complete if it's true. Does NOT clear other bits —
        // Leaf hooks that set bits before scene entry will be ORed against, not overwritten.
        // Called exactly once per Overarching scene load by OverarchingAlertSystem.
        public static void ApplyAutoRuleFromSaveStates(OverarchingAlertState state, MinigameSaveStates saveStates)
        {
            if (state == null || saveStates == null) { return; }
            ApplyOne(state, MinigameId.Design,      saveStates.Design      != null && saveStates.Design.FoundValidSolution);
            ApplyOne(state, MinigameId.Research,    saveStates.Research    != null && saveStates.Research.FoundValidSolution);
            ApplyOne(state, MinigameId.Fabrication, saveStates.Fabrication != null && saveStates.Fabrication.FoundValidSolution);
            ApplyOne(state, MinigameId.Supply,      saveStates.Supply      != null && saveStates.Supply.FoundValidSolution);
        }

        // Sets exactly one of the two mutually-exclusive auto-rule bits for the given minigame.
        private static void ApplyOne(OverarchingAlertState state, MinigameId mg, bool solved)
        {
            SetAlertBit(state, mg, solved ? AlertType.Complete : AlertType.NeedsAttention);
        }
    }
}
