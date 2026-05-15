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
        public InstructionLookup InstructionsLookup;
        [SerializeField] private GameObject m_SpaceImage, m_LRArrowImage, m_FullArrowImage, m_MouseImage, m_ADArrowImage;
        [SerializeField] private TextMeshProUGUI m_InstructionTMP, m_SubtitleTMP;

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

            m_SpaceImage.SetActive(false);
            m_LRArrowImage.SetActive(false);
            m_FullArrowImage.SetActive(false);
            m_MouseImage.SetActive(false);
            m_ADArrowImage.SetActive(false);
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
                case KeyImage.Space:
                    m_SpaceImage.SetActive(true);
                    break;
                case KeyImage.LRArrows:
                    m_LRArrowImage.SetActive(true);
                    break;
                case KeyImage.FullArrows:
                    m_FullArrowImage.SetActive(true);
                    break;
                case KeyImage.Mouse:
                    m_MouseImage.SetActive(true);
                    break;
                case KeyImage.ADKeys:
                    m_ADArrowImage.SetActive(true);
                    break;
            }

            m_InstructionTMP.text = uiInstructions.Instruction;
            m_SubtitleTMP.text = uiInstructions.Subtitle;
        }

        public void HideUI()
        {
            FaderGroup.alpha = 0f;
            FaderGroup.blocksRaycasts = false;

            InstructionsGroup.alpha = 0f;
            InstructionsGroup.blocksRaycasts = false;

            // setting the canvas shouldn't be necessary as it gets updated on ShowUI, but maybe change?
        }
    }
}