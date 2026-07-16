using FieldDay;
using FieldDay.Systems;
using UnityEngine;

namespace SpaceFab.Research
{
    public class ChamberActivationSystem : SystemComponent
    {
        public override unsafe void RegisterSystems(ref SystemRegistrationTable ecs)
        {
            ecs.Register(&ProcessWork,
                new SysUpdate(GameLoopPhase.Update, 0, UpdateMasks.ResearchChamberMask),
                new SysPermissions()
                    .ReadShared<ChamberInterfacerState>()
                    .ReadShared<BatteryChamberState>()
                    .ReadShared<ThermalChamberState>()
                    .ReadShared<DopingChamberState>()
            );
        }

        static private void ProcessWork(float deltaTime)
        {

            ChamberInterfacerState interfacer = Find.State<ChamberInterfacerState>();
            if (!interfacer.ActiveChamberChangedThisFrame) { return; }
            
            Find.State(
                out BatteryChamberState batteryChamber,
                out ThermalChamberState thermalChamber,
                out DopingChamberState dopingChamber
            );

            batteryChamber.Root.SetActive(false);
            thermalChamber.Root.SetActive(false);
            dopingChamber.Root.SetActive(false);

            ActiveChamberKind activeChamber = ChamberInterfacerUtility.GetActiveChamber(interfacer);

            switch (activeChamber)
            {
                case ActiveChamberKind.Voltage:
                    BatteryChamberUtility.ResetState(batteryChamber);
                    break;
                case ActiveChamberKind.Thermal:
                    ThermalChamberUtility.ResetState(thermalChamber);
                    break;
                case ActiveChamberKind.Doping:
                    DopingChamberUtility.ResetState(dopingChamber);
                    break;
            }

            ChamberInterfacerUtility.SetReceptive(interfacer, ChamberSlotKind.Primary, activeChamber != ActiveChamberKind.None);
            ChamberInterfacerUtility.SetReceptive(interfacer, ChamberSlotKind.Secondary, false);
            interfacer.ActiveChamberChangedThisFrame = false;
        }
    }
}