using BeauUtil;
using FieldDay;
using FieldDay.Assets;
using FieldDay.Music;
using FieldDay.Scripting;
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
                    .ReadWriteShared<ThermalChamberState>()
                    .ReadWriteShared<ResearchHypothesisPagesState>()
                    .ReadWriteShared<HypothesisViewModelState>()
                    .ReadWriteShared<ResearchPools>()
                    .ReadWrite<ResearchSamplePanel>()
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
                out BatteryChamberState batteryChamberState,
                out ThermalChamberState thermalChamberState
                );
            Find.State(
                out ResearchHypothesisPagesState hypothesisPagesState,
                out HypothesisViewModelState hypothesisViewModelState
                );

            researchState.AvailableMaterials.Clear();
            if (chapterState.CurrChapterDef != null) {
                StringHash32[] excluded = chapterState.CurrChapterDef.ExcludeFromResearch();
                foreach (var id in chapterState.CurrChapterDef.AvailableMaterials()) {
                    if (IsExcluded(excluded, id)) continue;
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

            // Init Battery Chamber. Instantiate the meter rig variant for
            // this save's unlock state under BatteryContainer, then prime
            // the dial: assigning Battery first lets RefreshVisualState
            // populate the freshly-spawned slots in the same pass.
            ResearchVoltageConfig voltageConfig = Find.GlobalAsset<ResearchVoltageConfig>();
            if (voltageConfig != null && batteryChamberState.BatteryContainer != null && batteryChamberState.Battery == null)
            {
                GameObject meterPrefab = playerProgress.BigBatteryUnlocked ? voltageConfig.BigBatteryMeterPrefab : voltageConfig.SmallBatteryMeterPrefab;
                if (meterPrefab != null)
                {
                    GameObject meterInstance = UnityEngine.Object.Instantiate(meterPrefab, batteryChamberState.BatteryContainer, false);
                    batteryChamberState.Battery = meterInstance.GetComponent<ChamberBattery>();
                }

                if (batteryChamberState.VoltageControl != null)
                {
                    batteryChamberState.VoltageControl.VoltageIndex = voltageConfig.DefaultIndex;
                    VoltageUtility.RefreshVisualState(batteryChamberState.VoltageControl, voltageConfig);
                }
            }

            // Init Thermal Chamber.
            ResearchHeatConfig heatConfig = Find.GlobalAsset<ResearchHeatConfig>();
            if (heatConfig != null && thermalChamberState.HeatControl != null)
            {
                thermalChamberState.HeatControl.HeatIndex = heatConfig.DefaultIndex;
                HeatUtility.RefreshVisualState(thermalChamberState.HeatControl, heatConfig);
            }

            // Load the observation picker chip set for the active
            // chamber. Available observations are constant per chamber,
            // so this is a one-shot sync — pool alloc + layout + overlay
            // resize. Per-chip disabled state is refreshed reactively by
            // ObservationPickerRefreshSystem. When the station-transition
            // system lands and the active chamber goes dynamic, this
            // call moves alongside the SetActiveChamber switch.
            ResearchPools pools = Find.State<ResearchPools>();
            if (pools != null)
            {
                foreach (var samplePanel in Find.Components<ResearchSamplePanel>())
                {
                    ObservationPickerLoadUtility.LoadFor(samplePanel, pools, batteryChamberState.AvailableObservations);
                    break;
                }
            }

            // Activate the Battery chamber. This is the only chamber today;
            // when the station-transition system lands, this hardcoded
            // activation moves into station logic and reacts to player nav.
            // TODO: clear ActiveChamber + receptive flags on minigame exit.
            ChamberInterfacerUtility.SetActiveChamber(interfacerState, ActiveChamberKind.Voltage);
            ChamberInterfacerUtility.SetReceptive(interfacerState, ChamberSlotKind.Primary, true);

            GameLoop.SuspendUpdates(UpdateMasks.SetupMask);
            GameLoop.ResumeUpdates(UpdateMasks.ResearchMask | UpdateMasks.ResearchChamberMask);

            using (var table = TempVarTable.Alloc()) {
                table.Set("minigame", "research");
                ScriptUtility.Trigger(ResearchScriptTriggers.OnSetupComplete, table);
            }
        }

        // Linear membership check against ChapterDef.ExcludeFromResearch.
        // The list is small (handful of ids per chapter) so a hash-set
        // build is not worth the allocation.
        private static bool IsExcluded(StringHash32[] excluded, StringHash32 id) {
            if (excluded == null) return false;
            for (int i = 0; i < excluded.Length; i++) {
                if (excluded[i] == id) return true;
            }
            return false;
        }
    }
}
