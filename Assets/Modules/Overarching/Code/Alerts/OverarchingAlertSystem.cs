using BeauPools;
using FieldDay;
using FieldDay.Systems;
using SpaceFab.Save;
using UnityEngine;

namespace SpaceFab.Overarching
{
    /// <summary>
    /// Owns two responsibilities under one ProcessWork:
    ///
    ///   1. **Auto-rule:** the first tick after OverarchingMask resumes, derives NeedsAttention /
    ///      Complete bits for each minigame from its MinigameSaveStates.FoundValidSolution. Runs
    ///      once per scene load (gated by OverarchingAlertState.AutoRuleApplied).
    ///
    ///   2. **Visual refresh:** when OverarchingAlertState.AlertVisualsDirty is raised (by the
    ///      auto-rule, by an inspector mask edit, or by a Leaf-callable Set/Clear), frees every
    ///      pooled alert icon and respawns one per set bit per zone, parented under the zone's
    ///      AlertIconContainer at a fixed horizontal stack offset.
    ///
    /// Runs on LateUpdate under OverarchingMask so MinigameZones (which auto-register on scene
    /// load) are guaranteed to be present, and so MinigameSaveStates is hydrated by SaveMgr
    /// before the first tick.
    /// </summary>
    public class OverarchingAlertSystem : SystemComponent
    {
        // Horizontal world-space gap between stacked icons. Each icon's localPosition gets
        // (stackIndex * IconStackSpacing) along +X relative to its AlertIconContainer.
        private const float IconStackSpacing = 0.6f;

        // Fixed display order for icon stacking. Stays stable so adjacent icons keep their
        // positions across a re-stack. Declaration order matches the bit declaration order in
        // AlertType.
        private static readonly AlertType[] DisplayOrder = new AlertType[]
        {
            AlertType.NeedsAttention,
            AlertType.Incomplete,
            AlertType.Locked,
            AlertType.Complete,
            AlertType.NotStarted,
        };

        public override unsafe void RegisterSystems(ref SystemRegistrationTable ecs)
        {
            ecs.Register(&ProcessWork,
                new SysUpdate(GameLoopPhase.LateUpdate, 5, UpdateMasks.OverarchingMask),
                new SysPermissions()
                    .ReadWriteShared<OverarchingAlertState>()
                    .ReadShared<MinigameSaveStates>()
                    .ReadWriteShared<OverarchingPools>()
                    .Read<MinigameZone>()
            );
        }

        static private void ProcessWork(float deltaTime)
        {
            OverarchingAlertState alertState = Find.State<OverarchingAlertState>();
            if (alertState == null) { return; }

            // (1) One-shot auto-rule. Wait until MinigameSaveStates is registered.
            if (!alertState.AutoRuleApplied)
            {
                MinigameSaveStates saveStates = Find.State<MinigameSaveStates>();
                if (saveStates == null) { return; }
                OverarchingAlertUtility.ApplyAutoRuleFromSaveStates(alertState, saveStates);
                alertState.AutoRuleApplied = true;
                alertState.AlertVisualsDirty = true;
            }

            // (2) Visual refresh. Only when something has changed.
            if (!alertState.AlertVisualsDirty) { return; }

            OverarchingPools pools = Find.State<OverarchingPools>();
            AlertIconDB iconDB = Find.GlobalAsset<AlertIconDB>();
            if (pools == null || iconDB == null) { return; }

            // Optional — drives the per-minigame inner tint. Absent DB just leaves the tint white.
            MinigameZoneOverlayDB overlayDB = Find.GlobalAsset<MinigameZoneOverlayDB>();

            FreeAllAlertIcons(pools);

            var zones = Find.Components<MinigameZone>();
            for (int z = 0; z < zones.Count; z++)
            {
                MinigameZone zone = zones[z];
                if (zone == null || zone.AlertIconContainer == null) { continue; }
                AlertType mask = OverarchingAlertUtility.GetMask(alertState, zone.Minigame);
                Color innerColor = MinigameZoneOverlayDBUtility.LookupZoneColor(overlayDB, zone.Minigame);
                SpawnIconsForMask(pools, iconDB, zone.AlertIconContainer, mask, innerColor);
            }

            alertState.AlertVisualsDirty = false;
        }

        // Returns every active alert icon to the pool. Called as the first step of each refresh
        // pass so the post-respawn ActiveAlertIcons list exactly mirrors the current masks.
        static private void FreeAllAlertIcons(OverarchingPools pools)
        {
            if (pools == null || pools.ActiveAlertIcons == null) { return; }
            int n = pools.ActiveAlertIcons.Count;
            for (int i = n - 1; i >= 0; i--)
            {
                AlertIconView icon = pools.ActiveAlertIcons[i];
                if (icon != null) { Pool.TryFree(icon); }
            }
            pools.ActiveAlertIcons.Clear();
        }

        // For a single zone's mask, allocs one icon per set bit (in DisplayOrder order),
        // parents it under the zone's container, sets the layered sprites (tinting the inner one
        // with the zone's minigame color), and positions it at the next horizontal stack slot.
        // Active icons are tracked on OverarchingPools.
        static private void SpawnIconsForMask(OverarchingPools pools, AlertIconDB iconDB, Transform container, AlertType mask, Color innerColor)
        {
            if (mask == AlertType.None) { return; }



            int stackIndex = 0;
            for (int b = 0; b < DisplayOrder.Length; b++)
            {
                AlertType bit = DisplayOrder[b];
                if ((mask & bit) == 0) { continue; }

                // locked overrides any other alerts
                if (((mask & AlertType.Locked)) != 0 && (bit != AlertType.Locked)) { continue; }

                AlertIconView icon = pools.AlertPool.Alloc();
                if (icon == null) { continue; }

                icon.transform.SetParent(container, worldPositionStays: false);
                icon.transform.localPosition = new Vector3(stackIndex * IconStackSpacing, 0f, 0f);
                icon.transform.localRotation = Quaternion.identity;
                icon.transform.localScale = Vector3.one;

                if (AlertIconDBUtility.TryLookupIcon(iconDB, bit, out Sprite baseSprite, out Sprite innerSprite, out Sprite symbolSprite))
                {
                    if (icon.BaseRenderer != null) { icon.BaseRenderer.sprite = baseSprite; }
                    if (icon.InnerRenderer != null)
                    {
                        icon.InnerRenderer.sprite = innerSprite;
                        icon.InnerRenderer.color = innerColor;
                    }
                    if (icon.SymbolRenderer != null) { icon.SymbolRenderer.sprite = symbolSprite; }
                }

                pools.ActiveAlertIcons.Add(icon);
                stackIndex++;
            }
        }
    }
}
