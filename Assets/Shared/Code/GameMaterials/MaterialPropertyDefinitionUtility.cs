using BeauUtil;
using FieldDay;

namespace SpaceFab.Materials
{
    /// <summary>
    /// Evaluator for MaterialPropertyDefinition dependency trees. Answers
    /// "for material M, with X as the dynamic context (or none for static
    /// labels), can this property be confirmed given the player's collected
    /// observations and already-confirmed properties?"
    ///
    /// The evaluator is the only consumer of the dependency graph. Contract
    /// satisfaction reads the post-confirmation state in MaterialPropertyRecord
    /// and never touches definitions; that separation is intentional.
    ///
    /// Observation storage is abstracted via a delegate so the evaluator
    /// doesn't bind to a specific store (sandbox observation list, test
    /// harness, etc.). Callers supply a lookup that returns whether the
    /// player has the requested (label, context) observation for the
    /// material under evaluation.
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
            return CanConfirmInternal(label, contextMaterialId, confirmedRecord, hasObservation);
        }

        // Recursive walk. The signature differs from the public entrypoint
        // only by being internal to allow the recursion target to be inlined
        // by the JIT.
        //   1. Observation labels: leaf check.
        //   2. Persistent labels already in the record: short-circuit true.
        //   3. Persistent labels with no record entry: walk every registered
        //      definition; the first whose dependency list is fully satisfied
        //      confirms the label.
        // Cycle protection is not implemented; the dependency graph is
        // assumed to be a DAG. Authoring-time cycles will stack-overflow.
        private static bool CanConfirmInternal(
            MaterialPropertyLabel label,
            StringHash32 contextMaterialId,
            in MaterialPropertyRecord confirmedRecord,
            HasObservationDelegate hasObservation)
        {
            // 1. Observation labels are leaves.
            if (!MaterialPropertyLabelUtility.IsPersistent(label))
            {
                return hasObservation != null && hasObservation(label, contextMaterialId);
            }

            // 2. Already confirmed in the record.
            if (MaterialPropertyRecordUtility.Has(confirmedRecord, label, contextMaterialId))
            {
                return true;
            }

            // 3. Try every registered definition.
            MaterialPropertyDefinitionAsset registry = Find.GlobalAsset<MaterialPropertyDefinitionAsset>();
            if (registry == null)
            {
                return false;
            }

            MaterialPropertyDefinition[] definitions = registry.GetDefinitions(label);
            for (int i = 0; i < definitions.Length; i++)
            {
                if (IsDefinitionSatisfied(definitions[i], contextMaterialId, confirmedRecord, hasObservation))
                {
                    return true;
                }
            }
            return false;
        }

        // Returns true if every dependency in the definition resolves to true
        // for the given context. Sub-property dependencies recurse; observation
        // dependencies hit the delegate.
        private static bool IsDefinitionSatisfied(
            MaterialPropertyDefinition definition,
            StringHash32 contextMaterialId,
            in MaterialPropertyRecord confirmedRecord,
            HasObservationDelegate hasObservation)
        {
            if (definition == null || definition.Dependencies == null)
            {
                return false;
            }

            for (int i = 0; i < definition.Dependencies.Length; i++)
            {
                MaterialPropertyLabel dep = definition.Dependencies[i];
                if (!CanConfirmInternal(dep, contextMaterialId, confirmedRecord, hasObservation))
                {
                    return false;
                }
            }
            return true;
        }

        /// <summary>
        /// Returns the first definition whose dependency list resolves to true
        /// under the given evidence. Used by the hypothesis-confirm flow to
        /// know which observations to consume on success. Returns null if no
        /// definition is satisfied (caller should treat that as "cannot
        /// confirm right now").
        /// </summary>
        public static MaterialPropertyDefinition FindSatisfiedDefinition(
            MaterialPropertyLabel label,
            StringHash32 contextMaterialId,
            in MaterialPropertyRecord confirmedRecord,
            HasObservationDelegate hasObservation)
        {
            if (!MaterialPropertyLabelUtility.IsPersistent(label))
            {
                return null;
            }

            MaterialPropertyDefinitionAsset registry = Find.GlobalAsset<MaterialPropertyDefinitionAsset>();
            if (registry == null)
            {
                return null;
            }

            MaterialPropertyDefinition[] definitions = registry.GetDefinitions(label);
            for (int i = 0; i < definitions.Length; i++)
            {
                if (IsDefinitionSatisfied(definitions[i], contextMaterialId, confirmedRecord, hasObservation))
                {
                    return definitions[i];
                }
            }
            return null;
        }
    }
}
