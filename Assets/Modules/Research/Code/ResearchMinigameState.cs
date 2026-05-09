using BeauUtil;
using FieldDay;
using FieldDay.SharedState;
using SpaceFab.Materials;
using SpaceFab.Save;
using System.Collections.Generic;
using UnityEngine;

namespace SpaceFab.Research
{
    public class ResearchMinigameState : MinigameStateBase, IRegistrationCallbacks, IMinigameState
    {
        #region Saved State

        // TODO: Save State

        #endregion // Saved State

        #region Runtime State

        [HideInInspector] public HashSet<StringHash32> AvailableMaterials = new HashSet<StringHash32>();
        // [HideInInspector] public ?? RequiredResearchGoals

        // Sandbox property store. In-session confirmations stay isolated to the
        // minigame; PlayerProgressState is touched only on minigame exit (via
        // ResearchStateUtility.CommitToPlayerProgress). Same vocabulary and shape
        // as PlayerProgressState.MaterialProperties so the export step is a
        // straight bitwise OR-merge with no further translation.
        [HideInInspector] public Dictionary<StringHash32, MaterialPropertyRecord> SandboxProperties = new Dictionary<StringHash32, MaterialPropertyRecord>();

        // Materials whose sandbox record changed during this session. Kept as a
        // hint for delta-merge / debug; export iterates SandboxProperties directly,
        // so this is non-load-bearing for correctness.
        [HideInInspector] public HashSet<StringHash32> SandboxDirty = new HashSet<StringHash32>();

        #endregion // Runtime State

        #region Interfaces

        // IRegistrationCallbacks

        public void OnDeregister()
        {
        }

        public void OnRegister()
        {
            DefaultUpdateMask = UpdateMasks.SetupMask;
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

        #endregion // Interfaces
    }

    public static class ResearchStateUtility
    {
        // Mid-session save-chunk import. Restores the sandbox so a saved-and-resumed
        // research session continues where the player left off. When this runs,
        // it bypasses LoadFromPlayerProgress because the sandbox snapshot is
        // authoritative for the resumed session.
        public static void ImportState(ResearchSaveState saveState, ResearchMinigameState researchState)
        {
            // TODO: deserialize SandboxProperties + SandboxDirty from saveState once
            // ResearchSaveState's chunk format is defined. Mirror the shape of
            // PlayerProgressUtility.UnpackMaterialProperties: iterate MaterialOrderAsset,
            // skip all-zero records to keep the dictionary sparse.
            //
            // Also TODO: deserialize any in-progress observation lists / selected
            // hypothesis / selected material once the prototype's ResearchInventory
            // and ResearchSelectionState shapes are ported.
        }

        // Mid-session save-chunk export. Snapshots the sandbox into the save chunk.
        // Distinct from CommitToPlayerProgress: this preserves an in-progress
        // session for resume; CommitToPlayerProgress promotes confirmations to
        // canonical save state and runs only on minigame exit.
        public static void ExportState(ref ResearchSaveState saveState, ResearchMinigameState researchState)
        {
            // TODO: serialize SandboxProperties + SandboxDirty into saveState once
            // ResearchSaveState's chunk format is defined.
            //
            // Also TODO: serialize any in-progress observation lists / selected
            // hypothesis / selected material once the prototype's ResearchInventory
            // and ResearchSelectionState shapes are ported.
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
        public static void Confirm(ResearchMinigameState state, StringHash32 materialId, MaterialPropertyLabel label, StringHash32 contextMaterialId)
        {
            state.SandboxProperties.TryGetValue(materialId, out var record);
            if (MaterialPropertyRecordUtility.TrySet(ref record, label, contextMaterialId))
            {
                state.SandboxProperties[materialId] = record;
                state.SandboxDirty.Add(materialId);
            }
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

            foreach (var materialId in researchState.AvailableMaterials)
            {
                if (progressState.MaterialProperties.TryGetValue(materialId, out var record)
                    && !MaterialPropertyRecordUtility.IsEmpty(record))
                {
                    researchState.SandboxProperties[materialId] = record;
                }
            }

            // TODO: project the sandbox into the in-session research inventory
            // (the chip-vocabulary mirror of MaterialKnowledge) once the prototype's
            // ResearchInventory is ported. For each (materialId, record) in
            // SandboxProperties, walk the set bits of StaticMask and the two
            // dynamic masks, recover the MaterialPropertyLabel for each bit (the
            // inverse of MaterialPropertyLabelUtility.GetStaticBitIndex for static
            // bits; PDopantFor / NDopantFor for dynamic), then translate the label
            // to its canonical ResearchChipId and add to the inventory's
            // MaterialKnowledge.
            //
            // For dynamic-property bits, the "other material" comes from the
            // MaterialOrderAsset index of the bit, matching how the chip's context
            // is stored runtime-side.
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

        #endregion // PlayerProgress bridge
    }
}