using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

namespace SpaceFab {
    public class ContractUI : MonoBehaviour
    {
        public TMP_Text Title;
        public TMP_Text Description;
        public Transform DurationParent;
        public Transform[] ProfitParent;
        public GameObject DurationElement, ProfitElement;

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
                element.transform.SetParent(ProfitParent[(profit - 1) / 5]);
                element.transform.localScale = Vector3.one;
            }
        }
    }

    public static class ContractUtility
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
                ui.ShowProfit(def.ExpectedProfit());
            }
        }
    }
}
