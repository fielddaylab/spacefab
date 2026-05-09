using BeauUtil;
using FieldDay;
using FieldDay.Assets;
using FieldDay.Systems;
using SpaceFab;
using UnityEngine;

namespace SpaceFab.Research {
    /// <summary>
    /// Handles post-load setup for the Research minigame: populates the available-materials
    /// and required-research-materials sets from ChapterDef and ContractDef, then
    /// transitions from SetupMask to ResearchMask.
    /// Runs on PreUpdate phase at order 0 under SetupMask.
    /// </summary>
    public class ResearchTransitionSystem : SystemComponent {
        public override unsafe void RegisterSystems(ref SystemRegistrationTable ecs) {
            ecs.Register(&ProcessWork,
                new SysUpdate(GameLoopPhase.PreUpdate, 0, UpdateMasks.SetupMask),
                new SysPermissions()
                    .ReadShared<ChapterState>()
                    .ReadShared<PlayerProgressState>()
                    .ReadWriteShared<ResearchMinigameState>()
            );
        }

        static private void ProcessWork(float deltaTime) {
            Find.State(
                out ChapterState chapterState,
                out PlayerProgressState playerProgress,
                out ResearchMinigameState researchState
            );

            researchState.AvailableMaterials.Clear();
            if (chapterState.CurrChapterDef != null) {
                foreach (var id in chapterState.CurrChapterDef.AvailableMaterials()) {
                    researchState.AvailableMaterials.Add(id);
                }
            }

            //researchState.RequiredResearchGoals.Clear();
            ContractAssetsWrapper contractAssets = Find.NamedAsset<ContractAssetsWrapper>(playerProgress.ContractAssetsWrapperId);
            ContractDef contractDef = contractAssets.ContractDef;
            if (contractDef != null) {
                researchState.RequiredResearchGoals = contractDef.RequiredMaterialProperties();
            }

            // Seed the sandbox with previously-confirmed properties for the
            // materials in this chapter's scope. Must run after AvailableMaterials
            // is populated (above). Mid-session
            // resume goes through ResearchStateUtility.ImportState instead and
            // bypasses this load.
            ResearchStateUtility.LoadFromPlayerProgress(researchState, playerProgress);

            GameLoop.SuspendUpdates(UpdateMasks.SetupMask);
            GameLoop.ResumeUpdates(UpdateMasks.ResearchMask);
        }
    }
}
