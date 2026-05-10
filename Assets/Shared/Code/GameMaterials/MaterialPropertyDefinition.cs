using UnityEngine;

namespace SpaceFab.Materials
{
    /// <summary>
    /// Defines what evidence the player must collect to confirm a single
    /// MaterialPropertyLabel. Multiple definitions for the same Label are
    /// allowed - each represents a distinct dependency tree that confirms
    /// the same property (e.g., a structural-evidence path and a
    /// behavioral-evidence path for PDopantFor). The evaluator OR-combines
    /// them: any one definition's tree being satisfied confirms the property.
    /// Observation labels are not defined here - they are leaves, not
    /// derived.
    ///
    /// Dependencies is a flat array of MaterialPropertyLabel entries. Each
    /// entry's kind is recovered at evaluation time via
    /// MaterialPropertyLabelUtility.IsPersistent: persistent = recurse into
    /// the sub-property's own definition; non-persistent = leaf observation
    /// check against the player's collected evidence. No tagged-union -
    /// the kind is implicit in the label.
    ///
    /// Dynamic context (the X in "P-Type Dopant for X") propagates through
    /// the dependency tree at evaluation time. The definition itself is
    /// X-agnostic; the evaluator inherits X from the top-level call and
    /// applies it to every dependency entry as the walk descends. Observed
    /// today: dopant properties have all-dynamic dependencies; static
    /// properties have all-static dependencies. If that ever mixes, the
    /// inheritance rule needs more nuance (see MaterialPropertyDefinitionUtility).
    /// </summary>
    [CreateAssetMenu(menuName = "SpaceFab/Material Property Definition")]
    public class MaterialPropertyDefinition : ScriptableObject
    {
        public MaterialPropertyLabel Label;
        public MaterialPropertyLabel[] Dependencies;
    }
}
