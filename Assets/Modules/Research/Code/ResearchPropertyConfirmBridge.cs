using BeauUtil;
using FieldDay;
using SpaceFab.Materials;

namespace SpaceFab.Research
{
    /// <summary>
    /// Single bridge between the runtime Research chip vocabulary and the
    /// in-session sandbox on ResearchMinigameState. The minigame's hypothesis-
    /// confirm flow funnels through HandleConfirmedProperty(); this is the only
    /// place that records runtime confirmations into the sandbox. The sandbox
    /// is the destination - PlayerProgressState is not touched until minigame
    /// exit (ResearchStateUtility.CommitToPlayerProgress).
    /// </summary>
    public static class ResearchPropertyConfirmBridge
    {
        /// <summary>
        /// Called when the player confirms a hypothesis in the Research minigame.
        /// Records the confirmation on the minigame sandbox. Observation-only
        /// labels are silently filtered out by ResearchStateUtility.Confirm -
        /// they are evidence, not confirmable Properties. For dynamic labels
        /// (PDopantFor / NDopantFor), contextMaterialId names the X material
        /// the property is parameterized by.
        /// </summary>
        public static void HandleConfirmedProperty(StringHash32 materialId, MaterialPropertyLabel propertyLabel, StringHash32 contextMaterialId)
        {
            ResearchMinigameState researchState = Find.State<ResearchMinigameState>();
            if (researchState == null)
            {
                return;
            }

            // TODO: dynamic property without a context material is a programmer
            // error. Once the prototype's hypothesis-submit flow is ported, decide
            // whether to assert here or quietly drop. The hypothesis UI should
            // never surface a dynamic property as confirmable until a context is
            // set, so this branch should be unreachable in practice.
            if (MaterialPropertyLabelUtility.IsDynamic(propertyLabel) && contextMaterialId.IsEmpty)
            {
                return;
            }

            ResearchStateUtility.Confirm(researchState, materialId, propertyLabel, contextMaterialId);
        }

        // TODO: once the prototype's ResearchChipId enum and Event_KnowledgeUpdated
        // event are ported into the production codebase, register a single
        // [InvokeOnBoot] listener here that:
        //   1. Reads the just-confirmed ResearchChipId and its context StringHash32.
        //   2. Calls ResearchChipUtility.Unalias and ResearchChipUtility.IsProperty
        //      to filter to Property chips only.
        //   3. Translates the chip to its MaterialPropertyLabel via
        //      ResearchPropertyMapper.ToPropertyLabel(chip).
        //   4. Calls HandleConfirmedProperty(materialId, label, contextMaterialId).
        //
        // Keeping the listener thin and pointed at HandleConfirmedProperty - rather
        // than letting the prototype's UI controllers write directly to the sandbox
        // - is what makes this the single greppable seam between the two vocabularies.
    }
}
