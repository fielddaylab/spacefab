using BeauUtil;
using FieldDay.Assets;
using UnityEngine;

namespace SpaceFab.UI {
    /// <summary>
    /// Global config naming the WikiPageData asset ids the player should
    /// start the game with unlocked. Consumed once per save by
    /// OverarchingStartupSequenceSystem on first scene-load: when the
    /// PlayerProgressState.InitialUnlocksApplied flag is false, every
    /// id in this array is fed through WikiUtility.UnlockPage and the
    /// flag is set. From then on the save state owns the unlocked set;
    /// editing this asset later only affects fresh saves.
    ///
    /// Stored as StringHash32[] (page asset names) rather than
    /// WikiPageData[] object refs so the config doesn't pull every
    /// referenced page asset into its serialized dependencies.
    /// AssetName attribute drives an inspector dropdown of authored
    /// WikiPageData assets so designers don't have to type names.
    /// </summary>
    [CreateAssetMenu(menuName = "SpaceFab/Wiki/Initial Unlocks Config")]
    public class WikiInitialUnlocksConfig : GlobalAsset {
        [AssetName(typeof(WikiPageData))]
        [SerializeField] private StringHash32[] m_InitialUnlockedPages;

        public StringHash32[] InitialUnlockedPages { get { return m_InitialUnlockedPages; } }
    }
}
