using BeauUtil;
using FieldDay.Assets;
using SpaceFab.Materials;
using UnityEngine;

namespace SpaceFab.Research {
    /// <summary>
    /// Visual data for rendering a MaterialAsset as a gem rig in the Research
    /// minigame. Paired by id: MaterialId matches MaterialAsset.AssetId.
    /// Resolved via Find.NamedAsset&lt;ResearchMaterialView&gt;. Kept as a parallel
    /// asset so MaterialAsset (shared across modules) stays lean and free of
    /// Research-specific visual data.
    /// </summary>
    [CreateAssetMenu(menuName = "SpaceFab/Research/Material View")]
    public class ResearchMaterialView : NamedAsset {
        // The MaterialAsset this view describes. [AssetName] renders the
        // inspector field as a dropdown of authored MaterialAsset assets via
        // AssetNamePropertyDrawer.
        [AssetName(typeof(MaterialAsset), true)]
        public SerializedHash32 MaterialId;

        // Sprite used when the material has a single atom in its structure.
        public Sprite SingleAtomSprite;

        // Sprite used when the material has multiple atoms in its structure.
        public Sprite MultiAtomSprite;

        // Gem renderer color.
        public Color GemColor = Color.white;

        // Uniform scale applied to the gem renderer's transform.
        public float GemScale = 1f;

        // Whether the material's gem uses the multi-atom sprite path.
        public bool IsMultiAtom;

        // Human-readable per-material identifier shown in UI ("SAMPLE 13").
        // Authored per asset; independent of asset id, ordering, or runtime
        // indexing so artists can place numbers freely.
        public int SampleNumber;

        // Alternativel switch to strings for letter labels (A, B, C)
        // retain sample number in case change later
        public string SampleLabel;
    }
}
