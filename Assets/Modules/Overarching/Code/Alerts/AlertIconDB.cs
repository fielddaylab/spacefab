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
        [Header("Alert Icons")]
        public Sprite NeedsAttentionIcon;
        public Sprite IncompleteIcon;
        public Sprite LockedIcon;
        public Sprite CompleteIcon;
    }

    /// <summary>
    /// Static lookup helpers paired with AlertIconDB. Mirrors the GridSpriteDB / GridSpriteDBUtility
    /// pattern (data on the GlobalAsset, switch-based lookup in a static utility).
    /// </summary>
    public static class AlertIconDBUtility
    {
        // Resolves a single AlertType bit to its sprite. The caller is responsible for passing
        // ONE bit (the AlertType.X enum values themselves are single-bit). Combined masks return
        // null — use OverarchingAlertSystem's bit-walk to call this per-bit.
        public static Sprite LookupIconSprite(AlertIconDB db, AlertType singleBit)
        {
            if (db == null) { return null; }
            switch (singleBit)
            {
                case AlertType.NeedsAttention: return db.NeedsAttentionIcon;
                case AlertType.Incomplete:     return db.IncompleteIcon;
                case AlertType.Locked:         return db.LockedIcon;
                case AlertType.Complete:       return db.CompleteIcon;
                default:                       return null;
            }
        }
    }
}
