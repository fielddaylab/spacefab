using BeauUtil;
using FieldDay;
using System.Collections.Generic;

namespace SpaceFab.Materials
{
    /// <summary>
    /// A single (label, context) observation leaf in a hypothesis's
    /// decomposition. Output element of MaterialPropertyDefinitionUtility.
    /// DecomposeToObservations. Context is the X in "P-Type Dopant for X"
    /// inherited down the dependency tree; for static observations it is
    /// StringHash32.Null.
    ///
    /// AncestorProperty (when HasAncestorProperty is true) names the
    /// outermost sub-property — i.e., the direct child of the page's top-
    /// level definition that this leaf descended from. The UI uses this to
    /// auto-populate the leaf's chip when AncestorProperty is already
    /// confirmed for the slotted material: the player doesn't have to
    /// re-collect observations for a sub-property they've already proven.
    ///
    /// ObservationType is cached at decomposition time from the static
    /// MaterialObservationChamberLookup; the observation chip uses it to
    /// look up the right sprite pair. Observations can sit outside any
    /// chamber (e.g. Special, ConfirmedProperty), which is why this is
    /// an ObservationType — broader than ChamberType.
    /// </summary>
    public struct MaterialObservationEntry
    {
        public MaterialPropertyLabel Label;
        public StringHash32 Context;
        public ObservationType ObservationType;
        public MaterialPropertyLabel AncestorProperty;
        public bool HasAncestorProperty;
    }

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

        /// <summary>
        /// Walks a definition's dependency tree and appends every leaf
        /// (observation) entry to output. Sub-property dependencies recurse
        /// into their canonical (first-registered) definition, since the
        /// caller has already split per-definition at the hypothesis-page
        /// level. Inherited context propagates down the tree.
        ///
        /// Each leaf carries the outermost sub-property it descended from
        /// (the direct child of the supplied top-level definition), so the
        /// UI can auto-populate that leaf when its outermost ancestor is
        /// already confirmed for the slotted material. Nested sub-properties
        /// do not overwrite the ancestor: the rule is "if any ancestor on
        /// the path is confirmed, the leaf is auto-populated," and the
        /// outermost ancestor is sufficient to make that decision because
        /// the evaluator's record-Has check short-circuits at the outermost
        /// confirmed level.
        /// </summary>
        public static void DecomposeToObservations(
            MaterialPropertyDefinition definition,
            StringHash32[] inheritedContext,
            List<MaterialObservationEntry> output)
        {
            if (definition == null || definition.Dependencies == null)
            {
                return;
            }

            MaterialPropertyDefinitionAsset registry = Find.GlobalAsset<MaterialPropertyDefinitionAsset>();

            for (int i = 0; i < definition.Dependencies.Length; i++)
            {
                MaterialPropertyLabel dep = definition.Dependencies[i];

                if (!MaterialPropertyLabelUtility.IsPersistent(dep))
                {
                    foreach (StringHash32 context in inheritedContext)
                    {
                        // Direct observation dependency — no ancestor sub-property.
                        output.Add(new MaterialObservationEntry
                        {
                            Label = dep,
                            Context = context,
                            ObservationType = MaterialObservationChamberLookup.GetChamberType(dep),
                            HasAncestorProperty = false,
                        });
                    }
                    continue;
                }

                if (registry == null)
                {
                    continue;
                }

                MaterialPropertyDefinition[] subDefs = registry.GetDefinitions(dep);
                if (subDefs.Length > 0)
                {
                    // Descended into a sub-property. dep becomes the
                    // outermost ancestor for every leaf below this point.
                    DecomposeUnderAncestor(subDefs[0], inheritedContext, dep, output, registry);
                }
            }
        }

        /// <summary>
        /// True iff the given observation (label, context) appears as a
        /// leaf in the decomposition of any persistent property in
        /// `materialProperties` (the MaterialAsset.Properties array).
        /// Used as the "is this observation actually true for this
        /// material?" ground-truth check — distinct from the evaluator,
        /// which only asks whether the player *claimed* the observation.
        ///
        /// Walks every registered definition for each property
        /// (alternate satisfaction paths) so the check accepts any
        /// observation that supports any valid way to confirm any
        /// property the material has.
        /// </summary>
        public static bool IsObservationTrueForProperties(
            MaterialPropertyLabel[] materialProperties,
            MaterialPropertyLabel observationLabel,
            StringHash32 observationContext,
            StringHash32[] availableContexts)
        {
            if (materialProperties == null || materialProperties.Length == 0) return false;

            MaterialPropertyDefinitionAsset registry = Find.GlobalAsset<MaterialPropertyDefinitionAsset>();
            if (registry == null) return false;

            // Reuse a single scratch list across each property so a
            // material with N authored properties stays O(total leaves)
            // and doesn't allocate per property.
            List<MaterialObservationEntry> scratch = new List<MaterialObservationEntry>(8);
            for (int i = 0; i < materialProperties.Length; i++) {
                MaterialPropertyLabel prop = materialProperties[i];
                if (!MaterialPropertyLabelUtility.IsPersistent(prop)) continue;

                MaterialPropertyDefinition[] defs = registry.GetDefinitions(prop);
                for (int d = 0; d < defs.Length; d++) {
                    scratch.Clear();
                    // inheritedContext: use all available contexts
                    DecomposeToObservations(defs[d], availableContexts, scratch);
                    for (int s = 0; s < scratch.Count; s++) {
                        if (scratch[s].Label == observationLabel && scratch[s].Context == observationContext) {
                            return true;
                        }
                    }
                }
            }
            return false;
        }

        // Continues the decomposition under an established outermost ancestor.
        // Nested sub-property descents reuse the same ancestor — only the
        // outermost matters for auto-population.
        private static void DecomposeUnderAncestor(
            MaterialPropertyDefinition definition,
            StringHash32[] inheritedContext,
            MaterialPropertyLabel outermostAncestor,
            List<MaterialObservationEntry> output,
            MaterialPropertyDefinitionAsset registry)
        {
            if (definition == null || definition.Dependencies == null)
            {
                return;
            }

            for (int i = 0; i < definition.Dependencies.Length; i++)
            {
                MaterialPropertyLabel dep = definition.Dependencies[i];

                if (!MaterialPropertyLabelUtility.IsPersistent(dep))
                {
                    foreach (StringHash32 context in inheritedContext)
                    {
                        output.Add(new MaterialObservationEntry
                        {
                            Label = dep,
                            Context = context,
                            ObservationType = MaterialObservationChamberLookup.GetChamberType(dep),
                            AncestorProperty = outermostAncestor,
                            HasAncestorProperty = true,
                        });
                    }
                    continue;
                }

                if (registry == null)
                {
                    continue;
                }

                MaterialPropertyDefinition[] subDefs = registry.GetDefinitions(dep);
                if (subDefs.Length > 0)
                {
                    DecomposeUnderAncestor(subDefs[0], inheritedContext, outermostAncestor, output, registry);
                }
            }
        }
    }
}
