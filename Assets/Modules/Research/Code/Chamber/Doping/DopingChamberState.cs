using FieldDay;
using FieldDay.Components;
using FieldDay.Audio;
using FieldDay.SharedState;
using SpaceFab.Materials;
using System;
using UnityEngine;
using BeauUtil;
using FieldDay.UI;
using TMPro;

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

        public MaterialAtom SubstrateAtom;
        public MaterialAtom DopantAtom;
        

        // Observation chips the player can add while this chamber is active.
        // Read by the chip-picker UI (Tier 4).
        public MaterialPropertyLabel[] AvailableObservations = new MaterialPropertyLabel[]
        {
            MaterialPropertyLabel.AtomicRadiusCompliant,
            MaterialPropertyLabel.ValenceOneLessThan,
            MaterialPropertyLabel.ValenceOneMoreThan
        };

        // Voltage and temperature are static for doping chamber
        public readonly float Voltage = 1f;
        public readonly float Temperature = 0f;

        // Toggle for polyelemental substrates
        public GameObject Toggle;
        public ResearchSpriteButton[] ElementToggle;
        public TMP_Text[] ElementToggleLabel;
        public int HostElementIndex;

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

            if (ElementToggle != null) {
                if (ElementToggle[0] != null) {
                    ElementToggle[0].Cursor.onClick.AddListener(() => HandleToggle(0));
                }
                if (ElementToggle[1] != null) {
                    ElementToggle[1].Cursor.onClick.AddListener(() => HandleToggle(1));
                }
            }

            NoCurrentWarningPlayed = false;
            Root.SetActive(false);
        }

        public void OnDeregister()
        {
        }

        public void HandleToggle(int index) {
            DopingChamberUtility.SetHostElementIndex(this, index);
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

            state.HostElementIndex = 0;
            state.AtomicViewChangedThisFrame = true;
            state.Root.SetActive(true);
        }

        public static void SetHostElementIndex(DopingChamberState state, int index)
        {
            if (state == null) {
                return;
            }
            if (state.HostElementIndex == index) {
                return;
            }

            state.HostElementIndex = index;
            state.AtomicViewChangedThisFrame = true;
        }
    }
}
