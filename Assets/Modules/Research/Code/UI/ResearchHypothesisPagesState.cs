using BeauUtil;
using FieldDay;
using FieldDay.SharedState;
using SpaceFab.Materials;
using System;
using System.Collections.Generic;

namespace SpaceFab.Research {
    /// <summary>
    /// One page of the hypothesis paginator. There is one HypothesisPage per
    /// (RequiredResearchGoal, MaterialPropertyDefinition) pair: if a goal's
    /// label has multiple registered definitions, the player gets one page
    /// for each. DecomposedObservations is the flat leaf set the player
    /// must collect against the slotted material to satisfy this page.
    /// </summary>
    public struct HypothesisPage {
        public MaterialPropertyLabel Label;
        public StringHash32 Context;
        public MaterialPropertyDefinition Definition;
        public MaterialObservationEntry[] DecomposedObservations;
    }

    /// <summary>
    /// Session-stable list of hypothesis pages for the active contract.
    /// Built once on minigame entry by ResearchHypothesisUtility.BuildPages
    /// (called from ResearchTransitionSystem); read by HypothesisViewModelSystem
    /// and HypothesisSubmitSystem.
    /// </summary>
    public class ResearchHypothesisPagesState : SharedStateComponent, IRegistrationCallbacks {
        [NonSerialized] public List<HypothesisPage> Pages;

        public void OnRegister() {
            Pages = new List<HypothesisPage>(4);
        }

        public void OnDeregister() {
            Pages?.Clear();
        }
    }
}
