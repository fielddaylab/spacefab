using FieldDay.Components;
using FieldDay.SharedState;
using SpaceFab.Materials;
using System;
using UnityEngine;

namespace SpaceFab.Research
{
    /// <summary>
    /// Battery state.
    /// Holds scene-authored references (the slot kind it consumes, its
    /// circuit renderer, its voltage control), the static observation set
    /// the chip-picker UI will surface when this chamber is active, and a
    /// per-frame "voltage changed" flag set by VoltageUtility and consumed
    /// by BatteryChamberSystem.
    /// </summary>
    public class BatteryChamberState : SharedStateComponent
    {
        // Which slot kind on ChamberInterfacerState this Battery reads.
        // Battery is single-slot; defaults to Primary.
        public ChamberSlotKind SlotKind = ChamberSlotKind.Primary;

        public CircuitRenderer Circuit;
        public VoltageControl VoltageControl;

        // Observation chips the player can add while this chamber is active.
        // Read by the chip-picker UI (Tier 4).
        public MaterialPropertyLabel[] AvailableObservations = new MaterialPropertyLabel[]
        {
            MaterialPropertyLabel.Conductive,
            MaterialPropertyLabel.NonConductive,
            MaterialPropertyLabel.VoltageResistant,
            MaterialPropertyLabel.HighMobility,
        };

        // Battery's fixed temperature. Battery doesn't expose temperature
        // control to the player; the Thermal chamber does.
        [Range(0f, 1f)] public float Temperature = 0f;

        // Set by VoltageUtility on a button press; consumed and cleared by
        // BatteryChamberSystem.
        [NonSerialized] public bool VoltageChangedThisFrame;
    }
}
