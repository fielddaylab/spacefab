using BeauUtil;
using FieldDay;
using SpaceFab.Materials;
using System.Collections.Generic;
using UnityEngine;

namespace SpaceFab.Research {
    /// <summary>
    /// Builds the hypothesis page list for the active contract. Called from
    /// ResearchTransitionSystem on minigame entry once RequiredResearchGoals
    /// has been populated. Each goal expands to one page per registered
    /// MaterialPropertyDefinition matching the goal's label, so the
    /// paginator exposes alternate satisfaction paths as separate pages.
    /// </summary>
    public static class ResearchHypothesisUtility {
        // Rebuilds pagesState.Pages from researchState.RequiredResearchGoals
        // and the global property-definition registry. Resets the active
        // index on the viewmodel to 0. Pages without any registered
        // definition for their label are skipped with a warning — the
        // contract is unsatisfiable until a definition is authored.
        public static void BuildPages(
            ResearchMinigameState researchState,
            ResearchHypothesisPagesState pagesState,
            HypothesisViewModelState viewModelState
        ) {
            pagesState.Pages.Clear();

            MaterialPropertyDefinitionAsset registry = Find.GlobalAsset<MaterialPropertyDefinitionAsset>();
            if (registry == null) {
                Debug.LogWarning("[ResearchHypothesisUtility] No MaterialPropertyDefinitionAsset registered; hypothesis panel will be empty.");
                viewModelState.ActivePageIndex = 0;
                HypothesisViewModelUtility.RequestRebuild(viewModelState);
                return;
            }

            List<MaterialObservationEntry> scratch = new List<MaterialObservationEntry>(8);

            MaterialPropertyCheck[] goals = researchState.RequiredResearchGoals;
            if (goals != null) {
                for (int g = 0; g < goals.Length; g++) {
                    MaterialPropertyCheck goal = goals[g];
                    if (goal == null) {
                        continue;
                    }

                    MaterialPropertyDefinition[] defs = registry.GetDefinitions(goal.Label);
                    if (defs.Length == 0) {
                        Debug.LogWarningFormat("[ResearchHypothesisUtility] No MaterialPropertyDefinition registered for goal label '{0}'.", goal.Label);
                        continue;
                    }

                    for (int d = 0; d < defs.Length; d++) {
                        scratch.Clear();
                        MaterialPropertyDefinitionUtility.DecomposeToObservations(defs[d], goal.InComparisonTo, scratch);

                        HypothesisPage page;
                        page.Label = goal.Label;
                        page.Context = goal.InComparisonTo;
                        page.Definition = defs[d];
                        page.DecomposedObservations = scratch.ToArray();
                        pagesState.Pages.Add(page);
                    }
                }
            }

            viewModelState.ActivePageIndex = 0;
            HypothesisViewModelUtility.RequestRebuild(viewModelState);
        }
    }
}
