using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

namespace SpaceFab {
    public class ContractUI : MonoBehaviour
    {
        public TMP_Text Title;
    }

    public static class ContractUtility
    {
        public static void LoadContractData(ContractUI ui, ContractDef def)
        {
            if (def == null)
            {
                ui.Title.SetText(string.Empty);
            }
            else
            {
                ui.Title.SetText(def.Title());
            }
        }
    }
}
