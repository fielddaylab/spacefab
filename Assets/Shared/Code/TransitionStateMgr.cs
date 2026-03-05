using BeauPools;
using BeauRoutine;
using BeauUtil;
using BeauUtil.Debugger;
using EasyAssetStreaming;
using FieldDay;
using FieldDay.Scenes;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Spacefab
{
    public class TransitionStateMgr : MonoBehaviour
    {
        private Routine m_SceneLoadRoutine;

        [NonSerialized] private StringHash32 m_EntranceId;
        [NonSerialized] private bool m_SceneLock;

        private Func<IEnumerator> m_OnSceneReadyFunc;
        private RingBuffer<Action> m_OnLoadQueue = new RingBuffer<Action>(64, RingBufferMode.Expand);


        #region Scene Loading

        private void ProcessCallbackQueue()
        {
            while (m_OnLoadQueue.TryPopFront(out var action))
            {
                action();
            }
        }

        /// <summary>
        /// Loads to another scene.
        /// </summary>
        public IEnumerator LoadScene(SceneBinding inScene, StringHash32 inEntrance = default(StringHash32), object inContext = null, SceneTransitionFlags inFlags = 0)
        {
            if (m_SceneLock)
            {
                Log.Error("[StateMgr] Scene load already in progress");
                return null;
            }

            if (!inScene.IsValid())
            {
                Log.Error("[StateMgr] Provided scene '{0}' is not valid", inScene);
                return null;
            }

            m_SceneLock = true;
            m_SceneLoadRoutine.Replace(SceneSwap(inScene, inEntrance, inContext, inFlags));
            return m_SceneLoadRoutine.Wait();
        }

        private IEnumerator SceneSwap(SceneBinding inNextScene, StringHash32 inEntrance, object inContext, SceneTransitionFlags inFlags)
        {
            /*
            if ((inFlags & SceneLoadFlags.DoNotDispatchPreUnload) == 0)
            {
                Services.Events.Dispatch(GameEvents.SceneWillUnload);
            }
            */

            SceneBinding active = SceneHelper.ActiveScene();
            m_EntranceId = inEntrance;

            active.BroadcastUnload(inContext);

            // Services.Deregister(active);

            AsyncOperation loadOp = SceneManager.LoadSceneAsync(inNextScene.Path, LoadSceneMode.Single);
            loadOp.allowSceneActivation = false;

            //Debug.Log("[TransitionStateMgr] Loading scene '{0}' with entrance '{1}'", inNextScene.Path, m_EntranceId);

            while (loadOp.progress < 0.9f)
                yield return null;

            Debug.Log("[TransitionStateMgr] Scene ready to activate");

            if (m_OnSceneReadyFunc != null)
            {
                IEnumerator readyFunc = m_OnSceneReadyFunc();
                m_OnSceneReadyFunc = null;
                yield return readyFunc;
            }

            loadOp.allowSceneActivation = true;

            while (!loadOp.isDone)
                yield return null;

            //BindScene(inNextScene);
            //Services.Camera.DisableRendering();
            //yield return WaitForServiceLoading();

            yield return WaitForCleanup();

            //Debug.Log("[TransitionStateMgr] Finished loading scene '{0}'", inNextScene.Path);

            ProcessCallbackQueue();
            inNextScene.BroadcastLoaded(inContext);
        }

        public void OnSceneLoadReady(Func<IEnumerator> inFunc)
        {
            m_OnSceneReadyFunc = inFunc;
        }

        private IEnumerator WaitForCleanup()
        {
            using (Profiling.Time("gc collect"))
            {
                GC.Collect();
            }
            using (Profiling.Time("unload unused assets"))
            {
                Streaming.UnloadUnusedAsync();
                while (Streaming.IsUnloading())
                {
                    yield return null;
                }
                yield return Resources.UnloadUnusedAssets();
            }
        }

        #endregion // Scene Loading
    }
}
