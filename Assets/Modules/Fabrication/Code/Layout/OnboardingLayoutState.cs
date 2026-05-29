using FieldDay;
using FieldDay.SharedState;
using SpaceFab.Design;
using SpaceFab.UI;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace SpaceFab.Fabrication
{
    public class OnboardingLayoutState : SharedStateComponent, IRegistrationCallbacks
    {
        public CanvasGroup GenerateGroup;
        public CanvasGroup ControlsGroup;
        public DynamicButton GenerateButton;
        public DynamicButton LeftArrowButton;
        public DynamicButton RightArrowButton;
        [HideInInspector] public bool IsGeneratePressed, IsLeftArrowPressed, IsRightArrowPressed;

        public void OnRegister()
        {
            OnboardingStateUtility.SetEnabledGroup(GenerateGroup, true);
            OnboardingStateUtility.SetEnabledGroup(ControlsGroup, false);
            
            if (GenerateButton != null)
            {
                GenerateButton.gameObject.SetActive(true);
                GenerateButton.onClick.AddListener(OnGenerateClicked);
            }

            if (LeftArrowButton != null)
            {
                LeftArrowButton.gameObject.SetActive(true);
                LeftArrowButton.onClick.AddListener(OnLeftArrowClicked);
            }

            if (RightArrowButton != null)
            {
                RightArrowButton.gameObject.SetActive(true);
                RightArrowButton.onClick.AddListener(OnRightArrowClicked);
            }
        }

        public void OnDeregister()
        {
            if (GenerateButton != null)
            {
                GenerateButton.onClick.RemoveListener(OnGenerateClicked);
            }

            if (LeftArrowButton != null)
            {
                LeftArrowButton.onClick.RemoveListener(OnLeftArrowClicked);
            }

            if (RightArrowButton != null)
            {
                RightArrowButton.onClick.RemoveListener(OnRightArrowClicked);
            }
        }

        private void OnGenerateClicked()
        {
            IsGeneratePressed = true;
        }

        private void OnLeftArrowClicked()
        {
            IsLeftArrowPressed = true;
        }

        private void OnRightArrowClicked()
        {
            IsRightArrowPressed = true;
        }
    }

    public static class OnboardingStateUtility
    {
        public static void HideGenerateButton()
        {
            Find.State(
                out OnboardingLayoutState onboardState
                );

            onboardState.GenerateGroup.gameObject.SetActive(false);
            SetEnabledGroup(onboardState.GenerateGroup, false);
            SetEnabledGroup(onboardState.ControlsGroup, true);
            onboardState.IsGeneratePressed = false;
        }
        
        public static void SetEnabledGroup(CanvasGroup canvasGroup, bool isEnabled)
        {
            if (canvasGroup == null)
            {
                Debug.LogWarning("OnboardingCanvas is null!");
                return;
            }

            canvasGroup.alpha = isEnabled ? 1f : 0f;
            canvasGroup.blocksRaycasts = isEnabled;
            canvasGroup.interactable = isEnabled;
        }
    }
}
