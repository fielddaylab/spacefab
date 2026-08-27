using BeauUtil;
using FieldDay;
using FieldDay.Systems;
using SpaceFab;
using SpaceFab.Materials;
using SpaceFab.UI;

namespace SpaceFab.Research {
    /// <summary>
    /// Renders the Contract Requirements list against the contract's
    /// research goals. LateUpdate order 500,
    /// alongside the sample panel's render and after the confirm path
    /// at order 60 has flipped its sandbox bit.
    /// </summary>
    public class ContractRequirementsVisualSystem : SystemComponent {
        public override unsafe void RegisterSystems(ref SystemRegistrationTable ecs) {
            ecs.Register(&ProcessWork,
                new SysUpdate(GameLoopPhase.LateUpdate, 500, UpdateMasks.ResearchMask),
                new SysPermissions()
                    .ReadShared<ResearchMinigameState>()
                    .ReadShared<PlayerProgressState>()
                    .ReadWriteShared<ResearchContractRequirementsPanelState>()
            );
        }

        private static void ProcessWork(float deltaTime) {
            if (!Game.SharedState.Has<ResearchContractRequirementsPanelState>()) {
                return;
            }

            Find.State(
                out ResearchContractRequirementsPanelState panel,
                out ResearchMinigameState researchState,
                out PlayerProgressState progressState
            );

            if (!panel.NeedsRefresh && !researchState.PropertyConfirmedThisFrame) {
                return;
            }

            ContractRequirementsVisualUtility.Apply(panel, researchState, progressState);
            panel.NeedsRefresh = false;
        }
    }

    /// <summary>
    /// Pushes contract-goal progress into the requirements panel's
    /// authored rows. Invoked only when a refresh has been requested (see
    /// ContractRequirementsVisualSystem).
    /// </summary>
    public static class ContractRequirementsVisualUtility {
        public static void Apply(ResearchContractRequirementsPanelState panel, ResearchMinigameState researchState, PlayerProgressState progressState) {
            if (panel == null || panel.Rows == null || researchState == null || progressState == null) {
                return;
            }

            MaterialPropertyCheck[] goals = researchState.RequiredResearchGoals;
            int goalCount = goals != null ? goals.Length : 0;

            // Resolved once per pass rather than per row. Absent in a scene that doesn't ship the
            // wiki prefab, which just leaves every row inert.
            var contents = Find.Components<WikiContent>();
            WikiContent wikiContent = contents.Count > 0 ? contents[0] : null;

            for (int i = 0; i < panel.Rows.Length; i++) {
                ResearchContractRequirementRow row = panel.Rows[i];
                if (row == null) {
                    continue;
                }

                // Handlers are rebound every pass, so drop the prior one first — otherwise a
                // refresh stacks a second registration on the same chip.
                ClearRowClick(row);

                MaterialPropertyCheck goal = i < goalCount ? goals[i] : null;
                if (goal == null) {
                    row.gameObject.SetActive(false);
                    continue;
                }
                row.gameObject.SetActive(true);
                if (row.Chip == null) {
                    continue;
                }

                // A goal is met when some material's merged (sandbox OR
                // saved) record satisfies it. A goal that names a substrate
                // is context-exact — "P-Type dopant for sample A" is not met
                // by a dopant confirmed for a different one. A goal with no
                // substrate is a wildcard; see SatisfiesCheck.
                bool fulfilled = ContractProgressUtility.HasAnyFulfillingMaterial(progressState, researchState, goal);

                // Goal labels are persistent properties, so the chamber
                // lookup resolves them to the ConfirmedProperty sprite
                // bucket — the same frame the sample panel's hypothesis
                // chip wears. A met goal takes that bucket's confirmed
                // sprite, which carries the green checkmark.
                row.Chip.SetState(BuildRequirementText(goal),
                    fulfilled ? ChipFillState.Confirmed : ChipFillState.Filled, false,
                    MaterialObservationChamberLookup.GetChamberType(goal.Label));

                BindRowClick(row, goal, wikiContent, progressState);
            }
        }

        // Example text: "P-Type dopant for sample A", plus the
        // substrate it is measured against when the goal names one.
        private static string BuildRequirementText(MaterialPropertyCheck goal) {
            string text = MaterialPropertyLabelDisplay.GetPropertyName(goal.Label);
            if (goal.InComparisonTo.IsEmpty) {
                return text;
            }
            // TODO: show actual name for known materials
            ResearchMaterialView contextView = Find.NamedAsset<ResearchMaterialView>(goal.InComparisonTo);
            return contextView != null ? $"{text} for sample {contextView.SampleLabel}" : text;
        }

        // Registers the row's chip to open the wiki on the goal's property page. Left unbound when
        // no page covers the property or the page is still locked — OpenTo would drop the request
        // at SelectPageById anyway, and an inert row is better than one that looks live and does
        // nothing.
        private static void BindRowClick(ResearchContractRequirementRow row, MaterialPropertyCheck goal, WikiContent wikiContent, PlayerProgressState progressState)
        {
            if (wikiContent == null || row.Chip.Click == null)
            {
                return;
            }
            if (!WikiUtility.TryFindPropertyPage(wikiContent, goal.Label, out StringHash32 tabId, out StringHash32 pageId))
            {
                return;
            }
            if (!WikiUtility.IsPageUnlocked(progressState, pageId))
            {
                return;
            }

            // Ids captured at bind time — the click resolves nothing, so a content change can't
            // land it on a different page than the one this pass validated.
            row.WikiClickHandler = () => WikiUtility.OpenTo(tabId, pageId);
            row.Chip.Click.onClick.Register(row.WikiClickHandler);
        }

        private static void ClearRowClick(ResearchContractRequirementRow row)
        {
            if (row.WikiClickHandler != null && row.Chip != null && row.Chip.Click != null)
            {
                row.Chip.Click.onClick.Deregister(row.WikiClickHandler);
            }
            row.WikiClickHandler = null;
        }
    }
}
