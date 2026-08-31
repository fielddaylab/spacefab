using BeauUtil;
using FieldDay;
using Leaf.Runtime;
using SpaceFab.UI;

namespace SpaceFab.Research {
    /// <summary>
    /// Leaf-callable queries and commands specific to the Research minigame.
    /// </summary>
    public static class ResearchScripting {
        // Opens the shared wiki to the material page of the last sample to have a property newly
        // confirmed this session. No-op when no research session is active, nothing new has been
        // discovered yet, no wiki is present, or that material has no authored material page.
        [LeafMember("OpenWikiToLastDiscovery")]
        public static void Leaf_OpenWikiToLastDiscovery() {
            if (!Game.SharedState.Has<ResearchMinigameState>()) { return; }
            ResearchMinigameState researchState = Find.State<ResearchMinigameState>();
            if (!researchState.LastDiscovery.IsValid) { return; }

            var contents = Find.Components<WikiContent>();
            if (contents.Count == 0) { return; }

            if (!WikiUtility.TryFindMaterialPage(contents[0], researchState.LastDiscovery.MaterialId, out StringHash32 tabId, out StringHash32 pageId)) {
                return;
            }

            WikiUtility.OpenTo(tabId, pageId);
        }

        // Resets the currently-active research chamber to its default state (e.g. the Battery's
        // voltage dial back to its initial setting). No-op when no chamber interfacer is present
        // or no chamber is active. Each chamber kind routes to its own ResetState below.
        [LeafMember("ResetCurrentChamber")]
        public static void Leaf_ResetCurrentChamber() {
            if (!Game.SharedState.Has<ChamberInterfacerState>()) { return; }

            switch (Find.State<ChamberInterfacerState>().ActiveChamber) {
                case ActiveChamberKind.Voltage:
                    if (Game.SharedState.Has<BatteryChamberState>()) {
                        BatteryChamberUtility.ResetState(Find.State<BatteryChamberState>());
                    }
                    break;
                case ActiveChamberKind.Thermal:
                    if (Game.SharedState.Has<ThermalChamberState>()) {
                        ThermalChamberUtility.ResetState(Find.State<ThermalChamberState>());
                    }
                    break;
            }
        }
    }
}