using System;
using System.Linq;
using BeauRoutine;
using BeauUtil.Debugger;
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
                       out ResearchPools pools,
                       out ResearchMinigameState researchState);

            if (interfacerState.LastUpdatedKind == ChamberSlotKind.Primary) {
                UpdateSemiconductor(interfacerState, dopingChamberState, researchState);
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

            bool substrateSlotted = ChamberInterfacerUtility.GetCurrent(interfacerState, ChamberSlotKind.Primary) != null;
            dopingChamberState.SampleHolder.SetActive(substrateSlotted);
            dopingChamberState.AtomicView.SetActive(substrateSlotted);
            dopingChamberState.SecondarySlotLid.SetActive(!substrateSlotted);
            ChamberInterfacerUtility.SetReceptive(interfacerState, ChamberSlotKind.Secondary, substrateSlotted);

            if (!substrateSlotted) return;
            UpdateAtomicView(interfacerState, dopingChamberState, researchState);
            ResearchUIAssets uiAssets = Find.GlobalAsset<ResearchUIAssets>();
            dopingChamberState.ElementToggle[0].Sprite.sprite = dopingChamberState.HostElementIndex == 0 ? uiAssets.ButtonDown : uiAssets.ButtonUp;
            dopingChamberState.ElementToggle[1].Sprite.sprite = dopingChamberState.HostElementIndex == 1 ? uiAssets.ButtonDown : uiAssets.ButtonUp;
        }

        private static void UpdateSemiconductor(ChamberInterfacerState interfacerState, DopingChamberState dopingChamber, ResearchMinigameState researchState)
        {
            MaterialAsset material = ChamberInterfacerUtility.GetCurrent(interfacerState, ChamberSlotKind.Primary);
            MaterialPhysicsProfile profile = material == null ? null : Find.NamedAsset<MaterialPhysicsProfile>(material.AssetId);

            // Clear dopant
            ResearchSlotUtility.FillInSlot(interfacerState, ChamberInterfacerUtility.GetSlot(interfacerState, ChamberSlotKind.Secondary), ChamberSlotKind.Secondary, null);

            if (material == null || profile == null) {
                CircuitUtility.SetLightStrength(dopingChamber.Circuit, 0f);
                CircuitUtility.SetFlowStrength(dopingChamber.Circuit, 0f);
                return;
            }

            // Show toggle for polyelemental substrates
            ResearchMaterialView view = Find.NamedAsset<ResearchMaterialView>(material.AssetId);
            dopingChamber.HostElementIndex = 0;
            bool isPolyelemental = material.AtomicRadii.Length > 1;
            dopingChamber.Toggle.SetActive(isPolyelemental);

            if (!isPolyelemental) {
                return;
            }

            // Set toggle labels
            bool known = researchState != null
                && researchState.SandboxProperties.TryGetValue(material.AssetId, out var record)
                && !MaterialPropertyRecordUtility.IsEmpty(record);
            dopingChamber.ElementToggleLabel[0].text = known ? material.ConstituentElementNames[0] : view.SampleLabel + "A";
            dopingChamber.ElementToggleLabel[1].text = known ? material.ConstituentElementNames[1] : view.SampleLabel + "B";
        }

        private static void UpdateDopant(ChamberInterfacerState interfacerState, DopingChamberState dopingChamber, ResearchExplosionState explosionState, ResearchPools vfxPool)
        {
            MaterialAsset substrate = ChamberInterfacerUtility.GetCurrent(interfacerState, ChamberSlotKind.Primary);
            if (substrate == null) {
                return;
            }

            MaterialAsset dopant = ChamberInterfacerUtility.GetCurrent(interfacerState, ChamberSlotKind.Secondary);
            MaterialPhysicsProfile profile = dopant == null ? null : Find.NamedAsset<MaterialPhysicsProfile>(dopant.AssetId);

            if (dopant == null || profile == null) {
                CircuitUtility.SetLightStrength(dopingChamber.Circuit, 0f);
                CircuitUtility.SetFlowStrength(dopingChamber.Circuit, 0f);

                return;
            }

            ResearchSlot slot = ChamberInterfacerUtility.GetSlot(interfacerState, ChamberSlotKind.Secondary);
            
            // Substrates must be semiconductors.
            if (Array.IndexOf(substrate.Properties, MaterialPropertyLabel.Semiconductor) < 0) {
                ResearchExplosionUtility.ExplodeSlot(
                explosionState, vfxPool, interfacerState, slot, ChamberSlotKind.Secondary,
                ExplosionStyle.TooBig, delay: 1f); // TODO: add explosion style if needed
                CircuitUtility.SetLightStrength(dopingChamber.Circuit, 0f);
                CircuitUtility.SetFlowStrength(dopingChamber.Circuit, 0f);
                dopingChamber.AtomicViewChangedThisFrame = true;

                return;
            }
            // Polyelemental materials cannot be used as dopants.
            if (dopant.ConstituentElementNames.Length > 1) {
                ResearchExplosionUtility.ExplodeSlot(
                explosionState, vfxPool, interfacerState, slot, ChamberSlotKind.Secondary,
                ExplosionStyle.TooBig, delay: 1f); // TODO: add explosion style if needed
                CircuitUtility.SetLightStrength(dopingChamber.Circuit, 0f);
                CircuitUtility.SetFlowStrength(dopingChamber.Circuit, 0f);
                dopingChamber.AtomicViewChangedThisFrame = true;

                return;
            }

            int index = dopingChamber.HostElementIndex;
            bool validRadius = substrate.AtomicRadii[index] > dopant.AtomicRadii[0];
            bool validElectronDiff = Mathf.Abs(substrate.ValenceElectronCounts[index] - dopant.ValenceElectronCounts[0]) == 1;

            if (validRadius && validElectronDiff) {
                // TODO: increased conduction multiplier
                float current = MaterialPhysicsUtility.GetCurrent(profile, dopingChamber.Voltage, dopingChamber.Temperature);
                if (current == 0) Sfx.Play(dopingChamber.NoCurrentSFX);
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

        private static void UpdateAtomicView(ChamberInterfacerState interfacer, DopingChamberState dopingChamber, ResearchMinigameState researchState)
        {
            ResearchAtomConfig config = Find.GlobalAsset<ResearchAtomConfig>();

            // Substrate atom
            MaterialAsset substrate = ChamberInterfacerUtility.GetCurrent(interfacer, ChamberSlotKind.Primary);
            if (substrate == null) return;
            ResearchMaterialView substrateView = Find.NamedAsset<ResearchMaterialView>(substrate.AssetId);

            int hostIndex = dopingChamber.HostElementIndex;
            bool substrateKnown = researchState != null
                && researchState.SandboxProperties.TryGetValue(substrate.AssetId, out var record)
                && !MaterialPropertyRecordUtility.IsEmpty(record);
            bool isPolyelemental = substrate.ConstituentElementNames.Length > 1;

            Assert.False(dopingChamber.SubstrateAtom == null);
            
            MaterialAtom substrateAtom = dopingChamber.SubstrateAtom;
            int cap = substrate.ValenceElectronCounts[hostIndex];

            for (int i = 0; i < substrateAtom.ElectronSprites.Length; i++) {
                substrateAtom.ElectronSprites[i].SetAlpha(i < cap ? 1f : 0f);
            }

            Debug.Log($"Substrate: {substrate} - {cap} electrons");

            // Dopant atom -- empty
            Assert.False(dopingChamber.DopantAtom == null);

            MaterialAsset dopant = ChamberInterfacerUtility.GetCurrent(interfacer, ChamberSlotKind.Secondary);
            MaterialAtom dopantAtom = dopingChamber.DopantAtom;
            if (dopant == null) {
                substrateAtom.Label.text = "?";
                dopantAtom.gameObject.SetActive(false);
                return;
            }

            // Dopant atom -- filled
            dopantAtom.gameObject.SetActive(true);
            substrateAtom.Label.text = "";

            ResearchMaterialView dopantView = Find.NamedAsset<ResearchMaterialView>(dopant.AssetId);
            int count = dopant.ValenceElectronCounts[0];
            int excess = count - cap;
            bool dopantKnown = researchState != null
                && researchState.SandboxProperties.TryGetValue(dopant.AssetId, out var dopantRecord)
                && !MaterialPropertyRecordUtility.IsEmpty(dopantRecord);

            dopantAtom.MaterialSprite.color = dopantView.AtomColor[0];
            dopantAtom.Label.text = dopantKnown ? dopant.ShortName : dopantView.SampleLabel;

            for (int i = 0; i < dopantAtom.ElectronSprites.Length; i++) {
                SpriteRenderer electron = dopantAtom.ElectronSprites[i];
                electron.color = dopantView.AtomColor[0];
                
                if (count <= cap) {
                    electron.SetAlpha(i < count ? 1f : 0f);
                } else {
                    bool front = i < cap;
                    bool back = i >= dopantAtom.ElectronSprites.Length - excess;
                    electron.SetAlpha(front || back ? 1f : 0f);
                }
            }

            Debug.Log($"Dopant: {dopant} - {count} electrons");
        }
    }
}
