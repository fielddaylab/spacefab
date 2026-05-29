using BeauUtil;
using BeauUtil.Debugger;
using FieldDay;
using FieldDay.SharedState;
using FieldDay.UI.Animation;
using SpaceFab.Fabrication.Stations;
using SpaceFab.UI;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using static Unity.Mathematics.math;

namespace SpaceFab.Fabrication.Layout
{
    /// <summary>
    /// </summary>
    public class MicrogameCanvasState : SharedStateComponent, IRegistrationCallbacks
    {
        public CanvasGroup FaderGroup;

        [Space(5)]
        [Header("Popup Group")]
        public CanvasGroup PopupGroup;
        public TMP_Text PopupMainText;
        public TMP_Text PopupSecondaryText;
        public DynamicButton StationRestartButton;

        [Space(5)]
        public CanvasGroup InstructionsGroup;
        public InstructionLookup InstructionsLookup;
        public GameObject SpaceImage, LRArrowImage, FullArrowImage, MouseImage, ADArrowImage;
        public TextMeshProUGUI m_InstructionTMP, m_SubtitleTMP;

        public void OnDeregister()
        {
        }

        public void OnRegister()
        {
            FaderGroup.alpha = 1;
            FaderGroup.blocksRaycasts = false;

            PopupGroup.alpha = 0;
            PopupGroup.blocksRaycasts = false;

            InstructionsGroup.alpha = 0;
            InstructionsGroup.blocksRaycasts = false;

            SpaceImage.SetActive(false);
            LRArrowImage.SetActive(false);
            FullArrowImage.SetActive(false);
            MouseImage.SetActive(false);
            ADArrowImage.SetActive(false);
        }
    }

    public static class MicrogameCanvasUtility
    {
        public static void ShowStationInstructions(MicrogameCanvasState state, SerializedHash32 stationID)
        {
            state.FaderGroup.alpha = 1f;
            state.FaderGroup.blocksRaycasts = true;

            state.InstructionsGroup.alpha = 1f;
            state.InstructionsGroup.blocksRaycasts = true;

            InstructionSet uiInstructions = InstructionLookupUtility.LookupInstructions(stationID, state.InstructionsLookup);

            switch (uiInstructions.UIKey)
            {
                case KeyImage.Space:
                    state.SpaceImage.SetActive(true);
                    break;
                case KeyImage.LRArrows:
                    state.LRArrowImage.SetActive(true);
                    break;
                case KeyImage.FullArrows:
                    state.FullArrowImage.SetActive(true);
                    break;
                case KeyImage.Mouse:
                    state.MouseImage.SetActive(true);
                    break;
                case KeyImage.ADKeys:
                    state.ADArrowImage.SetActive(true);
                    break;
            }

            state.m_InstructionTMP.text = uiInstructions.Instruction;
            state.m_SubtitleTMP.text = uiInstructions.Subtitle;
        }

        public static void HideStationInstructions(MicrogameCanvasState state)
        {
            state.FaderGroup.alpha = 0f;
            state.FaderGroup.blocksRaycasts = false;

            state.InstructionsGroup.alpha = 0f;
            state.InstructionsGroup.blocksRaycasts = false;

            state.SpaceImage.SetActive(false);
            state.LRArrowImage.SetActive(false);
            state.FullArrowImage.SetActive(false);
            state.MouseImage.SetActive(false);
            state.ADArrowImage.SetActive(false);
        }
    }
}