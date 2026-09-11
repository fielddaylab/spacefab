using System;
using BeauUtil;
using BeauUtil.UI;
using FieldDay;
using FieldDay.SharedState;
using FieldDay.UI;
using SpaceFab.Save;
using SpaceFab.UI;
using UnityEngine;

namespace SpaceFab.Research {
    public sealed class ResearchResultPanel : SharedPanel {
        public DynamicButton NextButton;
        public SceneReference NextScene;
        public ContractRequirementTable Table;

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
            Input.TryPushPriority();
        }

        // Pops the priority pushed by AcquirePriority, if held. Skipped during shutdown (the GuiMgr
        // stack is being torn down anyway and the layer may already be invalid).
        private void ReleasePriority() {
            Input.TryPopPriority();
        }

        public override void Show() {
            base.Show();

            AcquirePriority();
            Input.SetInputOverride(null);

            Find.State(out ChapterState chapterState, out ContractState contractState, out ResearchMinigameState researchState);

            ContractUIUtility.BuildRequirementData(Table, chapterState.ChapterIndex, contractState.ContractDefinition);
            ContractUIUtility.InitializeVisuals(Table);
            ContractUIUtility.UpdateVisualsWithCompletion(Table, researchState.SandboxProperties);
            Positioning.SetHeightDelta((RectTransform)Table.transform.parent, Table.Sizer.LastSize.y + 48);
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