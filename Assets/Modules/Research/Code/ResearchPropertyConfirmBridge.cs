using BeauUtil;
using SpaceFab.Materials;

namespace SpaceFab.Research
{
    /// <summary>
    /// Single seam where confirmed-hypothesis events land on the in-session
    /// sandbox. ResearchInventoryUtility.TryConfirmHypothesis calls into here
    /// after running the dependency evaluator and consuming the supporting
    /// observations. This is the only place runtime confirmations are written
    /// to ResearchMinigameState.SandboxProperties; PlayerProgressState is not
    /// touched until minigame exit via ResearchStateUtility.CommitToPlayerProgress.
    /// </summary>
    public static class ResearchPropertyConfirmBridge
    {
        /// <summary>
        /// Records a confirmed property on the sandbox. Observation-only
        /// labels are silently filtered out by ResearchStateUtility.Confirm -
        /// they are evidence, not confirmable Properties. For dynamic labels
        /// (PDopantFor / NDopantFor), contextMaterialId names the X material
        /// the property is parameterized by; the call no-ops if context is
        /// empty (the UI should not surface a dynamic property as confirmable
        /// until a context material is set).
        /// </summary>
        public static void HandleConfirmedProperty(ResearchMinigameState researchState, PlayerProgressState progressState, StringHash32 materialId, MaterialPropertyLabel propertyLabel, StringHash32 contextMaterialId)
        {
            if (researchState == null)
            {
                return;
            }

            if (MaterialPropertyLabelUtility.IsDynamic(propertyLabel) && contextMaterialId.IsEmpty)
            {
                return;
            }

            ResearchStateUtility.Confirm(researchState, materialId, propertyLabel, contextMaterialId);

            // Frame-flag for downstream view refreshes (e.g., the tray
            // rig labels flipping from sample number to ShortName once
            // the material becomes "known"). Idempotent confirms still
            // raise it — the consumers are cheap and the cost is one
            // pass over a small set per confirmation.
            researchState.PropertyConfirmedThisFrame = true;

            // Once the sandbox + player progress fully cover the active contract's required
            // properties, flip FoundValidSolution. Monotonic: never flips back to false within a
            // session. The contract-accept flow performs the equivalent pre-arming for the case
            // where existing player progress already covers the requirements.
            ResearchStateUtility.RefreshFoundValidSolutionFromActiveContract(researchState, progressState);
        }
    }
}
