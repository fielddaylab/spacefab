using BeauRoutine;
using FieldDay;
using FieldDay.Audio;
using FieldDay.Systems;
using SpaceFab;
using SpaceFab.Materials;
using UnityEngine;

namespace SpaceFab.Research
{
    /// <summary>
    /// Drives Doping chamber state every frame the chamber is active. Reads
    /// the active slots; computes variance via the
    /// material's MaterialPhysicsProfile; updates the CircuitRenderer's bulb
    /// strength and flow speed. Clears the slot on temperature-stability failure.
    /// Runs on Update at order 100 under ResearchChamberMask, after
    /// CircuitAnimationSystem (order 0) reads its previous state.
    /// </summary>
    public class DopingChamberSystem : SystemComponent
    {
        public override unsafe void RegisterSystems(ref SystemRegistrationTable ecs)
        {
            ecs.Register(&ProcessWork,
                new SysUpdate(GameLoopPhase.Update, 130, UpdateMasks.ResearchChamberMask),
                new SysPermissions()
                    .ReadShared<ChamberInterfacerState>()
                    .ReadWriteShared<DopingChamberState>()
                    .ReadShared<ResearchExplosionState>()
                    .ReadShared<ResearchPools>()
                    .ReadWrite<CircuitRenderer>()
                    .ReadWrite<ResearchSlot>()
            );
        }

        private static void ProcessWork(float deltaTime)
        {
            ChamberInterfacerState interfacerState = Find.State<ChamberInterfacerState>();
            if (ChamberInterfacerUtility.GetActiveChamber(interfacerState) != ActiveChamberKind.Doping)
                return;

            DopingChamberState dopingChamberState = Find.State<DopingChamberState>();
            
            if (!interfacerState.SlotMaterialUpdatedThisFrame && !dopingChamberState.AtomicViewChangedThisFrame)
                return;

            Find.State(out ResearchExplosionState explosionState,
                       out ResearchPools pools);

            if (interfacerState.LastUpdatedKind == ChamberSlotKind.Primary) {
                UpdateSemiconductor(interfacerState, dopingChamberState);
                foreach (var samplePanel in Find.Components<ResearchSamplePanel>()) {
                    ObservationPickerLoadUtility.LoadFor(samplePanel, pools, interfacerState, dopingChamberState.AvailableObservations);
                    break;
                }
            }
            else {
                UpdateDopant(interfacerState, dopingChamberState, explosionState, pools);
            }

            dopingChamberState.AtomicViewChangedThisFrame = false;

            if (dopingChamberState.SampleHolder == null) return;
            if (dopingChamberState.AtomicView == null) return;
            if (dopingChamberState.SecondarySlotLid == null) return;

            bool filled = ChamberInterfacerUtility.GetCurrent(interfacerState, ChamberSlotKind.Primary) != null;
            dopingChamberState.SampleHolder.SetActive(filled);
            dopingChamberState.AtomicView.SetActive(filled);
            dopingChamberState.SecondarySlotLid.SetActive(!filled);
            ChamberInterfacerUtility.SetReceptive(interfacerState, ChamberSlotKind.Secondary, filled);

            if (filled) UpdateAtomicView(dopingChamberState);
        }

        private static void UpdateSemiconductor(ChamberInterfacerState interfacerState, DopingChamberState dopingChamber)
        {
            MaterialAsset material = ChamberInterfacerUtility.GetCurrent(interfacerState, ChamberSlotKind.Primary);
            MaterialPhysicsProfile profile = material == null ? null : Find.NamedAsset<MaterialPhysicsProfile>(material.AssetId);

            if (material == null || profile == null) {
                CircuitUtility.SetLightStrength(dopingChamber.Circuit, 0f);
                CircuitUtility.SetFlowStrength(dopingChamber.Circuit, 0f);
                return;
            }

            // Clear dopant
            ResearchSlotUtility.FillInSlot(interfacerState, ChamberInterfacerUtility.GetSlot(interfacerState, ChamberSlotKind.Secondary), ChamberSlotKind.Secondary, null);
        }

        private static void UpdateDopant(ChamberInterfacerState interfacerState, DopingChamberState dopingChamber, ResearchExplosionState explosionState, ResearchPools vfxPool)
        {
            MaterialAsset semiconductor = ChamberInterfacerUtility.GetCurrent(interfacerState, ChamberSlotKind.Primary);
            if (semiconductor == null) {
                return;
            }

            MaterialAsset material = ChamberInterfacerUtility.GetCurrent(interfacerState, ChamberSlotKind.Secondary);
            MaterialPhysicsProfile profile = material == null ? null : Find.NamedAsset<MaterialPhysicsProfile>(material.AssetId);

            if (material == null || profile == null) {
                CircuitUtility.SetLightStrength(dopingChamber.Circuit, 0f);
                CircuitUtility.SetFlowStrength(dopingChamber.Circuit, 0f);

                return;
            }

            ResearchSlot slot = ChamberInterfacerUtility.GetSlot(interfacerState, ChamberSlotKind.Secondary);
            ResearchMaterialView view = Find.NamedAsset<ResearchMaterialView>(material.AssetId);
            
            // TODO: add a toggle for polyelemental materials
            if (material.AtomicRadii.Length > 1) {
                ResearchExplosionUtility.ExplodeSlot(
                explosionState, vfxPool, interfacerState, slot, ChamberSlotKind.Secondary,
                ExplosionStyle.TooBig, delay: 1f);
                CircuitUtility.SetLightStrength(dopingChamber.Circuit, 0f);
                CircuitUtility.SetFlowStrength(dopingChamber.Circuit, 0f);
                dopingChamber.AtomicViewChangedThisFrame = true;

                return;
            }

            // TODO: handle polyelemental semiconductors
            bool validRadius = semiconductor.AtomicRadii[0] > material.AtomicRadii[0];
            bool validElectronDiff = Mathf.Abs(semiconductor.ValenceElectronCounts[0] - material.ValenceElectronCounts[0]) == 1;

            if (validRadius && validElectronDiff) {
                // TODO: increased conduction multiplier
                float current = MaterialPhysicsUtility.GetCurrent(profile, dopingChamber.Voltage, dopingChamber.Temperature);
                if (current == 0) Sfx.Play(Find.State<BatteryChamberState>().NoCurrentSFX);
                CircuitUtility.SetLightStrength(dopingChamber.Circuit, current);
                CircuitUtility.SetFlowStrength(dopingChamber.Circuit, current);

                return;
            }

            ExplosionStyle explosionStyle = validRadius ? ExplosionStyle.InvalidCombo : ExplosionStyle.TooBig;
            ResearchExplosionUtility.ExplodeSlot(
                explosionState, vfxPool, interfacerState, slot, ChamberSlotKind.Secondary,
                explosionStyle, delay: 1f);
            CircuitUtility.SetLightStrength(dopingChamber.Circuit, 0f);
            CircuitUtility.SetFlowStrength(dopingChamber.Circuit, 0f);
            dopingChamber.AtomicViewChangedThisFrame = true;
        }

        private static void UpdateAtomicView(DopingChamberState dopingChamber)
        {
            ResearchAtomConfig config = Find.GlobalAsset<ResearchAtomConfig>();
            ChamberInterfacerState interfacer = Find.State<ChamberInterfacerState>();

            // Semiconductor atoms
            MaterialAsset semiconductor = ChamberInterfacerUtility.GetCurrent(interfacer, ChamberSlotKind.Primary);
            if (semiconductor == null) return;
            ResearchMaterialView semiconductorView = Find.NamedAsset<ResearchMaterialView>(semiconductor.AssetId);
            
            foreach (MaterialAtom atom in dopingChamber.SemiconductorAtomicViews) {
                atom.MaterialSprite.color = semiconductorView.GemColor;
                atom.Label.text = semiconductorView.SampleLabel;
            }

            // Dopant atom
            MaterialAsset dopant = ChamberInterfacerUtility.GetCurrent(interfacer, ChamberSlotKind.Secondary);
            bool hasDopant = dopant != null;
            ResearchMaterialView dopantView = hasDopant ? Find.NamedAsset<ResearchMaterialView>(dopant.AssetId) : null;
            
            SpriteRenderer MaterialSprite = dopingChamber.DopantAtomicView.MaterialSprite;
            // MaterialSprite.transform.SetScale(view.GemScale); // TODO
            MaterialSprite.sprite = hasDopant ? config.FilledSlotSprite : config.EmptySlotSprite;
            MaterialSprite.color = hasDopant ? dopantView.GemColor : config.ActiveSlotColor;
            dopingChamber.DopantAtomicView.Label.text = hasDopant ? dopantView.SampleLabel : "?";

            SpriteRenderer[] electrons = dopingChamber.DopantAtomicView.ElectronSprites;
            int count = hasDopant ? dopant.ValenceElectronCounts[0] : 0;
            // TODO: handle polyelemental semiconductors; a toggle switch will
            // determine which index will be used
            int cap = semiconductor.ValenceElectronCounts[0]; // TODO: handle polyelemental semiconductors

            for (int i = 0; i < electrons.Length; i++)
            {
                // sprite
                if (!hasDopant) {
                    electrons[i].sprite = i < cap ? config.EmptySlotSprite : config.FilledSlotSprite;
                }
                else if (count < cap) {
                    electrons[i].sprite = i < count ? config.FilledSlotSprite : config.EmptySlotSprite;
                }
                else {
                    electrons[i].sprite = config.FilledSlotSprite;
                }

                // color
                if (count >= cap && i >= cap && i < count) {
                    electrons[i].color = config.InvalidSlotColor;
                }
                else if (i < cap) {
                    electrons[i].color = config.ActiveSlotColor;
                }
                else {
                    electrons[i].color = config.DisabledSlotColor;
                }
            }

            Debug.Log($"Semiconductor: {semiconductor}\nDopant: {dopant}");
        }
    }
}
