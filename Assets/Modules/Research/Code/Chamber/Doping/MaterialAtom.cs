using FieldDay.Components;
using UnityEngine;
using TMPro;
using SpaceFab.Materials;
using FieldDay;
using BeauUtil.Debugger;
using BeauRoutine;

namespace SpaceFab.Research
{
    public class MaterialAtom : BatchedComponent
    {
        public SpriteRenderer MaterialSprite;
        public SpriteRenderer[] ElectronSprites;
        public TMP_Text Label;
    }

    public static class MaterialAtomicViewUtility {
        public static void RenderMaterialAtom(MaterialAtom atom, MaterialAsset material, ResearchMinigameState researchState, int elementIndex = 0) {
            Assert.False(atom == null);
            Assert.False(atom.ElectronSprites == null);
            
            bool known = researchState != null
                    && researchState.SandboxProperties.TryGetValue(material.AssetId, out var dopantRecord)
                    && !MaterialPropertyRecordUtility.IsEmpty(dopantRecord);

            ResearchMaterialView materialView = Find.NamedAsset<ResearchMaterialView>(material.AssetId);
            Assert.False(materialView == null, $"[ResearchSampleTrayUtility] Missing research material view for {material.AssetId.ToDebugString()}");

            atom.MaterialSprite.SetAlpha(1f);
            atom.MaterialSprite.color = materialView.AtomColor[elementIndex];

            if (atom.Label != null) {
                atom.Label.text = known ? material.ShortName : "?";
            }
            
            for (int i = 0; i < atom.ElectronSprites.Length; i++) {
                SpriteRenderer electron = atom.ElectronSprites[i];
                electron.SetAlpha(i < material.ValenceElectronCounts[elementIndex] ? 1f : 0f);
            }
        }

        public static void Clear(MaterialAtom atom) {
            if (atom.MaterialSprite != null) {
                atom.MaterialSprite.SetAlpha(0f);
            }
            if (atom.ElectronSprites != null) {
                foreach (SpriteRenderer electron in atom.ElectronSprites) {
                    electron.SetAlpha(0f);
                }
            }
            if (atom.Label != null) {
                atom.Label.SetText(string.Empty);
            }
        }
    }
}