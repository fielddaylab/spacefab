using BeauUtil;
using FieldDay;
using FieldDay.SharedState;
using SpaceFab.Materials;
using SpaceFab.Save;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace SpaceFab.Research
{
    /// <summary>
    /// A single research discovery: one property newly confirmed about a material during the
    /// current session. Held by ResearchMinigameState.LastDiscovery to answer "which sample most
    /// recently had new information found about it" (e.g. the wiki auto-open onboarding step).
    /// </summary>
    public struct ResearchDiscovery
    {
        public StringHash32 MaterialId;
        public MaterialPropertyLabel Property;
        // The X material for dynamic labels (PDopantFor / NDopantFor); empty for static labels.
        public StringHash32 ContextMaterialId;

        // True once a real discovery has been recorded — a material id is never empty.
        public bool IsValid { get { return !MaterialId.IsEmpty; } }
    }

    public class ResearchMinigameState : MinigameStateBase, IRegistrationCallbacks, IMinigameState
    {
        #region Saved State

        // TODO: Save State

        #endregion // Saved State

        #region Runtime State

        [NonSerialized] public HashSet<StringHash32> AvailableMaterials = new HashSet<StringHash32>();
        [NonSerialized] public MaterialPropertyCheck[] RequiredResearchGoals = new MaterialPropertyCheck[0];

        // Sandbox property store. In-session confirmations stay isolated to the
        // minigame; PlayerProgressState is touched only on minigame exit (via
        // ResearchStateUtility.CommitToPlayerProgress). Same vocabulary and shape
        // as PlayerProgressState.MaterialProperties so the export step is a
        // straight bitwise OR-merge with no further translation.
        [NonSerialized] public Dictionary<StringHash32, MaterialPropertyRecord> SandboxProperties = new Dictionary<StringHash32, MaterialPropertyRecord>();

        // Materials whose sandbox record changed during this session. Kept as a
        // hint for delta-merge / debug; export iterates SandboxProperties directly,
        // so this is non-load-bearing for correctness.
        [NonSerialized] public HashSet<StringHash32> SandboxDirty = new HashSet<StringHash32>();

        // Per-material observation lists. Tentative evidence the player has
        // collected this session. Not persisted; cleared on minigame entry by
        // ResearchStateUtility.LoadFromPlayerProgress. Keyed by the material
        // the observation is being made about (the dynamic context material,
        // when relevant, lives inside each observation entry).
        [NonSerialized] public Dictionary<StringHash32, MaterialObservationList> Observations = new Dictionary<StringHash32, MaterialObservationList>();

        // Set for one frame after ResearchPropertyConfirmBridge writes a
        // newly-confirmed property into SandboxProperties. Drives the
        // tray-rig label refresh so a material whose first property was
        // just confirmed flips from sample-number to ShortName the same
        // frame. Cleared by ResearchMinigameStateRefreshSystem at end
        // of frame.
        [NonSerialized] public bool PropertyConfirmedThisFrame;

        // The most recent genuinely-new property confirmation this session — idempotent
        // re-confirms don't update it. Invalid (default) until the first new confirmation;
        // reset on minigame entry by ResearchStateUtility.LoadFromPlayerProgress. Read by
        // ResearchScripting.Leaf_OpenWikiToLastDiscovery to open that material's wiki page.
        [NonSerialized] public ResearchDiscovery LastDiscovery;

        #endregion // Runtime State

        #region Interfaces

        // IRegistrationCallbacks

        public void OnDeregister()
        {
        }

        public void OnRegister()
        {
            DefaultUpdateMask = UpdateMasks.SetupMask | UpdateMasks.WikiMask;
        }

        // IMinigameState

        public override void ImportState(MinigameSaveStates saveStates)
        {
            ResearchStateUtility.ImportState(saveStates.Research, this);
        }

        public override void ExportState(ref MinigameSaveStates saveStates)
        {
            ResearchStateUtility.ExportState(ref saveStates.Research, this);
        }

        public override void MergeState() {
            ResearchStateUtility.CommitToPlayerProgress(this, Find.State<PlayerProgressState>());
        }

        #endregion // Interfaces
    }

    public static class ResearchStateUtility
    {
        // Mid-session save-chunk import hook. Mid-session resume is not a
        // supported feature today; this exists only to satisfy IMinigameState.
        public static void ImportState(ResearchSaveState saveState, ResearchMinigameState researchState)
        {
            researchState.FoundValidSolution = saveState.FoundValidSolution;
        }

        // Mid-session save-chunk export hook. Mid-session resume is not a
        // supported feature today; this exists only to satisfy IMinigameState.
        public static void ExportState(ref ResearchSaveState saveState, ResearchMinigameState researchState)
        {
            saveState.FoundValidSolution = researchState.FoundValidSolution;
        }

        #region Sandbox helpers

        // Mirrors PlayerProgressUtility.HasConfirmed against the in-session
        // sandbox. The runtime UI should query through here, not against
        // PlayerProgressState directly, so confirmations made this session are
        // visible immediately. For dynamic labels, contextMaterialId is the X in
        // "P-Type Dopant for X". Observation-only labels always return false.
        public static bool HasConfirmed(ResearchMinigameState state, StringHash32 materialId, MaterialPropertyLabel label, StringHash32 contextMaterialId)
        {
            if (!state.SandboxProperties.TryGetValue(materialId, out var record))
            {
                return false;
            }
            return MaterialPropertyRecordUtility.Has(record, label, contextMaterialId);
        }

        // Records a property confirmation in the sandbox only. Does not touch
        // PlayerProgressState. Idempotent (OR-mask semantics). Observation-only
        // labels are silently ignored. SandboxDirty is updated only when the
        // record actually changes, so the dirty set reflects genuine deltas.
        // Returns true iff this call added genuinely-new knowledge (false for an
        // idempotent re-confirm or an ignored observation-only label).
        public static bool Confirm(ResearchMinigameState state, StringHash32 materialId, MaterialPropertyLabel label, StringHash32 contextMaterialId)
        {
            state.SandboxProperties.TryGetValue(materialId, out var record);
            if (MaterialPropertyRecordUtility.TrySet(ref record, label, contextMaterialId))
            {
                state.SandboxProperties[materialId] = record;
                state.SandboxDirty.Add(materialId);
                return true;
            }
            return false;
        }

        // Empties the sandbox. Called after a successful CommitToPlayerProgress
        // so a re-entry to the minigame in the same session starts clean.
        public static void ClearSandbox(ResearchMinigameState state)
        {
            state.SandboxProperties.Clear();
            state.SandboxDirty.Clear();
        }

        #endregion // Sandbox helpers

        #region PlayerProgress bridge

        // Minigame entry: copies existing PlayerProgressState records for materials
        // in scope into the sandbox. After this, the sandbox starts as a faithful
        // mirror of saved progress; new confirmations made this session are
        // additive on top.
        //
        // Only materials currently in researchState.AvailableMaterials are imported,
        // since materials outside the chapter's scope can't be researched this session
        // and don't need to appear in the in-session inventory. Materials not yet in
        // PlayerProgressState are skipped (left as the default zero record on first
        // call to ResearchStateUtility.Confirm).
        public static void LoadFromPlayerProgress(ResearchMinigameState researchState, PlayerProgressState progressState)
        {
            ClearSandbox(researchState);
            ResearchInventoryUtility.ClearAllObservations(researchState);
            researchState.LastDiscovery = default;

            foreach (var materialId in researchState.AvailableMaterials)
            {
                if (progressState.MaterialProperties.TryGetValue(materialId, out var record)
                    && !MaterialPropertyRecordUtility.IsEmpty(record))
                {
                    researchState.SandboxProperties[materialId] = record;
                }
            }
        }

        // Mid-session re-sync: additively folds PlayerProgressState back into the sandbox
        // for every material in scope. Unlike LoadFromPlayerProgress this preserves the
        // observations and LastDiscovery already collected this session, so it is safe to
        // call while the minigame is running. It exists for progress written outside the
        // confirm flow - the Unlock All Knowledge debug menu - to become visible without
        // re-entering the minigame.
        //
        // Raises PropertyConfirmedThisFrame when anything changed: there is no
        // ResearchPropertyConfirmBridge on this path, and the view systems gated on that
        // flag (tray rigs, contract requirements panel) are what make the change show up
        // the same frame. Returns true if any sandbox record changed.
        public static bool MergeFromPlayerProgress(ResearchMinigameState researchState, PlayerProgressState progressState) {
            bool anyChanged = false;

            foreach (StringHash32 materialId in researchState.AvailableMaterials) {
                if (!progressState.MaterialProperties.TryGetValue(materialId, out var progressRecord)) {
                    continue;
                }

                researchState.SandboxProperties.TryGetValue(materialId, out var sandboxRecord);
                MaterialPropertyRecord merged = sandboxRecord;
                MaterialPropertyRecordUtility.Merge(ref merged, progressRecord);
                if (MaterialPropertyRecordUtility.AreEqual(sandboxRecord, merged)) {
                    continue;
                }

                researchState.SandboxProperties[materialId] = merged;
                researchState.SandboxDirty.Add(materialId);
                anyChanged = true;
            }

            if (anyChanged) {
                researchState.PropertyConfirmedThisFrame = true;
            }

            return anyChanged;
        }

        // Minigame exit: merges the sandbox into PlayerProgressState. OR-mask is
        // additive and idempotent: re-running with the same sandbox is a no-op,
        // and the sandbox can never un-confirm a property the player already had.
        // Clears the sandbox after merging.
        //
        // This is the only place the Research minigame writes to PlayerProgressState.
        // It is intended to be called once, from the minigame's exit / commit flow
        // (e.g. when MinigameRequestExitState reaches Confirmed in
        // ResearchRequestExitInterfacerSystem), before the scene unloads.
        public static void CommitToPlayerProgress(ResearchMinigameState researchState, PlayerProgressState progressState)
        {
            foreach (var kvp in researchState.SandboxProperties)
            {
                StringHash32 materialId = kvp.Key;
                MaterialPropertyRecord sandboxRecord = kvp.Value;

                progressState.MaterialProperties.TryGetValue(materialId, out var existing);
                MaterialPropertyRecordUtility.Merge(ref existing, sandboxRecord);

                if (!MaterialPropertyRecordUtility.IsEmpty(existing))
                {
                    progressState.MaterialProperties[materialId] = existing;
                }
            }

            ClearSandbox(researchState);
        }

        // Recomputes FoundValidSolution against the current sandbox + player progress + active
        // contract. Idempotent and monotonic: once the flag is true, this is a no-op (knowledge
        // can't disappear within a session, so the flag never flips back false). Called from
        // ResearchPropertyConfirmBridge.HandleConfirmedProperty after each sandbox confirmation.
        // The contract-accept flow performs the equivalent check directly on the save state via
        // ContractProgressUtility.IsContractSatisfied(progress, contract).
        public static void RefreshFoundValidSolutionFromActiveContract(ResearchMinigameState researchState, PlayerProgressState playerProgress, ContractState contractState)
        {
            if (researchState.FoundValidSolution)
            {
                return;
            }
            if (contractState.ContractDefinition == null) {
                return;
            }
            if (ContractProgressUtility.IsContractSatisfied(playerProgress, researchState, contractState.ContractDefinition))
            {
                researchState.MarkFoundValidSolution();
            }
        }

        #endregion // PlayerProgress bridge
    }
}
