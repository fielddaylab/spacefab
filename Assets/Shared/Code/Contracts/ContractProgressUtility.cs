using BeauUtil;
using SpaceFab.Materials;
using SpaceFab.Research;
using System.Collections.Generic;

namespace SpaceFab
{
    /// <summary>
    /// Per-slot satisfaction record for a contract. Each ContractSlotStatus
    /// corresponds to one MaterialPropertyCheck in the contract's
    /// RequiredMaterialProperties array. Bool-only by current design - if a
    /// future UI wants the list of fulfilling materials per slot, call
    /// FindFulfillingMaterials with that slot's MaterialPropertyCheck.
    /// </summary>
    public struct ContractSlotStatus
    {
        public MaterialPropertyCheck Check;
        public bool IsFulfilled;
    }

    /// <summary>
    /// Queries about whether the player's confirmed material properties
    /// satisfy a contract's requirements. Pure functions over PlayerProgressState
    /// (and optionally the in-session Research sandbox); no caching, no events.
    /// Bit math is delegated to MaterialPropertyRecordUtility - this file only
    /// owns the iteration and short-circuit logic.
    ///
    /// Two flavors of every query:
    ///   - Canonical: reads PlayerProgressState only. Used by Dashboard /
    ///     Overarching, where the player's "real" progress is what matters.
    ///   - Sandbox-aware: also folds in ResearchMinigameState.SandboxProperties
    ///     additively. Used by the Research minigame's own checklist UI so it
    ///     ticks slots live as the player confirms properties this session.
    ///     The sandbox can never un-satisfy a slot already satisfied by
    ///     canonical progress (additive merge).
    /// </summary>
    public static class ContractProgressUtility
    {
        #region Per-record predicate

        /// <summary>
        /// Pure per-record predicate: does this material's record satisfy this
        /// contract slot? One-liner over MaterialPropertyRecordUtility.Has;
        /// exists as a named function so callers (Supply Chain route yields,
        /// future Design / Fabrication checks, ...) have one obvious entry
        /// point for "is this material's data sufficient for this requirement."
        /// </summary>
        public static bool SatisfiesCheck(in MaterialPropertyRecord record, MaterialPropertyCheck check)
        {
            return MaterialPropertyRecordUtility.Has(record, check.Label, check.InComparisonTo);
        }

        #endregion // Per-record predicate

        #region Per-slot queries

        /// <summary>
        /// Short-circuit query: does any material in the player's canonical
        /// progress satisfy this slot? Returns on first match.
        /// </summary>
        public static bool HasAnyFulfillingMaterial(PlayerProgressState progress, MaterialPropertyCheck check)
        {
            foreach (var kvp in progress.MaterialProperties)
            {
                if (SatisfiesCheck(kvp.Value, check))
                {
                    return true;
                }
            }
            return false;
        }

        /// <summary>
        /// Sandbox-aware short-circuit: a slot is fulfilled if any material's
        /// merged (canonical OR sandbox) record satisfies it. Walks canonical
        /// first, then walks sandbox-only keys. No allocation; relies on
        /// dictionary ContainsKey being O(1).
        /// </summary>
        public static bool HasAnyFulfillingMaterial(PlayerProgressState progress, ResearchMinigameState sandbox, MaterialPropertyCheck check)
        {
            // Pass 1: every canonical entry, with sandbox bits OR-merged in.
            foreach (var kvp in progress.MaterialProperties)
            {
                MaterialPropertyRecord merged = kvp.Value;
                if (sandbox.SandboxProperties.TryGetValue(kvp.Key, out var sandboxRecord))
                {
                    MaterialPropertyRecordUtility.Merge(ref merged, sandboxRecord);
                }
                if (SatisfiesCheck(merged, check))
                {
                    return true;
                }
            }
            // Pass 2: sandbox-only entries (no canonical record to merge into).
            foreach (var kvp in sandbox.SandboxProperties)
            {
                if (progress.MaterialProperties.ContainsKey(kvp.Key))
                {
                    continue;
                }
                if (SatisfiesCheck(kvp.Value, check))
                {
                    return true;
                }
            }
            return false;
        }

        /// <summary>
        /// Full enumeration: appends to output every material in the player's
        /// canonical progress whose record satisfies this slot. Caller-allocates
        /// output. Returns the number of IDs appended this call. Does NOT clear
        /// output - callers that want a fresh list should clear before calling.
        ///
        /// Use HasAnyFulfillingMaterial when you only need a bool; this is for
        /// UIs that need to display or further filter the list.
        /// </summary>
        public static int FindFulfillingMaterials(PlayerProgressState progress, MaterialPropertyCheck check, ICollection<StringHash32> output)
        {
            int count = 0;
            foreach (var kvp in progress.MaterialProperties)
            {
                if (SatisfiesCheck(kvp.Value, check))
                {
                    output.Add(kvp.Key);
                    count++;
                }
            }
            return count;
        }

        /// <summary>
        /// Sandbox-aware enumeration. See HasAnyFulfillingMaterial(progress, sandbox, ...)
        /// for the merge semantics.
        /// </summary>
        public static int FindFulfillingMaterials(PlayerProgressState progress, ResearchMinigameState sandbox, MaterialPropertyCheck check, ICollection<StringHash32> output)
        {
            int count = 0;
            foreach (var kvp in progress.MaterialProperties)
            {
                MaterialPropertyRecord merged = kvp.Value;
                if (sandbox.SandboxProperties.TryGetValue(kvp.Key, out var sandboxRecord))
                {
                    MaterialPropertyRecordUtility.Merge(ref merged, sandboxRecord);
                }
                if (SatisfiesCheck(merged, check))
                {
                    output.Add(kvp.Key);
                    count++;
                }
            }
            foreach (var kvp in sandbox.SandboxProperties)
            {
                if (progress.MaterialProperties.ContainsKey(kvp.Key))
                {
                    continue;
                }
                if (SatisfiesCheck(kvp.Value, check))
                {
                    output.Add(kvp.Key);
                    count++;
                }
            }
            return count;
        }

        #endregion // Per-slot queries

        #region Whole-contract queries

        /// <summary>
        /// True iff every MaterialPropertyCheck in the contract's required
        /// properties has at least one fulfilling material in the player's
        /// canonical progress. Short-circuits on the first unfulfilled slot.
        ///
        /// Note: a single material may fulfill multiple slots. If contract
        /// design ever requires distinct materials per slot, this becomes a
        /// bipartite matching problem - revisit then.
        /// </summary>
        public static bool IsContractSatisfied(PlayerProgressState progress, ContractDef contract)
        {
            MaterialPropertyCheck[] checks = contract.RequiredMaterialProperties();
            for (int i = 0; i < checks.Length; i++)
            {
                if (!HasAnyFulfillingMaterial(progress, checks[i]))
                {
                    return false;
                }
            }
            return true;
        }

        /// <summary>
        /// Sandbox-aware whole-contract bool query. See HasAnyFulfillingMaterial
        /// (sandbox overload) for merge semantics.
        /// </summary>
        public static bool IsContractSatisfied(PlayerProgressState progress, ResearchMinigameState sandbox, ContractDef contract)
        {
            MaterialPropertyCheck[] checks = contract.RequiredMaterialProperties();
            for (int i = 0; i < checks.Length; i++)
            {
                if (!HasAnyFulfillingMaterial(progress, sandbox, checks[i]))
                {
                    return false;
                }
            }
            return true;
        }

        /// <summary>
        /// Slot-by-slot view for UIs that render the contract's requirements
        /// list with per-slot tick marks. Caller-allocates output. Does NOT
        /// clear output - callers that want a fresh list should clear before
        /// calling. Appends one ContractSlotStatus per MaterialPropertyCheck
        /// in the contract.
        /// </summary>
        public static void EnumerateContractStatus(PlayerProgressState progress, ContractDef contract, IList<ContractSlotStatus> output)
        {
            MaterialPropertyCheck[] checks = contract.RequiredMaterialProperties();
            for (int i = 0; i < checks.Length; i++)
            {
                output.Add(new ContractSlotStatus {
                    Check = checks[i],
                    IsFulfilled = HasAnyFulfillingMaterial(progress, checks[i]),
                });
            }
        }

        /// <summary>
        /// Sandbox-aware slot-by-slot view. Used by the Research minigame's
        /// own checklist UI so slots tick live as the player confirms
        /// properties this session.
        /// </summary>
        public static void EnumerateContractStatus(PlayerProgressState progress, ResearchMinigameState sandbox, ContractDef contract, IList<ContractSlotStatus> output)
        {
            MaterialPropertyCheck[] checks = contract.RequiredMaterialProperties();
            for (int i = 0; i < checks.Length; i++)
            {
                output.Add(new ContractSlotStatus {
                    Check = checks[i],
                    IsFulfilled = HasAnyFulfillingMaterial(progress, sandbox, checks[i]),
                });
            }
        }

        #endregion // Whole-contract queries
    }
}
