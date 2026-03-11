using BeauRoutine;
using BeauUtil;
using FieldDay;
using FieldDay.Scenes;
using SpaceFab.Save;
using SpaceFab;
using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace SpaceFab.Title
{
    public class TitleUI : MonoBehaviour, IScenePreload
    {
        private const string NEW_GAME_LABEL = "Begin";
        private const string CONTINUE_GAME_LABEL = "Continue";

        private enum GroupType
        {
            Main,
            NewGame,
            ContinueGame,
            Options,
        }

        public SceneReference m_NextScene;
        public SceneReference m_CreditsScene;

        [Header("Shared")]
        [SerializeField] private CanvasGroup m_SharedGroup;
        [SerializeField] private Button m_BackButton;

        [Header("Main Panel")]
        [SerializeField] private CanvasGroup m_MainGroup;
        [SerializeField] private Button m_NewGameGroupButton;
        [SerializeField] private Button m_ContinueGroupButton;
        [SerializeField] private Button m_OptionsButton;
        [SerializeField] private Button m_CreditsButton;

        [Header("Player Code Panel")]
        [SerializeField] private CanvasGroup m_PlayerCodeGroup;
        [SerializeField] private TMP_InputField m_PlayerCodeInput;
        [SerializeField] private Button m_StartButton;
        [SerializeField] private TMP_Text m_StartButtonText;
        [SerializeField] private CanvasGroup m_NotFoundGroup;

        [Header("Options Panel")]
        [SerializeField] private CanvasGroup m_OptionsGroup;


        private GroupType m_CurrGroupType = GroupType.Main;

        [NonSerialized] private Routine m_SharedGroupRoutine;
        [NonSerialized] private Routine m_MainGroupRoutine;
        [NonSerialized] private Routine m_PlayerCodeGroupRoutine;
        [NonSerialized] private Routine m_OptionsGroupRoutine;
        [NonSerialized] private Routine m_NotFoundRoutine;

        public IEnumerator<WorkSlicer.Result?> Preload()
        {
            m_NewGameGroupButton.onClick.AddListener(HandleNewGameGroupClicked);
            m_ContinueGroupButton.onClick.AddListener(HandleContinueGroupClicked);
            m_OptionsButton.onClick.AddListener(HandleOptionsClicked);
            m_CreditsButton.onClick.AddListener(HandleCreditsClicked);
            
            m_BackButton.onClick.AddListener(HandleBackButton);
            m_StartButton.onClick.AddListener(HandleStartButton);

            m_PlayerCodeInput.onValueChanged.AddListener(HandlePlayerCodeUpdated);

            m_MainGroup.alpha = 1;

            DisableGroup(m_SharedGroup);
            DisableGroup(m_PlayerCodeGroup);
            DisableGroup(m_NotFoundGroup);
            DisableGroup(m_OptionsGroup);

            return null;
        }

        private void OnDisable()
        {
            if (SpacefabGame.IsShuttingDown) { return; }

            m_NewGameGroupButton.onClick.RemoveListener(HandleNewGameGroupClicked);
            m_ContinueGroupButton.onClick.RemoveListener(HandleContinueGroupClicked);
            m_OptionsButton.onClick.RemoveListener(HandleOptionsClicked);
            m_CreditsButton.onClick.RemoveListener(HandleCreditsClicked);

            m_BackButton.onClick.RemoveListener(HandleBackButton);
            m_StartButton.onClick.RemoveListener(HandleStartButton);

            m_PlayerCodeInput.onValueChanged.RemoveListener(HandlePlayerCodeUpdated);
        }

        #region Helpers

        private void OpenSecondaryGroup(string playerCode)
        {
            m_PlayerCodeInput.SetTextWithoutNotify(playerCode);
            HandlePlayerCodeUpdated(m_PlayerCodeInput.text);

            m_MainGroupRoutine.Replace(this, HideGroupRoutine(m_MainGroup));

            m_SharedGroupRoutine.Replace(this, ShowGroupRoutine(m_SharedGroup));
            m_PlayerCodeGroupRoutine.Replace(this, ShowGroupRoutine(m_PlayerCodeGroup));

            m_NotFoundGroup.alpha = 0;
            m_NotFoundRoutine.Stop();
        }

        private void DisableGroup(CanvasGroup group)
        {
            group.alpha = 0;
            group.interactable = false;
            group.blocksRaycasts = false;
        }

        #endregion Helpers

        #region Handlers

        private void HandleNewGameGroupClicked()
        {
            m_CurrGroupType = GroupType.NewGame;

            OpenSecondaryGroup(string.Empty);
            m_PlayerCodeInput.readOnly = true;

            m_StartButtonText.SetText(NEW_GAME_LABEL);

            OGD.Player.NewId(HandleNewPlayerId, HandleNewPlayerIdError);
        }

        private void HandleContinueGroupClicked()
        {
            m_CurrGroupType = GroupType.ContinueGame;

            OpenSecondaryGroup(Game.SharedState.Get<UserSettingsState>().PlayerCode);
            m_PlayerCodeInput.readOnly = false;

            m_StartButtonText.SetText(CONTINUE_GAME_LABEL);
        }

        private void HandleStartButton()
        {
            // m_Raycaster.blocksRaycasts = false;
            if (m_CurrGroupType == GroupType.NewGame)
            {
                SpacefabGame.SaveBuffer.Clear();
                OGD.Player.ClaimId(m_PlayerCodeInput.text, null, HandleStartAccepted, HandleClaimNewIdError);
            }
            else
            {
                Future f = SaveUtility.LoadFromServer(m_PlayerCodeInput.text);
                f.OnComplete(HandleStartAccepted);
                f.OnFail(HandleLoadError);
            }
        }

        private void HandleOptionsClicked()
        {
            m_CurrGroupType = GroupType.Options;

            m_MainGroupRoutine.Replace(this, HideGroupRoutine(m_MainGroup));

            m_OptionsGroupRoutine.Replace(this, ShowGroupRoutine(m_OptionsGroup));
            m_SharedGroupRoutine.Replace(this, ShowGroupRoutine(m_SharedGroup));
        }

        private void HandleCreditsClicked()
        {
            Game.Scenes.LoadMainScene(m_CreditsScene);
        }

        private void HandleBackButton()
        {
            if (m_CurrGroupType == GroupType.Options) {
                m_OptionsGroupRoutine.Replace(this, HideGroupRoutine(m_OptionsGroup));
            }
            else {
                m_PlayerCodeGroupRoutine.Replace(this, HideGroupRoutine(m_PlayerCodeGroup));
            }
            m_SharedGroupRoutine.Replace(this, HideGroupRoutine(m_SharedGroup));

            m_CurrGroupType = GroupType.Main;

            m_MainGroupRoutine.Replace(this, ShowGroupRoutine(m_MainGroup));
        }

        private void HandlePlayerCodeUpdated(string text)
        {
            m_StartButton.interactable = text.Length > 1;
        }

        #endregion // Handlers

        #region Routines

        private IEnumerator HideGroupRoutine(CanvasGroup group)
        {
            group.blocksRaycasts = false;
            group.interactable = false;
            yield return group.FadeTo(0, 0.2f);
            group.gameObject.SetActive(false);
            DisableGroup(group);
        }

        private IEnumerator ShowGroupRoutine(CanvasGroup group)
        {
            // wait for hide group to complete
            yield return 0.2f;

            group.blocksRaycasts = false;
            group.gameObject.SetActive(true);
            yield return group.FadeTo(1, 0.2f);
            group.interactable = true;
            group.blocksRaycasts = true;
        }

        private IEnumerator CodeNotFoundRoutine()
        {
            yield return m_NotFoundGroup.FadeTo(1, 0.2f);
            yield return 3f;
            yield return m_NotFoundGroup.FadeTo(0, 0.2f);
        }

        #endregion // Routines

        #region OGD

        private void HandleNewPlayerId(string id)
        {
            if (m_CurrGroupType == GroupType.NewGame)
            {
                m_PlayerCodeInput.SetTextWithoutNotify(id);
                m_StartButton.interactable = true;
                HandlePlayerCodeUpdated(id);
            }
        }

        private void HandleNewPlayerIdError(OGD.Core.Error err)
        {
            if (m_CurrGroupType == GroupType.NewGame)
            {
                OGD.Player.NewId(HandleNewPlayerId, HandleNewPlayerIdError);
            }
        }

        private void HandleStartAccepted()
        {
            m_NotFoundRoutine.Stop();
            Game.SharedState.Get<UserSettingsState>().PlayerCode = m_PlayerCodeInput.text;
            
            // TODO: set this in OGD
            SpacefabGame.Events.Dispatch(GameEvents.TitleProfileStarting, m_PlayerCodeInput.text);
            Game.Scenes.LoadMainScene(m_NextScene);
            // TODO: enable saves
            // SaveUtility.Save(SaveSlot.Main);
        }

        private void HandleClaimNewIdError(OGD.Core.Error err)
        {
            // m_Raycaster.blocksRaycasts = true;
            Debug.LogError(err.ToString());
        }

        private void HandleLoadError()
        {
            // m_Raycaster.blocksRaycasts = true;
            Debug.LogError("[TitleUI] Load from server failed");
            m_NotFoundRoutine.Replace(CodeNotFoundRoutine());
        }

        #endregion // OGD
    }
}