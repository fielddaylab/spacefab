using FieldDay.Assets;
using UnityEngine;

namespace SpaceFab.Fabrication.Sequence
{
    /// <summary>
    /// ScriptableObject definition for a fabrication sequence: ordered steps and glitch authoring.
    /// Referenced by ContractAssetsWrapper. Card visuals are looked up at runtime via the global
    /// SequenceLookup asset (not authored per-level). The checkpoint-rollback lead-in is game-wide
    /// and lives on SequenceUtility.
    /// </summary>
    [CreateAssetMenu(menuName = "SpaceFab/Fabrication/Level")]
    public class FabricationLevel : NamedAsset
    {
        [SerializeField] private FabricationSequence m_sequence;
        public FabricationSequence Sequence => m_sequence;

        // Each step is independently rolled against this
        // probability at sequence reset. Range [0,1].
        [SerializeField, Range(0f, 1f)] private float m_GlitchChance = 0f;
        public float GlitchChance => m_GlitchChance;
    }
}