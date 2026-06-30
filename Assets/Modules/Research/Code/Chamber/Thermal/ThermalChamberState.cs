using FieldDay;
using FieldDay.Components;
using FieldDay.Audio;
using FieldDay.SharedState;
using SpaceFab.Materials;
using System;
using UnityEngine;
using BeauUtil;

namespace SpaceFab.Research
{
    public class ThermalChamberState : SharedStateComponent, IRegistrationCallbacks
    {
        // Which slot kind on ChamberInterfacerState this Battery reads.
        // Battery is single-slot; defaults to Primary.
        public ChamberSlotKind SlotKind = ChamberSlotKind.Primary;

        public CircuitRenderer Circuit;
        public HeatControl HeatControl;

        public ChamberBattery Battery;
        public GameObject SampleHolder;

        // Observation chips the player can add while this chamber is active.
        // Read by the chip-picker UI (Tier 4).
        public MaterialPropertyLabel[] AvailableObservations = new MaterialPropertyLabel[]
        {
            MaterialPropertyLabel.HeatActivated,
            MaterialPropertyLabel.HeatDeactivated,
            MaterialPropertyLabel.HeatUnaffected,
            MaterialPropertyLabel.HeatVulnerable,
            MaterialPropertyLabel.HeatResistant
        };
        [Range(0f, 1f)] public float Temperature = 0f;
        [NonSerialized] public bool HeatChangedThisFrame;

        // Sound played when no current
        [AudioEvent] public StringHash32 NoCurrentSFX;

        // Track whether warning sound played
        [NonSerialized] public bool NoCurrentWarningPlayed;

        public void OnRegister()
        {
            // Hide the sample holder until the player drops a material into
            // the primary slot. BatteryChamberSystem.UpdateBattery toggles
            // it on the slot-change frame.
            if (SampleHolder != null)
            {
                SampleHolder.SetActive(false);
            }
            NoCurrentWarningPlayed = false;
        }

        public void OnDeregister()
        {
        }
    }

    public static class ThermalChamberUtility
    {
        public static void ResetState(BatteryChamberState state)
        {
            if (state == null)
            {
                return;
            }
            VoltageUtility.Reset(state.VoltageControl, Find.GlobalAsset<ResearchVoltageConfig>());
        }
    }
}
