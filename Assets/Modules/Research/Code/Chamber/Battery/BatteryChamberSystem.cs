using FieldDay;
using FieldDay.Systems;
using SpaceFab;
using SpaceFab.Materials;
using UnityEngine;

namespace SpaceFab.Research
{
    /// <summary>
    /// Drives Battery chamber state every frame the chamber is active. Reads
    /// the active slot and the Battery's voltage; computes current via the
    /// material's MaterialPhysicsProfile; updates the CircuitRenderer's bulb
    /// strength and flow speed. Clears the slot on voltage-stability failure.
    /// Runs on Update at order 100 under ResearchChamberMask, after
    /// CircuitAnimationSystem (order 0) reads its previous state.
    /// </summary>
    public class BatteryChamberSystem : SystemComponent
    {
        public override unsafe void RegisterSystems(ref SystemRegistrationTable ecs)
        {
            ecs.Register(&ProcessWork,
                new SysUpdate(GameLoopPhase.Update, 100, UpdateMasks.ResearchChamberMask),
                new SysPermissions()
                    .ReadWriteShared<ChamberInterfacerState>()
                    .ReadWrite<BatteryChamberComponent>()
                    .ReadWrite<CircuitRenderer>()
                    .ReadWrite<ResearchSlot>()
            );
        }

        private static void ProcessWork(float deltaTime)
        {
            ChamberInterfacerState interfacerState = Find.State<ChamberInterfacerState>();
            if (ChamberInterfacerUtility.GetActiveChamber(interfacerState) != ActiveChamberKind.Battery)
            {
                return;
            }

            // Read once outside the per-Battery loop so the multi-Battery case
            // (unlikely today but cheap to support) doesn't re-poll the flag.
            bool slotChangedThisFrame = interfacerState.SlotMaterialUpdatedThisFrame;
            ChamberSlotKind changedKind = interfacerState.LastUpdatedKind;

            foreach (BatteryChamberComponent battery in Find.Components<BatteryChamberComponent>())
            {
                bool slotDirty = slotChangedThisFrame && changedKind == battery.SlotKind;
                bool dirty = battery.VoltageChangedThisFrame || slotDirty;

                if (dirty)
                {
                    UpdateBattery(interfacerState, battery);
                }

                battery.VoltageChangedThisFrame = false;
            }
        }

        // Single-Battery update: read material + voltage, run stability, drive
        // visuals. Splits out of ProcessWork so the loop body stays linear.
        private static void UpdateBattery(ChamberInterfacerState interfacerState, BatteryChamberComponent battery)
        {
            MaterialAsset material = ChamberInterfacerUtility.GetCurrent(interfacerState, battery.SlotKind);
            float voltage = battery.VoltageControl != null ? battery.VoltageControl.CurrentVoltage : 0f;

            if (material == null)
            {
                CircuitUtility.SetLightStrength(battery.Circuit, 0f);
                CircuitUtility.SetFlowSpeed(battery.Circuit, 0f);
                return;
            }

            MaterialPhysicsProfile profile = Find.NamedAsset<MaterialPhysicsProfile>(material.AssetId);
            if (profile == null)
            {
                Debug.LogWarningFormat("[BatteryChamberSystem] No MaterialPhysicsProfile registered for material '{0}'; treating as insulator.", material.name);
                CircuitUtility.SetLightStrength(battery.Circuit, 0f);
                CircuitUtility.SetFlowSpeed(battery.Circuit, 0f);
                return;
            }

            if (!MaterialPhysicsUtility.IsStableAtVoltage(profile, voltage))
            {
                Debug.LogFormat("[BatteryChamberSystem] '{0}' unstable at voltage {1:0.00}; clearing slot.", material.name, voltage);
                ResearchSlot slot = ChamberInterfacerUtility.GetSlot(interfacerState, battery.SlotKind);
                ResearchSlotUtility.FillInSlot(interfacerState, slot, battery.SlotKind, null);
                CircuitUtility.SetLightStrength(battery.Circuit, 0f);
                CircuitUtility.SetFlowSpeed(battery.Circuit, 0f);
                return;
            }

            float current = MaterialPhysicsUtility.GetCurrent(profile, voltage, battery.Temperature);
            CircuitUtility.SetLightStrength(battery.Circuit, current);
            CircuitUtility.SetFlowSpeed(battery.Circuit, current);
        }
    }
}
