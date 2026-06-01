using BeauUtil;
using FieldDay;
using FieldDay.Systems;
using SpaceFab.Save;

namespace SpaceFab.Design
{
    /// <summary>
    /// Handles the Design results-panel "Continue" hand-off. When the player clears a level, this
    /// either advances to the next level by reloading the Design scene (which re-imports and lands
    /// on the new first-unsolved level), or — on the contract's last level — confirms the normal
    /// exit back to overarching. Runs on Update under SimulateModeMask, after the results panel
    /// systems.
    /// </summary>
    public class DesignContinueSystem : SystemComponent
    {
        public override unsafe void RegisterSystems(ref SystemRegistrationTable ecs)
        {
            ecs.Register(&ProcessWork,
                new SysUpdate(GameLoopPhase.Update, 6, UpdateMasks.SimulateModeMask),
                new SysPermissions()
                    .ReadWriteShared<ResultState>()
                    .ReadWriteShared<MinigameRequestExitState>()
                    .ReadWriteShared<MinigameLoadExitState>()
                    .ReadShared<MinigameStateInterfacer>()
                    .ReadShared<DesignMinigameState>()
                    .ReadShared<MinigameSaveStates>()
            );
        }

        // Consumes ResultState.ContinueRequested and routes to next-level reload or exit.
        static private void ProcessWork(float deltaTime)
        {
            Find.State(
                out ResultState resultState,
                out MinigameRequestExitState requestExitState,
                out MinigameLoadExitState loadExitState,
                out MinigameStateInterfacer interfacer
                );
            Find.State(
                out DesignMinigameState designState,
                out MinigameSaveStates saveStates
                );

            if (!resultState.ContinueRequested) { return; }
            resultState.ContinueRequested = false;

            int activeIdx = designState.ActiveLevelIndex;
            int levelCount = saveStates.Design.LevelCount;

            // Last level (or a degenerate single/zero-level contract): return to overarching, where
            // the now-true aggregate FoundValidSolution lets the contract complete. Same path the
            // results panel used before multi-level contracts existed.
            if (activeIdx >= levelCount - 1)
            {
                requestExitState.ExitRequestState = RequestState.Confirmed;
                return;
            }

            // More levels remain: reload the Design scene for the next level. Resolve the scene from
            // the global lookup by the active minigame's id, point the exit pipeline at it, and kick
            // the pipeline. The Exiting phase exports + saves (persisting this level's solved flag)
            // before the reload, and the fresh load re-imports onto the next first-unsolved level.
            MinigameSceneLookup sceneLookup = Find.GlobalAsset<MinigameSceneLookup>();
            if (sceneLookup != null && sceneLookup.TryGetScene(interfacer.Id, out SceneReference designScene))
            {
                loadExitState.HasReloadTarget = true;
                loadExitState.ReloadTarget = designScene;
            }

            // Reuse the exit pipeline: flip to Exiting and swap to the transition mask, exactly as
            // MinigameRequestExitSystem does on a confirmed exit.
            loadExitState.Phase = MinigameLoadExitPhase.Exiting;
            GameLoop.SuspendUpdates(Bits.All32);
            GameLoop.ResumeUpdates(UpdateMasks.MinigameTransitionMask);
        }
    }
}
