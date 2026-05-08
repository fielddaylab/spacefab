using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

namespace SpaceFab {
    public class ContractUI : MonoBehaviour
    {
        public TMP_Text Title;
        public TMP_Text Description;
        public Transform DurationParent, ProfitParent;
        public GameObject DurationElement, ProfitElement;

        public void ShowElements(int elements, Transform parent, GameObject elementObject)
        {
            for (int i = 0; i < elements; i++)
            {
                GameObject element = Instantiate(elementObject);
                elementObject.transform.SetParent(parent);
                elementObject.transform.localScale = Vector3.one;

                Debug.Log("Working?");
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
                Debug.Log("Loading some stuff!");
                ui.ShowElements(def.ExpectedDuration(), ui.DurationParent, ui.DurationElement);
                ui.ShowElements(def.ExpectedProfit(), ui.ProfitParent, ui.ProfitElement);
                Debug.Log("Loaded some stuff!");
            }
        }
    }
}
