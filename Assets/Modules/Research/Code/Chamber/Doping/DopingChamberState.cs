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
    public class DopingChamberState : SharedStateComponent, IRegistrationCallbacks
    {
        public CircuitRenderer Circuit;
        public GameObject SampleHolder;

        // Root of doping chamber's GameObject hierarchy; used to toggle visibility on activation/deactivation.
        public GameObject Root;

        // Root of atomic view ui
        public GameObject AtomicView;
        public GameObject SecondarySlotLid;

        public MaterialAtom[] SemiconductorAtomicViews;
        public MaterialAtom DopantAtomicView;

        // Observation chips the player can add while this chamber is active.
        // Read by the chip-picker UI (Tier 4).
        public MaterialPropertyLabel[] AvailableObservations = new MaterialPropertyLabel[]
        {
            MaterialPropertyLabel.AtomicRadiusCompliant,
            MaterialPropertyLabel.ValenceOneLessThan,
            MaterialPropertyLabel.ValenceOneMoreThan
        };

        public float Voltage = 1f;
        public float Temperature = 0f;
        [NonSerialized] public bool AtomicViewChangedThisFrame;

        // Sound played when no current
        [AudioEvent] public StringHash32 NoCurrentSFX;

        // Track whether warning sound played
        [NonSerialized] public bool NoCurrentWarningPlayed;

        public void OnRegister()
        {
            // Hide the atomic view and the secondary slot until the player
            // drops a material into the primary slot.
            if (SampleHolder != null)
            {
                SampleHolder.SetActive(false);
            }
            NoCurrentWarningPlayed = false;
            Root.SetActive(false);
        }

        public void OnDeregister()
        {
        }
    }

    public static class DopingChamberUtility
    {
        public static void ResetState(DopingChamberState state)
        {
            if (state == null)
            {
                return;
            }

            state.AtomicViewChangedThisFrame = true;
            state.Root.SetActive(true);
        }
    }
}
