using System.Text;
using BeauRoutine;
using BeauUtil;
using BeauUtil.Debugger;
using FieldDay;
using FieldDay.Debugging;
using FieldDay.Scripting;

namespace SpaceFab.Overarching
{
    /// <summary>
    /// Development-only debug menu for jumping to game states: setting the active contract and force-entering
    /// a minigame. Registered via [DebugMenuFactory] (auto-discovered at boot; compiled out of release builds
    /// since the attribute is Conditional). Both tools depend on Overarching state and no-op gracefully when
    /// it isn't present.
    ///
    /// NOTE: DMMenuUI builds its element views (PopulateMenu) BEFORE firing a menu's OnEnter, so mutating a
    /// DMInfo's Elements at runtime desyncs the views and crashes UpdateElements. All elements here are
    /// therefore built once at boot; dynamic content is expressed via live text getters and predicate-gated
    /// buttons (both re-evaluated every frame), never by rebuilding the element list.
    /// </summary>
    public static class OverarchingDebugMenu
    {
        // Fixed pool of contract slot buttons. Out-of-range slots are disabled by their predicate. A chapter
        // is expected to have far fewer than this many available contracts.
        private const int MaxContractSlots = 8;

        // Handle to the in-flight contract-apply routine. The jump/set buttons stay disabled while it runs so
        // a minigame jump can't fire before the contract (and the minigame save it seeds) is fully applied.
        private static Routine s_ContractApplyRoutine;

        [DebugMenuFactory]
        private static DMInfo CreateJumpsDebugMenu()
        {
            DMInfo menu = new DMInfo("Jumps", 2);
            menu.AddSubmenu(BuildSetContractMenu());
            menu.AddSubmenu(BuildEnterMinigameMenu());
            return menu;
        }

        // True while a scene load or an Overarching->minigame transition is underway. The jump/set buttons
        // disable themselves during this window so a debug action can't race an in-flight transition.
        private static bool IsTransitioning()
        {
            if (s_ContractApplyRoutine.Exists())
            {
                return true;
            }
            if (Game.Scenes.IsLoadingAnyScene() || Game.Scenes.IsMainLoading())
            {
                return true;
            }
            if (Game.SharedState.Has<OverarchingToMinigameSequenceState>()
                && Find.State<OverarchingToMinigameSequenceState>().Phase != OverarchingToMinigamePhase.Waiting)
            {
                return true;
            }
            return false;
        }

        // ---- Set Contract ----

        private static DMInfo BuildSetContractMenu()
        {
            DMInfo menu = new DMInfo("Set Contract", MaxContractSlots + 1);

            // Live listing of the available contracts (index: title), refreshed every frame.
            menu.AddText("Contracts", AppendContractList);

            // Fixed slot buttons; the predicate enables only the slots that map to a real contract.
            for (int i = 0; i < MaxContractSlots; i++)
            {
                int index = i; // capture per-iteration for the closures
                menu.AddButton("Set contract #" + i, () => DebugSetContract(index), () => index < AvailableContractCount() && !IsTransitioning());
            }

            // Opening the menu loads the chapter's available-contracts bundle if it isn't already (safe: no
            // element mutation), so the slots populate even from outside the contract-select screen.
            menu.OnEnter.Register(EnsureContractsLoaded);
            return menu;
        }

        // Loads the current chapter's available-contracts bundle on menu open if it isn't loaded yet.
        private static void EnsureContractsLoaded(DMInfo menu)
        {
            if (!Game.SharedState.Has<ChapterState>() || !Game.SharedState.Has<AvailableContractsLookup>())
            {
                return;
            }

            ChapterState chapterState = Find.State<ChapterState>();
            if (chapterState.CurrAvailableContractsBundle != null)
            {
                return;
            }

            AvailableContractsLookup lookup = Find.State<AvailableContractsLookup>();
            int chapterIndex = chapterState.CurrChapterIndex;
            if (lookup.Entries == null || chapterIndex < 0 || chapterIndex >= lookup.Entries.Length)
            {
                return;
            }

            // Host the routine on the lookup component so BeauRoutine stops it if that scene unloads (e.g. a
            // minigame jump mid-load) instead of letting a hostless routine run on into destroyed state.
            Routine.Start(lookup, ContractsLookupUtility.LoadAvailableContractsAtChapter(lookup, chapterState, chapterIndex));
        }

        private static void AppendContractList(StringBuilder sb)
        {
            ContractsBundle bundle = ResolveContractsBundle();
            if (bundle == null || bundle.AvailableContracts == null || bundle.AvailableContracts.Length == 0)
            {
                sb.Append("(no contracts loaded)");
                return;
            }

            ContractDef[] contracts = bundle.AvailableContracts;
            for (int i = 0; i < contracts.Length; i++)
            {
                if (i > 0) { sb.Append('\n'); }
                string title = string.IsNullOrEmpty(contracts[i].Title()) ? ("Contract " + i) : contracts[i].Title();
                sb.Append(i).Append(": ").Append(title);
            }
        }

        private static int AvailableContractCount()
        {
            ContractsBundle bundle = ResolveContractsBundle();
            return (bundle != null && bundle.AvailableContracts != null) ? bundle.AvailableContracts.Length : 0;
        }

        private static ContractsBundle ResolveContractsBundle()
        {
            if (!Game.SharedState.Has<ChapterState>())
            {
                return null;
            }
            return Find.State<ChapterState>().CurrAvailableContractsBundle;
        }

        // Applies the chosen contract as the active one via the shared data path (no selection UI). Runs the
        // confirm data core as a routine because it waits on the contract-assets scene load.
        private static void DebugSetContract(int index)
        {
            if (!Game.SharedState.Has<ChapterState>() || !Game.SharedState.Has<PlayerProgressState>() || !Game.SharedState.Has<ContractAssetsLookup>())
            {
                Log.Warn("[OverarchingDebugMenu] Set Contract unavailable: required states not present");
                return;
            }

            if (IsTransitioning())
            {
                Log.Warn("[OverarchingDebugMenu] Set Contract ignored: a scene load or transition is in progress");
                return;
            }

            ChapterState chapterState = Find.State<ChapterState>();
            ContractsBundle bundle = chapterState.CurrAvailableContractsBundle;
            if (bundle == null || bundle.AvailableContracts == null || index < 0 || index >= bundle.AvailableContracts.Length)
            {
                Log.Warn("[OverarchingDebugMenu] Set Contract: index {0} out of range or contracts not loaded", index);
                return;
            }

            PlayerProgressState playerProgress = Find.State<PlayerProgressState>();
            ContractAssetsLookup lookup = Find.State<ContractAssetsLookup>();
            StringHash32 contractId = bundle.AvailableContracts[index].AssetId;

            ContractsLookupUtility.ConstructMap(lookup);
            if (lookup.Map == null || !lookup.Map.ContainsKey(contractId))
            {
                Log.Warn("[OverarchingDebugMenu] Set Contract: no asset-lookup entry for '{0}'", contractId.ToDebugString());
                return;
            }

            // Host on the lookup so the routine is stopped if its scene unloads mid-flight, and keep the
            // handle so IsTransitioning() can hold off a minigame jump until the contract is fully applied.
            s_ContractApplyRoutine.Replace(lookup, ContractConfirmUtility.ApplyContractByIndex(chapterState, playerProgress, lookup, index));
            Log.Msg("[OverarchingDebugMenu] Set contract to index {0} ('{1}')", index, bundle.AvailableContracts[index].Title());
        }

        // ---- Enter Minigame ----

        private static DMInfo BuildEnterMinigameMenu()
        {
            DMInfo menu = new DMInfo("Enter Minigame", (int) MinigameId.COUNT);
            for (int i = 0; i < (int) MinigameId.COUNT; i++)
            {
                MinigameId id = (MinigameId) i; // capture per-iteration for the closure
                menu.AddButton(id.ToString(), () => DebugEnterMinigame(id), () => !IsTransitioning());
            }
            return menu;
        }

        // Force-enters the given minigame from the Overarching hub, bypassing the zone lock (the lock is only
        // enforced in the UI click handler; ConfirmEnterMinigame itself does not check it). No-ops elsewhere.
        private static void DebugEnterMinigame(MinigameId id)
        {
            if (!Game.SharedState.Has<MinigameZonesState>())
            {
                Log.Warn("[OverarchingDebugMenu] Enter Minigame only works from the Overarching hub");
                return;
            }

            if (IsTransitioning())
            {
                Log.Warn("[OverarchingDebugMenu] Enter Minigame ignored: a scene load or transition is in progress");
                return;
            }

            MinigameZonesState zonesState = Find.State<MinigameZonesState>();
            for (int i = 0; i < zonesState.Zones.Length; i++)
            {
                if (zonesState.Zones[i].Minigame == id)
                {
                    // Stop any running script threads (e.g. a dialogue line) before unloading the hub. They
                    // are hosted on the persistent GameLoop.Host, so they'd otherwise keep ticking after the
                    // scene unloads and touch the destroyed dialogue printer (ScriptPlugin line completion),
                    // which throws. Killing here halts them cleanly while their printer is still valid.
                    ScriptUtility.KillAllThreads();
                    MinigameZonesUtility.ClickZone(zonesState, i);
                    Log.Msg("[OverarchingDebugMenu] Force-entering minigame {0} (zone {1})", id, i);
                    return;
                }
            }

            Log.Warn("[OverarchingDebugMenu] No zone found for minigame {0}", id);
        }
    }
}
