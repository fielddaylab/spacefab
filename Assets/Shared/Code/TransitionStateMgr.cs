using BeauPools;
using BeauRoutine;
using BeauUtil;
using BeauUtil.Debugger;
using BeauUtil.Services;
using EasyAssetStreaming;
using FieldDay;
using FieldDay.Scenes;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace SpaceFab
{
    public class TransitionStateMgr
    {
        private Routine m_SceneLoadRoutine;
        private Routine m_SaveRoutine;

        #region Constructor

        public TransitionStateMgr()
        {
            Game.Scenes.OnMainSceneUnloading.Register(HandleMainSceneUnloading);
            Game.Scenes.OnMainSceneReady.Register(HandleMainSceneReady);

            Game.Events.Register(GameEvents.ProfileSaveBegin, HandleProfileSaveBegin);
            Game.Events.Register(GameEvents.ProfileSaveSuccess, HandleProfileSaveSuccess);
            Game.Events.Register(GameEvents.ProfileSaveError, HandleProfileSaveError);
        }

        #endregion // Constructor

        #region Scene Loading

        private void HandleMainSceneUnloading() {
            m_SceneLoadRoutine.Replace(LoadingIconTransition());
        }

        private void HandleMainSceneReady() {
            Find.State(out SharedUIState uiState);

            m_SceneLoadRoutine.Stop();
            uiState.LoadIcon.Group.gameObject.SetActive(false);
        }

        private IEnumerator LoadingIconTransition() {
            Find.State(out FullscreenTransitionState fullscreenTransition, out SharedUIState uiState);
            while(!FullscreenTransitionUtility.IsTransitionFullyFadedOut(fullscreenTransition)) {
                yield return null;
            }
            yield return 1.5;
            uiState.LoadIcon.Group.gameObject.SetActive(true);
            uiState.LoadIcon.Group.alpha = 0;
            yield return uiState.LoadIcon.Group.FadeTo(1, 0.3f);
        }

        #endregion // Scene Loading

        #region Profile Saving

        private void HandleProfileSaveBegin()
        {
            m_SaveRoutine.Stop();

            SharedUIState uiState = Find.State<SharedUIState>();
            m_SaveRoutine.Replace(SharedUIUtility.OnBeginSave(uiState));
        }

        private void HandleProfileSaveSuccess()
        {
            if (m_SaveRoutine.Exists())
            {
                m_SaveRoutine.OnComplete(() =>
                {
                    SharedUIState uiState = Find.State<SharedUIState>();
                    m_SaveRoutine.Replace(SharedUIUtility.OnSaveSuccess(uiState));
                });
            }
            else
            {
                SharedUIState uiState = Find.State<SharedUIState>();
                m_SaveRoutine.Replace(SharedUIUtility.OnSaveSuccess(uiState));
            }
        }

        private void HandleProfileSaveError()
        {
            if (m_SaveRoutine.Exists())
            {
                m_SaveRoutine.OnComplete(() =>
                {
                    SharedUIState uiState = Find.State<SharedUIState>();
                    m_SaveRoutine.Replace(SharedUIUtility.OnSaveError(uiState));
                });
            }
            else
            {
                SharedUIState uiState = Find.State<SharedUIState>();
                m_SaveRoutine.Replace(SharedUIUtility.OnSaveError(uiState));
            }
        }

        #endregion // Profile Saving
    }
}
