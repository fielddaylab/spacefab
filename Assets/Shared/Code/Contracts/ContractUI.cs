using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using SpaceFab.Research;

namespace SpaceFab {
    public class ContractUI : MonoBehaviour
    {
        public TMP_Text Title;
        public TMP_Text Description;
        public Transform DurationParent;
        public Transform[] ProfitParent;
        public Transform RequirementParent;

        public GameObject DurationElement, ProfitElement;
        public ResearchObservationChip RequirementElement;
        public GameObject ApprovedStamp;
        public Image SignatureImage;

        public void ClearElements()
        {
            for (int i = 0; i < DurationParent.childCount; i++)
            {
                Destroy(DurationParent.GetChild(i).gameObject);
            }
            for (int i = 0; i < ProfitParent[0].childCount; i++)
            {
                Destroy(ProfitParent[0].GetChild(i).gameObject);
            }
            for (int i = 0; i < ProfitParent[1].childCount; i++)
            {
                Destroy(ProfitParent[1].GetChild(i).gameObject);
            }
            for (int i = 0; i < RequirementParent.childCount; i++)
            {
                Destroy(RequirementParent.GetChild(i).gameObject);
            }
        }

        public void ShowDuration(int duration)
        {
            for (int i = 0; i < duration; i++)
            {
                GameObject element = Instantiate(DurationElement);
                element.transform.SetParent(DurationParent);
                element.transform.localScale = Vector3.one;
            }
        }

        public void ShowProfit(int profit)
        {
            ProfitParent[1].gameObject.SetActive(profit > 5);
            for (int i = 0; i < profit; i++)
            {
                GameObject element = Instantiate(ProfitElement);
                element.transform.SetParent(ProfitParent[i / 5]);
                element.transform.localScale = Vector3.one;
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
