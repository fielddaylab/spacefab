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
            ResearchPools pools = Find.State<ResearchPools>();

            switch (activeChamber)
            {
                case ActiveChamberKind.Voltage:
                    BatteryChamberUtility.ResetState(batteryChamber);
                    if (pools != null) {
                        foreach (var samplePanel in Find.Components<ResearchSamplePanel>()) {
                            ObservationPickerLoadUtility.LoadFor(samplePanel, pools, batteryChamber.AvailableObservations);
                            break;
                        }
                    }
                    break;
                case ActiveChamberKind.Thermal:
                    ThermalChamberUtility.ResetState(thermalChamber);
                    if (pools != null) {
                        foreach (var samplePanel in Find.Components<ResearchSamplePanel>()) {
                            ObservationPickerLoadUtility.LoadFor(samplePanel, pools, thermalChamber.AvailableObservations);
                            break;
                        }
                    }
                    break;
                case ActiveChamberKind.Doping:
                    DopingChamberUtility.ResetState(dopingChamber);
                    if (pools != null) {
                        foreach (var samplePanel in Find.Components<ResearchSamplePanel>()) {
                            ObservationPickerLoadUtility.LoadFor(samplePanel, pools, dopingChamber.AvailableObservations);
                            break;
                        }
                    }
                    break;
            }

            ChamberInterfacerUtility.SetReceptive(interfacer, ChamberSlotKind.Primary, activeChamber != ActiveChamberKind.None);
            ChamberInterfacerUtility.SetReceptive(interfacer, ChamberSlotKind.Secondary, false);
            interfacer.ActiveChamberChangedThisFrame = false;
        }
    }
}