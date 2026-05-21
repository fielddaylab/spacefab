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
using SpaceFab;
using System.Diagnostics.Tracing;
using FieldDay.Analytics;
using System.Linq;
using SpaceFab.Design;


namespace SpaceFab.Logging
{
    public class Logging : MonoBehaviour
    {
        public enum ToolID : byte
        {
            PTYPE,
            NTYPE,
            METAL,
            CONTACT,
            GATE
        }

        public enum Minigame: byte
        {
            RESEARCH,
            DESIGN,
            SUPPLY_CHAIN,
            FABRICATION
        }

        public enum Layer: byte
        {
            METAL,
            TRANSISTOR
        }

        public struct GridCoordinate
        {
            public int X;
            public int Y;
            public Layer Layer;

            public GridCoordinate(int x, int y, Layer layer)
            {
                X = x;
                Y = y;
                Layer = layer;
            }
        }

        public struct DesignIO
        {
            public List<GridCoordinate> Inputs;
            public List<GridCoordinate> Outputs;
        }

        public struct DesignGrid
        {
            public List<List<HashSet<ToolID>>> Grid;

            public JsonBuilder ToJson(JsonBuilder json)
            {
                /*
                json.BeginArray();
                for (int i = 0; i < Grid.Count; i++)
                {
                    json.BeginArray();
                    for (int j = 0; j < Grid[i].Count; j++)
                    {
                        json.BeginArray();
                        foreach (ToolID tool in Grid[i][j])
                        {
                            json.Value(tool.ToString());
                        }
                        json.EndArray();
                    }
                    json.EndArray();
                }
                */

                return json;
            }
        }

        // public  List<List<HashSet<ToolID>>> DesignGrid;

        private const ushort CLIENT_LOG_VERSION = 0;
        private readonly JsonBuilder m_JsonBuilder = new JsonBuilder(Unsafe.KiB * 64); // json allocation capacity
        private OGDLog m_Log;
        [NonSerialized] private bool m_Debug;

        #region Inspector
        [SerializeField, Required] private string m_AppId;
        [SerializeField, Required] private string m_AppVersion;
        [SerializeField] private FirebaseConsts m_Firebase;
        [SerializeField] private bool m_Testing;

        [NonSerialized] private float m_SecondsFromLaunch;
        [NonSerialized] private string m_CurrentContract;
        [NonSerialized] private Minigame? m_CurrentMinigame;
        [NonSerialized] private Dictionary<string, Dictionary<string, int>> m_ContractLevels; // number of levels of each minigame, specified by the contract
        [NonSerialized] private DesignGrid? m_DesignGrid;

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
            m_JsonBuilder.Begin()
                .Field("seconds_from_launch", m_SecondsFromLaunch)
                .Field("current_contract", m_CurrentContract)
                .Field("current_minigame", m_CurrentMinigame.HasValue ? m_CurrentMinigame.Value.ToString() : null);
            
            m_JsonBuilder.BeginObject("contract_levels"); // key: contract
            foreach (var kvp in m_ContractLevels)
            {
                m_JsonBuilder.BeginObject(kvp.Key); // key: minigame
                foreach (var minigameKvp in kvp.Value)
                {
                    m_JsonBuilder.Field(minigameKvp.Key, minigameKvp.Value); // value: number of levels
                }
                m_JsonBuilder.EndObject();
            }
            m_JsonBuilder.EndObject();

            if (m_DesignGrid.HasValue)
            {
                m_JsonBuilder.BeginArray("design_grid");
                m_DesignGrid.Value.ToJson(m_JsonBuilder);
                m_JsonBuilder.EndArray();
            }
            else
            {
                m_JsonBuilder.Field("design_grid", (string) null);
            }
            
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
            SpacefabGame.Events.Register<string>(GameEvents.TitleProfileStarting, SetAnalyticsUserCode);

            // TODO: Logging events
            SpacefabGame.Events.Register(GameEvents.TitleStartGameClicked, LogClickNewGame);
            SpacefabGame.Events.Register(GameEvents.TitleBackFromInputClicked, LogClickResumeGame);

        }
        #endregion

        #region Logging Variables

        [NonSerialized] private string m_CurrentContractId;

        #endregion // Logging Variables

        #region State Handlers

        #endregion // State Handlers

        #region Logging

        private void LogSessionStart()
        {
            m_Log.NewEvent("session_start");
        }

        private void LogGameStart(bool fromResume)
        {
            using (var e = m_Log.NewEvent("game_start")) {
            e.Param("from_resume", fromResume);
    }
        }

        private void LogClickNewGame()
        {
            m_Log.NewEvent("click_new_game");
        }

        private void LogClickResumeGame()
        {
            m_Log.NewEvent("click_resume_game");
        }

        private void LogAcceptContract(string contractId, Dictionary<string, Dictionary<string, int>> contractLevels)
        {
            m_CurrentContractId = contractId;
            m_ContractLevels = contractLevels;
            SubmitGameState();

            m_JsonBuilder.Begin();
            foreach (var kvp in contractLevels)
            {
                m_JsonBuilder.BeginObject(kvp.Key); // key: minigame
                foreach (var minigameKvp in kvp.Value)
                {
                    m_JsonBuilder.Field(minigameKvp.Key, minigameKvp.Value); // value: number of levels
                }
                m_JsonBuilder.EndObject();
            }
            string levelsJson = m_JsonBuilder.End().ToString();

            using (var e = m_Log.NewEvent("accept_contract"))
            {
                e.Param("contract_id", contractId);
                e.Param("contract_levels", levelsJson);
            }
        }

        private void LogOpenContractView(string contractName)
        {
            using (var e = m_Log.NewEvent("open_contract_view"))
            {
                e.Param("contract_name", contractName);
            }
        }

        private void LogStartChangeContract(string contractId)
        {
            using (var e = m_Log.NewEvent("start_change_contract"))
            {
                e.Param("contract_id", contractId);
            }
        }

        private void LogConfirmChangeContract(string contractId)
        {
            m_CurrentContractId = contractId;
            SubmitGameState();

            using (var e = m_Log.NewEvent("confirm_change_contract"))
            {
                e.Param("contract_id", contractId);
            }
        }

        private void LogCancelChangeContract(string contractId)
        {
            using (var e = m_Log.NewEvent("cancel_change_contract"))
            {
                e.Param("contract_id", contractId);
            }
        }

        private void LogShipMenuDisplayed()
        {
            m_Log.NewEvent("ship_menu_displayed");
        }

        # region Research
        private void LogSelectResearch()
        {
            m_Log.NewEvent("select_research");
        }

        private void LogStartResearch()
        {
            m_CurrentMinigame = Minigame.RESEARCH;
            SubmitGameState();

            m_Log.NewEvent("start_research");
        }

        #endregion // Research

        #region Design

        private void LogSelectDesign()
        {
            m_Log.NewEvent("select_design");
        }

        private void LogStartDesign()
        {
            m_CurrentMinigame = Minigame.DESIGN;
            SubmitGameState();

            m_Log.NewEvent("start_design");
        }

        private void LogDesignLevelBegin(
            DesignGrid InitialGridState,
            List<GridCoordinate> inputs,
            List<GridCoordinate> outputs
            )
        {
            m_DesignGrid = new DesignGrid() { Grid = InitialGridState.Grid };
            SubmitGameState();

            m_JsonBuilder.Begin();
            InitialGridState.ToJson(m_JsonBuilder);
            string gridJson = m_JsonBuilder.End().ToString();

            m_JsonBuilder.Begin();
            m_JsonBuilder.BeginArray("inputs");
            foreach (var input in inputs)
            {
                m_JsonBuilder.BeginObject()
                    .Field("x", input.X)
                    .Field("y", input.Y)
                    .Field("layer", input.Layer.ToString())
                    .EndObject();
            }
            m_JsonBuilder.EndArray();
            string inputsJson = m_JsonBuilder.End().ToString();

            m_JsonBuilder.Begin();
            m_JsonBuilder.BeginArray("outputs");
            foreach (var output in outputs){
                m_JsonBuilder.BeginObject()
                    .Field("x", output.X)
                    .Field("y", output.Y)
                    .Field("layer", output.Layer.ToString())
                    .EndObject();
            }
            m_JsonBuilder.EndArray();
            string outputsJson = m_JsonBuilder.End().ToString();

            using(var e = m_Log.NewEvent("design_level_begin"))
            {
                e.Param("initial_grid_state", gridJson);
                e.Param("inputs", inputsJson);
                e.Param("outputs", outputsJson);
            }
        }

        private void LogSelectTool(ToolID toolId)
        {
            using (var e = m_Log.NewEvent("select_tool"))
            {
                e.Param("tool_id", toolId.ToString());
            }
        }

        private void LogFillGrid(ToolID toolId, GridCoordinate coordinate)
        {
            // TODO
            using (var e = m_Log.NewEvent("fill_grid"))
            {
                e.Param("tool_id", toolId.ToString());
                e.Param("x", coordinate.X);
                e.Param("y", coordinate.Y);
                e.Param("layer", coordinate.Layer.ToString());
            }
        }

        private void LogSubmitDesign(List<GridCoordinate> inputs, List<GridCoordinate> outputs) // ? inputs outputs type
        {
            // TODO
            m_DesignGrid = null;
            SubmitGameState();
        }

        private void LogSubmissionSucceeded(string message)
        {
            using (var e = m_Log.NewEvent("submission_succeeded"))
            {
                e.Param("message", message);
            }
        }

        private void LogSubmissionFailed(string message)
        {
            using (var e = m_Log.NewEvent("submission_failed"))
            {
                e.Param("message", message);
            }
        }

        private void LogExitDesign()
        {
            m_CurrentMinigame = null;
            m_DesignGrid = null;
            SubmitGameState();

            m_Log.NewEvent("exit_design");
        }

        #endregion // Design

        #region Supply Chain

        private void LogSelectSupplyChain()
        {
            m_Log.NewEvent("select_supply_chain");
        }

        private void LogStartSupplyChain()
        {
            m_CurrentMinigame = Minigame.SUPPLY_CHAIN;
            SubmitGameState();

            m_Log.NewEvent("start_supply_chain");
        }

        #endregion // Supply Chain

        #region Fabrication

        private void LogSelectFabrication()
        {
            m_Log.NewEvent("select_fabrication");
        }
        
        private void LogStartFabrication()
        {
            m_CurrentMinigame = Minigame.FABRICATION;
            SubmitGameState();

            m_Log.NewEvent("start_fabrication");
        }

        private void LogGenerateWafer()
        {
            m_Log.NewEvent("generate_wafer");
        }

        private void LogTimerStart()
        {
            m_Log.NewEvent("timer_start");
        }

        private void LogInstructionUpdated(string nextStation, bool isHidden)
        /*
         "instruction_updated": {
            "description": "When the instruction shown to the player is updated.",
            "event_data": {
                "next_station" : {
                    "type" : "str",
                    "description" : "The next station the player needs to complete, as indicated by the instruction."
                },
                "is_hidden" : {
                    "type" : "bool",
                    "description" : "Whether the instruction is currently hidden from the player or not."
                }
            }
        }
        */
        {
            // TODO

            using (var e = m_Log.NewEvent("instruction_updated"))
            {
                e.Param("next_station", nextStation);
                e.Param("is_hidden", isHidden);
            }
        }

        private void LogActivateStation(string stationId)
        {
            using (var e = m_Log.NewEvent("activate_station"))
            {
                e.Param("station_id", stationId);
            }
        }

        private void LogInvalidActivation(string stationId, string nextStation)
        {
            using (var e = m_Log.NewEvent("invalid_activation"))
            {
                e.Param("station_id", stationId);
                e.Param("reason", nextStation);
            }
        }

        private void LogStationComplete(string stationId, float accuracy, bool isAutomated)
        {
            using (var e = m_Log.NewEvent("station_complete"))
            {
                e.Param("station_id", stationId);
                e.Param("accuracy", accuracy);
                e.Param("is_automated", isAutomated);
            }
        }

        private void LogFabricationComplete()
        {
            m_CurrentMinigame = null;
            SubmitGameState();

            m_Log.NewEvent("fabrication_complete");
        }

        private void LogUseAutomation(string stationId)
        {
            using (var e = m_Log.NewEvent("use_automation"))
            {
                e.Param("station_id", stationId);
            }
        }

        private void LogFabricationSuccess(float accuracy, float time, int production_cycles)
        {
            using (var e = m_Log.NewEvent("fabrication_success"))
            {
                e.Param("accuracy", accuracy);
                e.Param("time", time);
                e.Param("production_cycles", production_cycles);
            }
        }

        #endregion // Fabrication

        private void LogLevelMenuDisplayed()
        {
            m_Log.NewEvent("level_menu_displayed");
        }

        private void LogSelectLevel(string levelId)
        {
            using (var e = m_Log.NewEvent("select_level"))
            {
                e.Param("level_id", levelId);
            }
        }

        
        #endregion // Logging
    }
}