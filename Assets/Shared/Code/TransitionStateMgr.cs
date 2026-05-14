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
            Game.Scenes.OnPrepareScene.Register(HandlePrepareScene);
            Game.Scenes.OnSceneReady.Register(HandleSceneReady);

            Game.Events.Register(GameEvents.ProfileSaveBegin, HandleProfileSaveBegin);
            Game.Events.Register(GameEvents.ProfileSaveSuccess, HandleProfileSaveSuccess);
            Game.Events.Register(GameEvents.ProfileSaveError, HandleProfileSaveError);
        }

        #endregion // Constructor

        #region SceneMgr Handlers

        private void HandlePrepareScene(SceneCallbackArgs args)
        {
            // Skip SharedUI behavior in Boot scene, and when loading aux / persistent scenes
            SceneBinding active = SceneManager.GetActiveScene();
            if (active.BuildIndex != GameConsts.StartGameSceneIndex) {
                if ((args.LoadType != SceneType.Aux) && (args.LoadType != SceneType.Persistent))
                {
                    m_SceneLoadRoutine.Stop();

                    SharedUIState uiState = Find.State<SharedUIState>();
                    m_SceneLoadRoutine.Replace(SharedUIUtility.OnBeginLoading(uiState));
                }
            }
        }

        private void HandleSceneReady(SceneCallbackArgs args)
        {
            // Skip SharedUI behavior in Boot scene
            SceneBinding active = SceneManager.GetActiveScene();
            if (active.BuildIndex != GameConsts.StartGameSceneIndex)
            {
                if ((args.LoadType != SceneType.Aux) && (args.LoadType != SceneType.Persistent))
                {
                    if (m_SceneLoadRoutine.Exists())
                    {
                        m_SceneLoadRoutine.OnComplete(() =>
                        {
                            SharedUIState uiState = Find.State<SharedUIState>();
                            m_SceneLoadRoutine.Replace(SharedUIUtility.OnLoadingComplete(uiState));
                        });
                    }
                    else
                    {
                        SharedUIState uiState = Find.State<SharedUIState>();
                        m_SceneLoadRoutine.Replace(SharedUIUtility.OnLoadingComplete(uiState));
                    }
                }
            }
        }

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

        #endregion // SceneMgr Handlers
    }
}
