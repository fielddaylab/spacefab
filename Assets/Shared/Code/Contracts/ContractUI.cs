using TMPro;
using UnityEngine;
using UnityEngine.UI;
using SpaceFab.Research;
using FieldDay;
using SpaceFab.UI;

namespace SpaceFab {
    public class ContractUI : MonoBehaviour
    {
        public TMP_Text Title;
        public TMP_Text Description;
        public ContractRequirementTable Requirements;

        public Image[] TimeIndicators;
        public Image[] RevenueIndicators;

        public DynamicButton SelectContractButton;
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

        public void ShowRequirement(int chapterIndex, ContractDef contract)
        {
            ContractUIUtility.BuildRequirementData(Requirements, chapterIndex, contract);
            ContractUIUtility.InitializeVisuals(Requirements);
            ContractUIUtility.UpdateVisualsWithCompletion(Requirements, Find.State<PlayerProgressState>().MaterialProperties);
        }
    }

    public static partial class ContractUtility
    {
        public static void LoadContractData(ContractUI ui, int chapterIndex, ContractDef def)
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
                ui.ShowRequirement(chapterIndex, def);
            }
        }
    }
}
