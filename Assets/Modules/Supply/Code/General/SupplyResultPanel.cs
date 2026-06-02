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

        public override void Show() {
            base.Show();

            Game.Gui.PushPriority(Input);
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

            Game.Gui.PopPriority(Input);
            Input.SetInputOverride(false);
        }
    }
}