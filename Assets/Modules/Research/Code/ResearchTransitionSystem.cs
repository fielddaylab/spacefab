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
                    .ReadWriteShared<ResearchSampleTrayState>()
                    .ReadWriteShared<ChamberInterfacerState>()
                    .ReadWriteShared<BatteryChamberState>()
                    .ReadWriteShared<ResearchHypothesisPagesState>()
                    .ReadWriteShared<HypothesisViewModelState>()
            );
        }

        static private void ProcessWork(float deltaTime) {
            Find.State(
                out ChapterState chapterState,
                out PlayerProgressState playerProgress,
                out ResearchMinigameState researchState,
                out ResearchSampleTrayState trayState
            );
            Find.State(
                out ChamberInterfacerState interfacerState,
                out BatteryChamberState batteryChamberState
                );
            Find.State(
                out ResearchHypothesisPagesState hypothesisPagesState,
                out HypothesisViewModelState hypothesisViewModelState
                );

            researchState.AvailableMaterials.Clear();
            if (chapterState.CurrChapterDef != null) {
                foreach (var id in chapterState.CurrChapterDef.AvailableMaterials()) {
                    researchState.AvailableMaterials.Add(id);
                }
            }

            //researchState.RequiredResearchGoals.Clear();
            if (Game.Assets.HasNamed<ContractAssetsWrapper>(playerProgress.ContractAssetsWrapperId))
            {
                ContractAssetsWrapper contractAssets = Find.NamedAsset<ContractAssetsWrapper>(playerProgress.ContractAssetsWrapperId);
                ContractDef contractDef = contractAssets.ContractDef;
                if (contractDef != null)
                {
                    researchState.RequiredResearchGoals = contractDef.RequiredMaterialProperties();
                }
            }

            // Seed the sandbox with previously-confirmed properties for the
            // materials in this chapter's scope. Must run after AvailableMaterials
            // is populated (above). Mid-session
            // resume goes through ResearchStateUtility.ImportState instead and
            // bypasses this load.
            ResearchStateUtility.LoadFromPlayerProgress(researchState, playerProgress);

            // Spawn the tray's draggable samples for this chapter's available
            // materials. Idempotent on re-entry — the utility clears any
            // previously-spawned gems before refilling.
            ResearchSampleTrayUtility.SpawnTray(trayState, researchState);

            // Build the hypothesis page list for the contract's required
            // research goals. One page per (goal × registered definition).
            // Resets the viewmodel's active page index to 0.
            ResearchHypothesisUtility.BuildPages(researchState, hypothesisPagesState, hypothesisViewModelState);

            // Init Battery Chamber
            ResearchVoltageConfig config = Find.GlobalAsset<ResearchVoltageConfig>();
            if (config != null)
            {
                batteryChamberState.VoltageControl.VoltageIndex = config.DefaultIndex;
                VoltageUtility.RefreshVisualState(batteryChamberState.VoltageControl, config);
            }

            // Activate the Battery chamber. This is the only chamber today;
            // when the station-transition system lands, this hardcoded
            // activation moves into station logic and reacts to player nav.
            // TODO: clear ActiveChamber + receptive flags on minigame exit.
            ChamberInterfacerUtility.SetActiveChamber(interfacerState, ActiveChamberKind.Battery);
            ChamberInterfacerUtility.SetReceptive(interfacerState, ChamberSlotKind.Primary, true);

            GameLoop.SuspendUpdates(UpdateMasks.SetupMask);
            GameLoop.ResumeUpdates(UpdateMasks.ResearchMask | UpdateMasks.ResearchChamberMask);
        }
    }
}
