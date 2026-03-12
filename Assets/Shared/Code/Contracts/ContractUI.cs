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
        // TODO
        public static void LoadContractData(ContractUI contract, string title)
        {
            contract.Title.SetText(title);
        }
    }
}
