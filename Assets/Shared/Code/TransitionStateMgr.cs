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
        private Routine m_SharedUIRoutine;

        #region Constuctor

        public TransitionStateMgr()
        {
            Game.Scenes.OnPrepareScene.Register(HandlePrepareScene);
            Game.Scenes.OnSceneReady.Register(HandleSceneReady);
        }

        #endregion // Constructor

        #region SceneMgr Handlers

        private void HandlePrepareScene(SceneCallbackArgs args)
        {
            // Skip SharedUI behavior in Boot scene
            SceneBinding active = SceneManager.GetActiveScene();
            if (active.BuildIndex != GameConsts.StartGameSceneIndex) {
                m_SharedUIRoutine.Stop();

                SharedUIState uiState = Find.State<SharedUIState>();
                m_SharedUIRoutine.Replace(SharedUIUtility.OnBeginLoading(uiState));
            }
        }

        private void HandleSceneReady(SceneCallbackArgs args)
        {
            // Skip SharedUI behavior in Boot scene
            SceneBinding active = SceneManager.GetActiveScene();
            if (active.BuildIndex != GameConsts.StartGameSceneIndex)
            {
                if (m_SharedUIRoutine.Exists())
                {
                    m_SharedUIRoutine.OnComplete(() =>
                    {
                        SharedUIState uiState = Find.State<SharedUIState>();
                        m_SharedUIRoutine.Replace(SharedUIUtility.OnLoadingComplete(uiState));
                    });
                }
            }
        }

        #endregion // SceneMgr Handlers
    }
}
