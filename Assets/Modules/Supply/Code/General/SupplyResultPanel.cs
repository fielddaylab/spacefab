using System;
using BeauUtil;
using BeauUtil.UI;
using FieldDay;
using FieldDay.SharedState;
using FieldDay.UI;
using SpaceFab.Save;
using SpaceFab.UI;
using UnityEngine;

namespace SpaceFab.Supply {
    public sealed class SupplyResultPanel : SharedPanel {
        public DynamicButton NextButton;
        public SceneReference NextScene;

        [Header("Ships")]
        public SupplyProgressMeterView MeterView;
        public SupplyShipBreakdownRow[] ShipRows;

        // Tracks whether this panel currently holds a pushed GUI input priority. Pushing is balanced
        // against popping through OnDisable so the priority is always released when the panel goes
        // away — including when the Commit button tears the minigame down without Hide() running.
        // A leaked PushPriority survives on the persistent GuiMgr stack and disables every
        // lower-priority layer in the next scene (overarching), which reads as "all input dead".
        [NonSerialized] private bool m_PriorityPushed;

        protected override void Awake() {
            base.Awake();

            NextButton.onClick.AddListener(Commit);
        }

        private void Start() {
            Hide();
        }

        private void Commit() {
            Find.State<MinigameRequestExitState>().ExitRequestState = RequestState.Confirmed;
        }

        // Safety net for the pushed priority: OnDisable fires both on Hide() (SetActive(false)) and
        // on scene-teardown deactivation, and runs before OnDestroy invalidates the input layer, so
        // PopPriority's not-destroyed assert still holds. This is what catches the Commit-then-exit
        // path where Hide() never runs.
        private void OnDisable() {
            ReleasePriority();
        }

        // Pushes GUI input priority for this panel, at most once.
        private void AcquirePriority() {
            if (m_PriorityPushed) { return; }
            m_PriorityPushed = true;
            Game.Gui.PushPriority(Input);
        }

        // Pops the priority pushed by AcquirePriority, if held. Skipped during shutdown (the GuiMgr
        // stack is being torn down anyway and the layer may already be invalid).
        private void ReleasePriority() {
            if (!m_PriorityPushed) { return; }
            m_PriorityPushed = false;
            if (!Game.IsShuttingDown) {
                Game.Gui.PopPriority(Input);
            }
        }

        public override void Show() {
            base.Show();

            AcquirePriority();
            Input.SetInputOverride(null);

            Find.State(out SupplyRouteCollection routes, out SupplyMinigameState minigameState, out SupplyShipIndex ships);

            int cost = 0,
                time = 0,
                risk = 0;

            for (int i = 0; i < ships.ShipCount; i++) {
                cost += routes.RouteStats[i].Cost;
                time = Math.Max(time, routes.RouteStats[i].Time);
                risk += routes.RouteStats[i].Risk;
            }

            minigameState.Cost = cost;
            minigameState.TotalCycles = time;
            minigameState.Reliability = risk;

            minigameState.FoundValidSolution = true;

            ref SupplySaveState saveState = ref Find.State<MinigameSaveStates>().Supply;
            SupplyStateUtility.ExportState(ref saveState, minigameState);

            Populate(minigameState);
        }

        private void Populate(SupplyMinigameState minigameState) {

        }

        public override void Hide() {
            base.Hide();

            // base.Hide() deactivates the GameObject, which fires OnDisable -> ReleasePriority; this
            // call is a guarded no-op in that case but keeps the Show/Hide pairing explicit.
            ReleasePriority();
            Input.SetInputOverride(false);
        }
    }
}