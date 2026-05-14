using BeauUtil.Debugger;
using FieldDay;
using FieldDay.SharedState;
using SpaceFab.Fabrication.Stations;
using System.Collections;
using System.Collections.Generic;
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
        public GameObject SpaceSprite, LRArrowSprite, FullArrowSprite, ADArrowSprite;
        public TextMeshProUGUI InstructionTMP, SubtitleTMP;
        public InstructionsLookup InstructionsLookup;

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
        }
    }
}