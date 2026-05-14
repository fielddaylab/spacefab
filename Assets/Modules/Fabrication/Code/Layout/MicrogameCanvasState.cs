using BeauUtil;
using BeauUtil.Debugger;
using FieldDay;
using FieldDay.SharedState;
using SpaceFab.Fabrication.Stations;
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
        public CanvasGroup PopupGroup;
        public CanvasGroup InstructionsGroup;
        public GameObject SpaceImage, LRArrowImage, FullArrowImage, MouseImage, ADArrowImage;
        public TextMeshProUGUI InstructionTMP, SubtitleTMP;
        public InstructionLookup InstructionsLookup;

        public void OnDeregister()
        {
        }

        public void OnRegister()
        {
            FaderGroup.alpha = 0;
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

        public void ShowUI(SerializedHash32 stationID)
        {
            FaderGroup.alpha = 1f;
            FaderGroup.blocksRaycasts = true;

            InstructionsGroup.alpha = 1f;
            InstructionsGroup.blocksRaycasts = true;

            InstructionSet uiInstructions = InstructionLookupUtility.LookupInstructions(stationID, InstructionsLookup);

            switch (uiInstructions.UIKey)
            {
                case KeyType.Space:
                    SpaceImage.SetActive(true);
                    break;
                case KeyType.LRArrows:
                    LRArrowImage.SetActive(true);
                    break;
                case KeyType.FullArrows:
                    FullArrowImage.SetActive(true);
                    break;
                case KeyType.Mouse:
                    MouseImage.SetActive(true);
                    break;
                case KeyType.ADKeys:
                    ADArrowImage.SetActive(true);
                    break;
            }

            InstructionTMP.text = uiInstructions.Instruction;
            SubtitleTMP.text = uiInstructions.Subtitle;
        }

        public void HideUI()
        {
            FaderGroup.alpha = 0f;
            FaderGroup.blocksRaycasts = false;

            InstructionsGroup.alpha = 0f;
            InstructionsGroup.blocksRaycasts = false;

            // setting the canvas shouldn't be necessary as it gets updated on show ui, but maybe change?
        }
    }
}