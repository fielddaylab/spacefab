using FieldDay;
using FieldDay.SharedState;
using Leaf.Runtime;
using SpaceFab.Save;
using UnityEngine;

namespace SpaceFab.Overarching
{
    /// <summary>
    /// One minigame's unlock prerequisites. The minigame is Locked until every entry in
    /// Prerequisites is Complete (FoundValidSolution). Authored on OverarchingAlertState.UnlockRules.
    /// </summary>
    [System.Serializable]
    public struct MinigameUnlockRule
    {
        public MinigameId Minigame;
        // All must be Complete before Minigame unlocks. Empty (or no rule for a minigame at all)
        // means the minigame is always available.
        public MinigameId[] Prerequisites;
    }

    /// <summary>
    /// Per-minigame alert mask storage. One AlertType value per MinigameId, indexed by
    /// (int)MinigameId. Mutation, query, and the scene-entry auto-rule all live in
    /// OverarchingAlertUtility — this class is data-only.
    ///
    /// Lifecycle: scene-scoped (Overarching). OnRegister allocates Masks and flags the visuals
    /// dirty so OverarchingAlertSystem renders an initial pass; AutoRuleApplied is false so the
    /// system also runs the save-flag → Complete / Incomplete / NotStarted derivation (and the
    /// prerequisite locking) on the first tick that has MinigameSaveStates available.
    ///
    /// MinigameId covers the four save-backed minigames (Design / Research / Fabrication / Supply);
    /// the auto-rule maps each to its save state. If a future minigame is added, extend MinigameId
    /// AND MinigameSaveStates together.
    /// </summary>
    public class OverarchingAlertState : SharedStateComponent, IRegistrationCallbacks
    {
        // Designer-authored unlock prerequisites. A minigame with a rule is Locked until every
        // prerequisite is Complete; a minigame with no rule (or empty Prerequisites) is always
        // available. Expresses arbitrary unlock graphs (e.g. Supply requires Design AND Research).
        public MinigameUnlockRule[] UnlockRules;

        // Flat mask per minigame, indexed by (int)MinigameId. Sized to (int)MinigameId.COUNT.
        [HideInInspector] public AlertType[] Masks;

        // Raised by any mask mutation (SetAlertBit / ClearAlertBit) and on first registration.
        // Consumed by OverarchingAlertSystem to rebuild the per-zone icon stacks.
        [HideInInspector] public bool AlertVisualsDirty;

        // False until OverarchingAlertSystem has run the one-shot scene-entry auto-rule that
        // derives the Complete / Incomplete / NotStarted progress bit from each minigame's save
        // flags. After it flips to true, Leaf and other callers can mutate masks freely without
        // the system overwriting them on subsequent ticks.
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
        //   (1) For every MinigameId, set the progress bit from its save flags: Complete if solved,
        //       else Incomplete if started-but-unsolved, else NotStarted.
        //   (2) For every UnlockRule, lock the minigame until every prerequisite is Complete. A
        //       minigame with no rule (or empty Prerequisites) is always available.
        // Both passes use SetAlertBit, which only ORs in — Leaf hooks that pre-set bits (e.g.
        // NeedsAttention) before scene entry are preserved. Called once per Overarching scene load.
        public static void ApplyAutoRuleFromSaveStates(OverarchingAlertState state, MinigameSaveStates saveStates)
        {
            if (state == null || saveStates == null) { return; }

            // Pass 1: progress bit (Complete / Incomplete / NotStarted) per minigame.
            for (int i = 0; i < (int)MinigameId.COUNT; i++)
            {
                MinigameId mg = (MinigameId)i;
                MinigameSaveStateBase save = MinigameSaveUtility.GetState(saveStates, mg);
                bool solved = save != null && save.FoundValidSolution;
                bool started = save != null && save.Started;

                AlertType progress = solved ? AlertType.Complete
                    : (started ? AlertType.Incomplete : AlertType.NotStarted);
                SetAlertBit(state, mg, progress);
            }

            // Pass 2: prerequisite locking. A minigame with a rule is Locked until every
            // prerequisite is Complete. Minigames without a rule are always available. Transitivity
            // is automatic — a locked minigame can't be solved, so it keeps its dependents locked.
            if (state.UnlockRules == null) { return; }
            for (int i = 0; i < state.UnlockRules.Length; i++)
            {
                MinigameUnlockRule rule = state.UnlockRules[i];
                if (rule.Prerequisites == null || rule.Prerequisites.Length == 0) { continue; }

                bool allMet = true;
                for (int p = 0; p < rule.Prerequisites.Length; p++)
                {
                    if (!GetSolvedFlag(saveStates, rule.Prerequisites[p]))
                    {
                        allMet = false;
                        break;
                    }
                }
                if (!allMet)
                {
                    SetAlertBit(state, rule.Minigame, AlertType.Locked);
                }
            }
        }

        // Resolves a MinigameId to its corresponding MinigameSaveStates.FoundValidSolution flag.
        // Returns false for any id without a save-state slot (defensive — none today, but the
        // enum could grow with non-save-backed entries in the future).
        private static bool GetSolvedFlag(MinigameSaveStates saveStates, MinigameId mg)
        {
            MinigameSaveStateBase save = MinigameSaveUtility.GetState(saveStates, mg);
            return save != null && save.FoundValidSolution;
        }
    }
}
