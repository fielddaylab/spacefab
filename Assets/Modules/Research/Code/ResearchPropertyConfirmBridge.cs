using BeauUtil;
using FieldDay;
using SpaceFab.Materials;
using SpaceFab.UI;

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
        public static void HandleConfirmedProperty(ResearchMinigameState researchState, StringHash32 materialId, MaterialPropertyLabel propertyLabel, StringHash32 contextMaterialId)
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

            // Unlock the corresponding material's wiki page on first
            // confirmation. Material wiki pages are authored with the
            // same asset name as the MaterialAsset, so the material
            // id is the page id. UnlockPage short-circuits on
            // duplicates (HashSet.Add returns false), so this is a
            // no-op once the page is already unlocked. Wiki unlock
            // is account-scoped and writes through to
            // PlayerProgressState immediately, even though the
            // sandbox property only commits at minigame exit — the
            // wiki page being visible is part of "this material
            // exists" knowledge, not the per-property record.
            PlayerProgressState progressState = Find.State<PlayerProgressState>();
            if (progressState != null)
            {
                WikiUtility.UnlockPage(progressState, materialId);
            }
        }
    }
}
