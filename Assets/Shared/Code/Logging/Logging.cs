using BeauUtil;
using BeauUtil.Debugger;
using FieldDay;
using FieldDay.Scripting;
using FieldDay.Vox;
using OGD;
using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace SpaceFab.Logging
{
    public class Logging : MonoBehaviour
    {
        /* TODO: implement once Field Day's override of TMP is sorted
        private const ushort CLIENT_LOG_VERSION = 0;
        private readonly JsonBuilder m_JsonBuilder = new JsonBuilder(Unsafe.KiB * 64); // json allocation capacity
        private OGDLog m_Log;
        [NonSerialized] private bool m_Debug;

        #region Inspector
        [SerializeField, Required] private string m_AppId;
        [SerializeField, Required] private string m_AppVersion;
        [SerializeField] private FirebaseConsts m_Firebase;
        [SerializeField] private bool m_Testing;

        #endregion //Inspector

        private void Start()
        {
            RegisterEvents();
            PrepareLogging();
        }

        #region Game State


        private void SubmitGameState()
        {
            m_JsonBuilder.Clear();

            // TODO

            m_Log.GameState(m_JsonBuilder.End());
        }

        #endregion // Game State

        #region Initialization

        private void PrepareLogging()
        {
#if DEVELOPMENT
            m_Debug = true;
#endif // DEVELOPMENT

            m_Log = new OGDLog(CreateOGDConsts(), new OGDLog.MemoryConfig(2048 * 2, Unsafe.KiB * 64, 256));

            if (!string.IsNullOrEmpty(m_Firebase.ApiKey))
            {
                m_Log.UseFirebase(m_Firebase);
            }
            m_Log.SetDebug(m_Debug);

            // "testing mode" in editor: skip OGD upload but disable debug
#if UNITY_EDITOR
            if (!m_Testing)
            {
                m_Log.AddSettings(OGDLog.SettingsFlags.SkipOGDUpload);
                m_Log.SetDebug(false);
            }
#endif //UNITY_EDITOR

            OGDLog.SchedulingConfig sched = OGDLog.SchedulingConfig.Default;
            sched.FlushDelay = 2;
            m_Log.ConfigureScheduling(sched);
        }


        private void SetAnalyticsUserCode(string userCode)
        {
            Log.Msg("[OGDLog] Assigning user code {0}", userCode);
            m_Log.SetUserId(userCode);
            m_Log.Initialize(CreateOGDConsts());
            m_Log.NewEvent("session_start");
        }

        private OGDLogConsts CreateOGDConsts()
        {
            return new OGDLogConsts()
            {
                AppId = m_AppId,
                AppVersion = m_AppVersion,
                AppBranch = BuildInfo.Branch(),
                ClientLogVersion = CLIENT_LOG_VERSION,
            };
        }

        #endregion //Initialization

        #region Event Registration
        private void RegisterEvents()
        {
            // TODO: Logging events

        }
        #endregion

        #region Logging Variables

        #endregion // Logging Variables

        #region State Handlers

        #endregion // State Handlers

        #region Logging

        #endregion // Logging
        */
    }
}