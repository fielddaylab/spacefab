using FieldDay;
using FieldDay.SharedState;
using Leaf.Runtime;
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
        // Designer-authored progression order. When the auto-rule runs, every minigame that
        // comes after the first unsolved entry in this sequence has Locked OR'd into its mask.
        // Leave empty (length 0 or null) to disable progression locking entirely. Order matters;
        // duplicates are allowed but redundant.
        public MinigameId[] ProgressionSequence;

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

        [LeafMember("SetMinigameAlert")]
        public static void SetAlertBitLeaf(MinigameId mg, AlertType bit)
        {
            var state = Find.State<OverarchingAlertState>();
            if (state == null || state.Masks == null) { return; }
            int idx = (int)mg;
            if (idx < 0 || idx >= state.Masks.Length) { return; }
            AlertType prev = state.Masks[idx];
            AlertType next = prev | bit;
            if (next == prev) { return; }
            state.Masks[idx] = next;
            state.AlertVisualsDirty = true;
        }

        [LeafMember("ClearMinigameAlert")]
        public static void ClearAlertBitLeaf(MinigameId mg, AlertType bit)
        {
            var state = Find.State<OverarchingAlertState>();
            if (state == null || state.Masks == null) { return; }
            int idx = (int)mg;
            if (idx < 0 || idx >= state.Masks.Length) { return; }
            AlertType prev = state.Masks[idx];
            AlertType next = prev & ~bit;
            if (next == prev) { return; }
            state.Masks[idx] = next;
            state.AlertVisualsDirty = true;
        }

        // One-shot derivation. Runs in two passes:
        //   (1) For every MinigameId, set NeedsAttention if !FoundValidSolution, Complete if true.
        //   (2) Walk ProgressionSequence in order; once we hit the first entry with
        //       !FoundValidSolution, every subsequent entry gets Locked OR'd into its mask.
        // Both passes use SetAlertBit, which only ORs in — Leaf hooks that pre-set bits before
        // scene entry are preserved. Called exactly once per Overarching scene load.
        public static void ApplyAutoRuleFromSaveStates(OverarchingAlertState state, MinigameSaveStates saveStates)
        {
            if (state == null || saveStates == null) { return; }

            // Pass 1: NeedsAttention / Complete per minigame.
            for (int i = 0; i < (int)MinigameId.COUNT; i++)
            {
                MinigameId mg = (MinigameId)i;
                bool solved = GetSolvedFlag(saveStates, mg);
                SetAlertBit(state, mg, solved ? AlertType.Complete : AlertType.NeedsAttention);
            }

            // Pass 2: progression-order locking. Lock every minigame that appears in the sequence
            // after the player's current frontier (the first unsolved entry). Skipped entirely
            // when no progression sequence is authored.
            if (state.ProgressionSequence == null || state.ProgressionSequence.Length == 0) { return; }
            bool reachedFrontier = false;
            for (int i = 0; i < state.ProgressionSequence.Length; i++)
            {
                MinigameId mg = state.ProgressionSequence[i];
                if (reachedFrontier)
                {
                    SetAlertBit(state, mg, AlertType.Locked);
                    continue;
                }
                if (!GetSolvedFlag(saveStates, mg))
                {
                    reachedFrontier = true;
                }
            }
        }

        // Resolves a MinigameId to its corresponding MinigameSaveStates.FoundValidSolution flag.
        // Returns false for any id without a save-state slot (defensive — none today, but the
        // enum could grow with non-save-backed entries in the future).
        private static bool GetSolvedFlag(MinigameSaveStates saveStates, MinigameId mg)
        {
            switch (mg)
            {
                case MinigameId.Design:      return saveStates.Design      != null && saveStates.Design.FoundValidSolution;
                case MinigameId.Research:    return saveStates.Research    != null && saveStates.Research.FoundValidSolution;
                case MinigameId.Fabrication: return saveStates.Fabrication != null && saveStates.Fabrication.FoundValidSolution;
                case MinigameId.Supply:      return saveStates.Supply      != null && saveStates.Supply.FoundValidSolution;
                default:                     return false;
            }
        }
    }
}
