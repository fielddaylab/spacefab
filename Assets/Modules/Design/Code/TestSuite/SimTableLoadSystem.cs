using BeauUtil.Debugger;
using FieldDay;
using FieldDay.Systems;
using SpaceFab.Design.Visuals;
using SpaceFab.Save;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace SpaceFab.Design
{
    /// <summary>
    /// Suspends SetupMask when done.
    /// </summary>
    public class SimTableLoadSystem : SystemComponent
    {
        public override unsafe void RegisterSystems(ref SystemRegistrationTable ecs)
        {
            ecs.Register(&ProcessWork,
                new SysUpdate(GameLoopPhase.PreUpdate, 5, UpdateMasks.SetupMask),
                new SysPermissions()
                    .ReadWriteShared<DesignTransitionState>()
                    .ReadWriteShared<SimulateUIState>()
                    .ReadWriteShared<SimulateRunState>()
            );
        }

        // Advances the setup phase state machine one step per tick until setup is complete.
        static private void ProcessWork(float deltaTime)
        {
            Find.State(
                out DesignTransitionState transitionState,
                out SimulateUIState simUIState,
                out SimulateRunState simRunState,
                out PlayerProgressState progressState
                );

            ContractAssetsWrapper contractAssets = Find.NamedAsset<ContractAssetsWrapper>(progressState.ContractAssetsWrapperId);
            TestSuiteData suiteData = contractAssets.DesignLevelData.GetTestSuite();
            var suiteDB = Find.GlobalAsset<SuiteVisualsDB>();

            switch (transitionState.Phase)
            {
                case DesignTransitionPhase.BuildSimTable:
                    Debug.Log("[SimTableLoadSystem] Building Sim Table...");
                    // build Sim table
                    SimulateUIUtility.BuildTable(simUIState, suiteData, simRunState, suiteDB);
                    transitionState.Phase = DesignTransitionPhase.SetupComplete;
                    break;
                case DesignTransitionPhase.SetupComplete:
                    Debug.Log("[SimTableLoadSystem] Load Complete!");
                    // Setup is finished; stop running this system until the next minigame load
                    GameLoop.SuspendUpdates(UpdateMasks.SetupMask);
                    break;
                default:
                    break;
            }
        }
    }
}
