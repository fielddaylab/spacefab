using BeauUtil;
using FieldDay;
using System;

namespace SpaceFab.Materials
{
    /// <summary>
    /// Evaluator for MaterialPropertyDefinition dependency trees. Answers
    /// "for material M, with X as the dynamic context (or none for static
    /// labels), can this property be confirmed given the player's collected
    /// observations and already-confirmed properties?"
    ///
    /// The evaluator is the only consumer of the dependency graph. Contract
    /// satisfaction (ContractProgressUtility) reads the post-confirmation
    /// state in MaterialPropertyRecord and never touches definitions; that
    /// separation is intentional.
    ///
    /// Inputs are abstracted via a delegate so the evaluator doesn't bind to
    /// a specific observation-storage type (sandbox vs. future shared state
    /// vs. test harness). The chamber-port work fills in the delegate when
    /// observation storage lands on ResearchMinigameState.
    /// </summary>
    public static class MaterialPropertyDefinitionUtility
    {
        /// <summary>
        /// Asks the caller's evidence source whether the player has observed
        /// the given (label, context) pair for the material under evaluation.
        /// For static observations, contextMaterialId is StringHash32.Null.
        /// For dynamic observations, contextMaterialId is the X inherited from
        /// the top-level call.
        /// </summary>
        public delegate bool HasObservationDelegate(MaterialPropertyLabel observationLabel, StringHash32 contextMaterialId);

        /// <summary>
        /// True iff the dependency tree for `label` resolves to true given the
        /// supplied evidence.
        ///   - Static labels: contextMaterialId is ignored; pass StringHash32.Null.
        ///   - Dynamic labels: contextMaterialId names X. Inherits down the tree
        ///     so every dependency entry is checked against the same X.
        ///   - Observation entries (non-persistent labels in the dependency
        ///     array): leaf checks against hasObservation(...).
        ///   - Sub-property entries (persistent labels in the dependency
        ///     array): recurse into that sub-property's own definition,
        ///     OR also check confirmedRecord directly (so an already-confirmed
        ///     sub-property short-circuits the recursion).
        /// </summary>
        public static bool CanConfirm(
            MaterialPropertyLabel label,
            StringHash32 contextMaterialId,
            in MaterialPropertyRecord confirmedRecord,
            HasObservationDelegate hasObservation)
        {
            // TODO: implement once observation storage lands on
            // ResearchMinigameState (or wherever the chamber port decides it
            // belongs). The recursive walk:
            //
            //   1. If label is non-persistent, the caller is asking about an
            //      observation directly. Delegate to hasObservation(label, contextMaterialId).
            //   2. If label is persistent and already set in confirmedRecord
            //      (MaterialPropertyRecordUtility.Has), return true - already confirmed.
            //   3. Look up the definition: Find.GlobalAsset<MaterialPropertyDefinitionAsset>().GetDefinition(label).
            //      If null, return false (no definition = cannot be confirmed).
            //   4. For each entry in def.Dependencies:
            //      - If MaterialPropertyLabelUtility.IsPersistent(entry):
            //          recurse via CanConfirm(entry, contextMaterialId, confirmedRecord, hasObservation).
            //          (The sub-property inherits X from the parent call.)
            //      - Else: leaf observation check via hasObservation(entry, contextMaterialId).
            //      - On any false, short-circuit to false.
            //   5. All dependencies passed -> return true.
            //
            // Open question: cycle protection. The current dependency table is
            // a DAG (HiTempConductor -> Conductor; no back-edges), but nothing
            // enforces that at authoring time. If the asset author makes a
            // cycle, the recursion stack-overflows. Cheapest fix: a small
            // visited-set passed through the recursion. Add only if asset
            // authoring doesn't catch cycles via inspector validation.
            throw new NotImplementedException("Awaiting observation storage on ResearchMinigameState (or equivalent). See body comments for the walk shape.");
        }

        // TODO: bulk-evaluate variant - "for material M, which currently
        // unconfirmed persistent properties just became confirmable?" Walk
        // every persistent label in MaterialPropertyLabel, skip those already
        // set in confirmedRecord, call CanConfirm on each. Used by the
        // hypothesis-submit flow to find out which property the player just
        // earned. Defer until CanConfirm itself is implemented.
    }
}
