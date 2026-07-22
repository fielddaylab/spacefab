using TMPro;
using UnityEngine;
using UnityEngine.UI;
using SpaceFab.Research;
using FieldDay;

namespace SpaceFab {
    public class ContractUI : MonoBehaviour
    {
        public TMP_Text Title;
        public TMP_Text Description;
        public Transform RequirementParent;
        public ResearchObservationChip RequirementElement;

        public Image[] TimeIndicators;
        public Image[] RevenueIndicators;

        public GameObject ApprovedStamp;
        public Image SignatureImage;

        public void ClearElements()
        {
            Find.GlobalAsset<ContractMeterSpriteSet>(out ContractMeterSpriteSet spriteSet);

            for (int i = 0; i < TimeIndicators.Length; i++) {
                TimeIndicators[i].sprite = spriteSet.TimeEmpty;
            }
            for (int i = 0; i < RevenueIndicators.Length; i++) {
                RevenueIndicators[i].sprite = spriteSet.RevenueEmpty;
            }
            for (int i = 0; i < RequirementParent.childCount; i++) {
                Destroy(RequirementParent.GetChild(i).gameObject);
            }
        }

        public void ShowDuration(int duration)
        {
            Find.GlobalAsset<ContractMeterSpriteSet>(out ContractMeterSpriteSet spriteSet);

            for (int i = 0; i < duration; i++) {
                TimeIndicators[i].sprite = spriteSet.TimeFilled;
            }
        }

        public void ShowProfit(int profit)
        {
            Find.GlobalAsset<ContractMeterSpriteSet>(out ContractMeterSpriteSet spriteSet);
            
            for (int i = 0; i < profit; i++) {
                RevenueIndicators[i].sprite = spriteSet.RevenueFilled;
            }
        }

        public void ShowRequirement(Materials.MaterialPropertyCheck[] requiredProperties)
        {
            foreach (var property in requiredProperties)
            {
                string materialName = MaterialPropertyLabelDisplay.GetPropertyName(property.Label);
                materialName = char.ToUpper(materialName[0]) + materialName[1..].ToLower();
                
                RequirementElement.SetState(
                    materialName,
                    true,
                    false,
                    Materials.MaterialObservationChamberLookup.GetChamberType(property.Label)
                );
                GameObject element = Instantiate(RequirementElement.gameObject);
                element.transform.SetParent(RequirementParent);
                element.transform.localScale = Vector3.one;
            }
        }
    }

    public static partial class ContractUtility
    {
        public static void LoadContractData(ContractUI ui, ContractDef def)
        {
            if (def == null)
            {
                ui.Title.SetText(string.Empty);
                ui.Description.SetText(string.Empty);
            }
            else
            {
                ui.Title.SetText(def.Title());
                ui.Description.SetText(def.Description());
                ui.ClearElements();
                ui.ShowDuration(def.ExpectedDuration());
                ui.ShowProfit(def.Payout());
                ui.ShowRequirement(def.RequiredMaterialProperties());
            }
        }
    }
}
