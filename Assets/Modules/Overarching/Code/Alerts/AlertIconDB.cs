using FieldDay.Assets;
using UnityEngine;

namespace SpaceFab.Overarching
{
    /// <summary>
    /// GlobalAsset mapping each AlertType bit to its overlay icon sprite. Authored once as a
    /// project asset (CreateAssetMenu → SpaceFab/Overarching/Alert Icon DB) and looked up at
    /// runtime by OverarchingAlertSystem when spawning the per-zone icon stack.
    /// </summary>
    [CreateAssetMenu(menuName = "SpaceFab/Overarching/Alert Icon DB")]
    public class AlertIconDB : GlobalAsset
    {
        [Header("Not Started")]
        public Sprite NotStartedBaseIcon;
        public Sprite NotStartedInnerIcon;
        public Sprite NotStartedSymbolIcon;

        [Header("Needs Attention")]
        public Sprite NeedsAttentionBaseIcon;
        public Sprite NeedsAttentionInnerIcon;
        public Sprite NeedsAttentionSymbolIcon;

        [Header("Incomplete")]
        public Sprite IncompleteBaseIcon;
        public Sprite IncompleteInnerIcon;
        public Sprite IncompleteSymbolIcon;

        [Header("Locked")]
        public Sprite LockedBaseIcon;
        public Sprite LockedInnerIcon;
        public Sprite LockedSymbolIcon;

        [Header("Complete")]
        public Sprite CompleteBaseIcon;
        public Sprite CompleteInnerIcon;
        public Sprite CompleteSymbolIcon;
    }

    /// <summary>
    /// Static lookup helpers paired with AlertIconDB. Mirrors the GridSpriteDB / GridSpriteDBUtility
    /// pattern (data on the GlobalAsset, switch-based lookup in a static utility).
    /// </summary>
    public static class AlertIconDBUtility
    {
        // Resolves a single AlertType bit to its three layered sprites (base plate, inner shape,
        // top symbol). The caller passes ONE bit (the AlertType.X values are single-bit). Returns
        // false for None or a combined mask — OverarchingAlertSystem walks the bits and calls this
        // per-bit. The inner sprite is tinted per-minigame by the caller (see MinigameZoneOverlayDB);
        // base and symbol render untinted.
        public static bool TryLookupIcon(AlertIconDB db, AlertType singleBit, out Sprite baseSprite, out Sprite innerSprite, out Sprite symbolSprite)
        {
            baseSprite = null;
            innerSprite = null;
            symbolSprite = null;
            if (db == null) { return false; }
            switch (singleBit)
            {
                case AlertType.NotStarted:
                    baseSprite = db.NotStartedBaseIcon;
                    innerSprite = db.NotStartedInnerIcon;
                    symbolSprite = db.NotStartedSymbolIcon;
                    return true;
                case AlertType.NeedsAttention:
                    baseSprite = db.NeedsAttentionBaseIcon;
                    innerSprite = db.NeedsAttentionInnerIcon;
                    symbolSprite = db.NeedsAttentionSymbolIcon;
                    return true;
                case AlertType.Incomplete:
                    baseSprite = db.IncompleteBaseIcon;
                    innerSprite = db.IncompleteInnerIcon;
                    symbolSprite = db.IncompleteSymbolIcon;
                    return true;
                case AlertType.Locked:
                    baseSprite = db.LockedBaseIcon;
                    innerSprite = db.LockedInnerIcon;
                    symbolSprite = db.LockedSymbolIcon;
                    return true;
                case AlertType.Complete:
                    baseSprite = db.CompleteBaseIcon;
                    innerSprite = db.CompleteInnerIcon;
                    symbolSprite = db.CompleteSymbolIcon;
                    return true;
                default:
                    return false;
            }
        }
    }
}
