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

        public struct DesignIO
        {
            public List<GridCoord> Inputs;
            public List<GridCoord> Outputs;
        }

        public struct DesignGrid
        {
            public List<List<HashSet<ToolID>>> Grid;
        }

        // public  List<List<HashSet<ToolID>>> DesignGrid;

        private const ushort CLIENT_LOG_VERSION = 0;
        private readonly JsonBuilder m_JsonBuilder = new JsonBuilder(Unsafe.KiB * 64); // json allocation capacity
        private OGDLog m_Log;
        [NonSerialized] private bool m_Debug;

        #region Inspector
        [SerializeField, Required] private string m_AppId = "SPACEFAB";
        [SerializeField, Required] private string m_AppVersion;
        [SerializeField] private FirebaseConsts m_Firebase;
        [SerializeField] private bool m_Testing;

        //[NonSerialized] private float m_SecondsFromLaunch;
        [NonSerialized] private string m_CurrentContract;
        [NonSerialized] private Minigame? m_CurrentMinigame;
        [NonSerialized] private Dictionary<string, Dictionary<Minigame, int>> m_ContractLevels = new Dictionary<string, Dictionary<Minigame, int>>(); // number of levels of each minigame, specified by the contract
        [NonSerialized] private DesignGrid? m_DesignGrid = null;

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
                //.Field("seconds_from_launch", m_SecondsFromLaunch)
                .Field("current_contract", m_CurrentContract)
                .Field("current_minigame", m_CurrentMinigame.HasValue ? m_CurrentMinigame.Value.ToString() : null);
            
            m_JsonBuilder.BeginObject("contract_levels"); // key: contract
            if (m_ContractLevels != null)
            {
                foreach (var kvp in m_ContractLevels)
                {
                    m_JsonBuilder.BeginObject(kvp.Key); // key: minigame
                    foreach (var minigameKvp in kvp.Value)
                    {
                        m_JsonBuilder.Field(minigameKvp.Key.ToString(), minigameKvp.Value); // value: number of levels
                    }
                    m_JsonBuilder.EndObject();
                }
            }
            m_JsonBuilder.EndObject();

            if (m_DesignGrid.HasValue)
            {
                m_JsonBuilder.BeginArray("design_grid");
                m_DesignGrid.Value.Grid.ForEach(row => row.ForEach(cell =>
                {
                    m_JsonBuilder.BeginArray();
                    foreach (var toolId in cell)
                    {
                        m_JsonBuilder.Item(toolId.ToString());
                    }
                    m_JsonBuilder.EndArray();
                }));
                m_JsonBuilder.EndArray();
            }
            else
            {
                m_JsonBuilder.Field("design_grid", (string) null);
            }
            
            m_Log.GameState(m_JsonBuilder.End());
        }

        private void UpdateContractLevels(string contract, Minigame minigame, int numLevel)
        {
            m_ContractLevels[contract][minigame] += numLevel ;
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
            m_Log.NewEvent("session_start");
            m_Log.Initialize(CreateOGDConsts());
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
            SpacefabGame.Events
                .Register<bool>(GameEvents.TitleStartGameClicked, LogGameStart)
                .Register(GameEvents.TitleNewGameClicked, LogClickNewGame)
                .Register(GameEvents.TitleBackFromInputClicked, LogClickResumeGame)
                .Register(GameEvents.OpenContractView, LogOpenContractView)
                .Register<string>(GameEvents.ConfirmSelectContract, LogAcceptContract)
                .Register<string>(GameEvents.StartChangeContract, LogStartChangeContract)
                .Register<string>(GameEvents.ConfirmChangeContract, LogConfirmChangeContract)
                .Register<string>(GameEvents.CancelChangeContract, LogCancelChangeContract)
                .Register<int>(GameEvents.SelectMinigame, HandleMinigameSelect)
                .Register<int>(GameEvents.StartMinigame, HandleMinigameStart);

            // Design
            SpacefabGame.Events
                .Register<GridStackConfig>(GameEvents.DesignGridModified, UpdateDesignGridState);

            // Fabrication
            SpacefabGame.Events
                .Register(GameEvents.FabActivateStation, (string stationId) => LogActivateStation(stationId))
                .Register<(string, string)>(GameEvents.FabInvalidActivateStation, LogInvalidActivation);
        }
        #endregion

        #region Logging Variables

        [NonSerialized] private string m_CurrentContractId;

        #endregion // Logging Variables

        #region State Handlers

        private void UpdateDesignGridState(GridStackConfig config)
        {
            // initialize design grid for horizontal slice
            if (m_DesignGrid == null)
            {
                m_DesignGrid = new DesignGrid()
                {
                    Grid = new List<List<HashSet<ToolID>>>()
                };
                for (int i = 0; i < config.LayerDims.X; i++)
                {
                    List<HashSet<ToolID>> row = new List<HashSet<ToolID>>();
                    for (int j = 0; j < config.LayerDims.Y; j++)
                    {
                        row.Add(new HashSet<ToolID>());
                    }
                    m_DesignGrid.Value.Grid.Add(row);
                }
            }

            for (int row = 0; row < config.LayerDims.X; row++)
            {
                for (int col = 0; col < config.LayerDims.Y; col++)
                {
                    if (config.Cells[row * config.LayerDims.X + col].LayerIndex == (int)Layer.METAL)
                    {
                        GridCellConfig cell = config.Cells[row * config.LayerDims.X + col];
                        if (cell.CellType != CellType.NONE)
                        {
                            switch (cell.CellType)
                            {
                                case CellType.Metal:
                                    m_DesignGrid.Value.Grid[row][col].Add(ToolID.METAL);
                                    break;
                                case CellType.PTransistor:
                                    m_DesignGrid.Value.Grid[row][col].Add(ToolID.PTYPE);
                                    break;
                                case CellType.NTransistor:
                                    m_DesignGrid.Value.Grid[row][col].Add(ToolID.NTYPE);
                                    break;
                            }
                        }

                        if (cell.TransferType != TransferType.NONE) // Exclude TransferType.Implicit
                        {
                            if (cell.TransferType == TransferType.Via)
                            {
                                m_DesignGrid.Value.Grid[row][col].Add(ToolID.CONTACT);
                            }
                            else if (cell.TransferType == TransferType.GateAbove || cell.TransferType == TransferType.GateBelow)
                            {
                                m_DesignGrid.Value.Grid[row][col].Add(ToolID.GATE);
                            }
                        }
                    }
                }
            }
        }

        #endregion // State Handlers

        #region Logging

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

        private void LogAcceptContract(string contractId)
        /*
         "accept_contract": {
             "description": "When the player accepts a new contract.",
             "event_data": {
                "contract_id" : {
                   "type" : "str",
                   "description" : "The ID for the specific contract."
                },
                "contract_levels" : {
                   "type" : "Dict",
                   "description" : "JSON structure indicating the number of levels of each minigame, specified by the contract."
                }
             }
          }
         */
        {
            m_CurrentContractId = contractId;
            if (!m_ContractLevels.ContainsKey(contractId))
            {
                m_ContractLevels[contractId] = new Dictionary<Minigame, int>();
            }
            SubmitGameState();

            m_JsonBuilder.Begin();
            foreach (var kvp in m_ContractLevels)
            {
                m_JsonBuilder.BeginObject(kvp.Key); // key: minigame
                foreach (var minigameKvp in kvp.Value)
                {
                    m_JsonBuilder.Field(minigameKvp.Key.ToString(), minigameKvp.Value); // value: number of levels
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

        private void LogOpenContractView()
        {
            m_Log.NewEvent("open_contract_view");
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

        private void HandleMinigameSelect(int zoneIndex)
        {
            switch(zoneIndex)
            {
                case 0:
                    LogSelectSupplyChain();
                    break;
                case 1:
                    LogSelectDesign();
                    break;
                case 2:
                    LogSelectFabrication();
                    break;
                case 3:
                    LogSelectResearch();
                    break;
                default:
                    Log.Msg("[Logging] Unrecognized minigame zone index: {0}", zoneIndex);
                    break;
            }
        }

        // Ensure the indices, which is different from minigame zone select
        public void HandleMinigameStart(int zoneIndex)
        {
            switch(zoneIndex)
            {
                case 0:
                    LogStartResearch();
                    break;
                case 1:
                    LogStartDesign();
                    break;
                case 2:
                    LogStartSupplyChain();
                    break;
                case 3:
                    LogStartFabrication();
                    break;
                default:
                    Log.Msg("[Logging] Unrecognized minigame zone index: {0}", zoneIndex);
                    break;
            }
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


        private void LogDesignLevelBegin(GridStackConfig config)
            /*
             "design_level_begin": {
                 "description": "When the player begins a new level in teh design minigame.",
                 "event_data": {
                    "initial_board_state" : {
                       "type" : "DesignGrid",
                       "description" : "The initial state of the design grid, when the player began the level."
                    },
                    "inputs" : {
                       "type" : "TBD",
                       "description" : "The inputs. Not 100% sure what this was. Possibly the coordinates of the input points?"
                    },
                    "outputs" : {
                       "type" : "TBD",
                       "description" : "The outputs. Not 100% sure what this was. Possibly the coordinates of the output point?"
                    }
                 }
              }
             */
        {
            UpdateDesignGridState(config);
            SubmitGameState();

            m_JsonBuilder.Begin();
            m_DesignGrid.Value.Grid.ForEach(row => row.ForEach(cell => 
                { 
                    m_JsonBuilder.BeginArray(); foreach (var toolId in cell) 
                    { 
                        m_JsonBuilder.Item(toolId.ToString()); 
                    } 
                    m_JsonBuilder.EndArray(); 
                }));
            string gridJson = m_JsonBuilder.End().ToString();

            List<(int, int)> inputs = new List<(int, int)>();
            List<(int, int)> outputs = new List<(int, int)>();
            for (int i = 0; i < config.LayerDims.X; i++)
            {
                for (int j = 0; j < config.LayerDims.Y; j++)
                {
                    GridCellConfig cell = config.Cells[i * config.LayerDims.X + j];
                    if (cell.CellType == CellType.Input)
                    {
                        inputs.Add((i, j));
                    }
                }
            }
        }

        private void LogSelectTool(ToolID toolId)
        {
            using (var e = m_Log.NewEvent("select_tool"))
            {
                e.Param("tool_id", toolId.ToString());
            }
        }

        private void LogFillGrid(ToolID toolId, GridCoord coordinate)
        {
            // TODO
            using (var e = m_Log.NewEvent("fill_grid"))
            {
                e.Param("tool_id", toolId.ToString());
                e.Param("x", coordinate.Row);
                e.Param("y", coordinate.Col);
                e.Param("layer", coordinate.Layer.ToString());
            }
        }

        private void LogSubmitDesign(List<GridCoord> inputs, List<GridCoord> outputs) // ? inputs outputs type
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

        private void LogShipMenuDisplayed()
        {
            m_Log.NewEvent("ship_menu_displayed");
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

        private void LogInvalidActivation((string, string) stationIds)
        /*
         "invalid_activation": {
             "description": "When the player attempted to activate the wrong station.",
             "event_data": {
                "station_id" : {
                   "type" : "str",
                   "description" : "The ID of the station the player activated."
                },
                "next_station" : {
                   "type" : "str",
                   "description" : "The actual next station the player was supposed to activate."
                }
             }
          }
         */
        {
            using (var e = m_Log.NewEvent("invalid_activation"))
            {
                e.Param("station_id", stationIds.Item1);
                e.Param("next_station", stationIds.Item2);
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

        private void LogFabricationSuccess(float accuracy, float time, int productionCycles)
        {
            using (var e = m_Log.NewEvent("fabrication_success"))
            {
                e.Param("accuracy", accuracy);
                e.Param("time", time);
                e.Param("production_cycles", productionCycles);
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