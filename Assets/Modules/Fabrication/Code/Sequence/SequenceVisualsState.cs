using BeauRoutine;
using FieldDay;
using FieldDay.SharedState;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace SpaceFab.Fabrication.Sequence
{
    /// <summary>
    /// Holds the visuals layer for the Fabrication sequence: prefab and slot references for the
    /// on-screen step "cards", plus polled dirty flags written by SequenceUtility when the sequence
    /// resets, advances, or completes. Owned by the Fabrication minigame; consumed by
    /// SequenceVisualsSystem in LateUpdate.
    /// </summary>
    public class SequenceVisualsState : SharedStateComponent, IRegistrationCallbacks
    {
        // Prefab instantiated for each on-screen card. Authored on the scene state holder.
        public GameObject StepCardPrefab;

        // Parent transform for instantiated cards. Authored on the scene state holder.
        public Transform StepCardContainer;

        // Local position of the on-screen "current" card slot.
        public Vector3 CurrentSlotLocalPos;

        // Local position of the pre-loaded "next" card slot, sitting below the current slot.
        public Vector3 NextSlotLocalPos;

        // Local position the swipe-out animation targets (off-screen left).
        public Vector3 ExitLocalPos;

        // Tunable: how long the "completed" stamp visual holds before the swipe-left begins.
        public float StampDurationSeconds = 0.4f;

        // Tunable: duration of the swipe-left + next-card-up animation.
        public float SwipeDurationSeconds = 0.35f;

        // Currently on-screen card. Null until the first ResetRequested is consumed.
        [HideInInspector] public GameObject CurrentCard;

        // Pre-loaded card sitting at NextSlotLocalPos. Null when the current step is the last step.
        [HideInInspector] public GameObject NextCard;

        // BeauRoutine handle for the active stamp-and-swipe animation.
        [HideInInspector] public Routine TransitionRoutine;

        // Set by SequenceUtility.ResetSequence. Consumed by SequenceVisualsSystem to rebuild both cards.
        [HideInInspector] public bool ResetRequested;

        // Set by SequenceUtility.AdvanceStep on a non-final advance. Consumed by SequenceVisualsSystem
        // to play the stamp-and-swipe routine and pre-load the next-next card.
        [HideInInspector] public bool AdvanceRequested;

        // Set by SequenceUtility.AdvanceStep on the final advance. Consumed by SequenceVisualsSystem
        // to play the stamp-and-swipe routine without promoting a next card.
        [HideInInspector] public bool CompletionRequested;

        public void OnRegister()
        {
            CurrentCard = null;
            NextCard = null;
            ResetRequested = false;
            AdvanceRequested = false;
            CompletionRequested = false;
        }

        public void OnDeregister()
        {
            TransitionRoutine.Stop();
        }
    }

    /// <summary>
    /// Pairs with SequenceVisualsState. Builds and animates the on-screen step cards in response to
    /// sequence reset / advance / completion signals. The system consumes the dirty flags and calls
    /// these methods; SequenceUtility never calls them directly.
    /// </summary>
    public static class SequenceVisualsUtility
    {
        // Tears down any existing cards and instantiates fresh ones for the current step and the
        // step immediately after it (if in range). Called when ResetRequested is observed.
        public static void RebuildAllCards(SequenceVisualsState visualsState, SequenceState sequenceState)
        {
            // 1. Stop any in-flight animation; the rebuild supersedes it.
            visualsState.TransitionRoutine.Stop();

            // 2. Destroy any existing card instances.
            if (visualsState.CurrentCard != null) {
                Object.Destroy(visualsState.CurrentCard);
                visualsState.CurrentCard = null;
            }
            if (visualsState.NextCard != null) {
                Object.Destroy(visualsState.NextCard);
                visualsState.NextCard = null;
            }

            // 3. Bail if there is nothing to display (no level loaded, no steps).
            if (sequenceState.Level == null || sequenceState.Level.Steps == null || sequenceState.Level.Steps.Length == 0) {
                return;
            }

            int currentIndex = sequenceState.CurrentStepIndex;
            SequenceLookup lookup = Find.GlobalAsset<SequenceLookup>();

            // 4. Spawn the current card if the index is in range.
            if (currentIndex >= 0 && currentIndex < sequenceState.Level.Steps.Length) {
                visualsState.CurrentCard = SpawnCard(visualsState, sequenceState, currentIndex, visualsState.CurrentSlotLocalPos, lookup);
            }

            // 5. Spawn the next card if there is a step after the current one.
            int nextIndex = currentIndex + 1;
            if (nextIndex < sequenceState.Level.Steps.Length) {
                visualsState.NextCard = SpawnCard(visualsState, sequenceState, nextIndex, visualsState.NextSlotLocalPos, lookup);
            }
        }

        // Stamp-and-swipe routine for a non-final step advance. The card representing the
        // just-completed step is stamped "completed", then swipes off-screen left while the
        // pre-loaded next card slides up into the current slot. Once the swipe finishes, a fresh
        // next card is instantiated below for the new upcoming step (if one exists).
        public static IEnumerator AdvanceRoutine(SequenceVisualsState visualsState, SequenceState sequenceState, int justCompletedIndex)
        {
            // 1. Apply the "completed" stamp on the outgoing card. Placeholder until card prefab
            //    structure is finalized.
            ApplyCompletedStamp(visualsState.CurrentCard);

            // 2. Hold the stamp for the configured duration.
            yield return visualsState.StampDurationSeconds;

            // 3. Animate the outgoing card and the incoming card in parallel.
            yield return Routine.Combine(
                MoveCardLocal(visualsState.CurrentCard, visualsState.ExitLocalPos, visualsState.SwipeDurationSeconds),
                MoveCardLocal(visualsState.NextCard, visualsState.CurrentSlotLocalPos, visualsState.SwipeDurationSeconds)
            );

            // 4. Destroy the outgoing card and promote the incoming card to current.
            if (visualsState.CurrentCard != null) {
                Object.Destroy(visualsState.CurrentCard);
            }
            visualsState.CurrentCard = visualsState.NextCard;
            visualsState.NextCard = null;

            // 5. Pre-load a new next card for the step after the one we just promoted (if any).
            int newNextIndex = justCompletedIndex + 2;
            if (sequenceState.Level != null && sequenceState.Level.Steps != null && newNextIndex < sequenceState.Level.Steps.Length) {
                SequenceLookup lookup = Find.GlobalAsset<SequenceLookup>();
                visualsState.NextCard = SpawnCard(visualsState, sequenceState, newNextIndex, visualsState.NextSlotLocalPos, lookup);
            }
        }

        // Stamp-and-swipe routine for the final-step completion. Identical to AdvanceRoutine but
        // does not promote or pre-load any next card; both slots end empty.
        public static IEnumerator CompletionRoutine(SequenceVisualsState visualsState)
        {
            // 1. Apply the "completed" stamp on the final card.
            ApplyCompletedStamp(visualsState.CurrentCard);

            // 2. Hold the stamp.
            yield return visualsState.StampDurationSeconds;

            // 3. Swipe the final card off-screen.
            yield return MoveCardLocal(visualsState.CurrentCard, visualsState.ExitLocalPos, visualsState.SwipeDurationSeconds);

            // 4. Tear it down. NextCard is already null at completion.
            if (visualsState.CurrentCard != null) {
                Object.Destroy(visualsState.CurrentCard);
                visualsState.CurrentCard = null;
            }
        }

        // Instantiates a single card under StepCardContainer at the given local position and
        // populates its visuals from SequenceLookup.
        private static GameObject SpawnCard(SequenceVisualsState visualsState, SequenceState sequenceState, int stepIndex, Vector3 localPos, SequenceLookup lookup)
        {
            GameObject card = Object.Instantiate(visualsState.StepCardPrefab, visualsState.StepCardContainer);
            card.transform.localPosition = localPos;
            FabricationStep step = sequenceState.Level.Steps[stepIndex];
            StepRuntimeData runtime = sequenceState.StepRuntime != null && stepIndex < sequenceState.StepRuntime.Length
                ? sequenceState.StepRuntime[stepIndex]
                : default;
            PopulateCardVisuals(card, step, runtime, lookup);
            return card;
        }

        // Pulls per-step and per-chunk display data from SequenceLookup and applies it to the card.
        // Stub for now — the card prefab structure isn't authored yet. The signature is in place so
        // callers don't need to be updated when the visual hookup is filled in.
        private static void PopulateCardVisuals(GameObject card, FabricationStep step, StepRuntimeData runtime, SequenceLookup lookup)
        {
            // TODO: read SequenceStepEntry via lookup.GetStep(step.StepId), set the foreground sprite/text.
            // TODO: read SequenceChunkEntry via lookup.GetChunk(step.Chunk), set the background sprite/text.
            // TODO: if runtime.IsGlitched, apply lookup.GlitchOverlaySprite / GlitchOverlayText.
        }

        // Toggles the "completed" stamp visual on a card. Stub until the card prefab carries a
        // dedicated component for the stamp.
        private static void ApplyCompletedStamp(GameObject card)
        {
            // TODO: locate the stamp child renderer on the card prefab and enable it.
        }

        // Lerps a card's localPosition to the target over the given duration. Falls back to a
        // direct snap if the card was destroyed mid-routine.
        private static IEnumerator MoveCardLocal(GameObject card, Vector3 targetLocalPos, float duration)
        {
            if (card == null) {
                yield break;
            }
            Transform t = card.transform;
            Vector3 start = t.localPosition;
            float elapsed = 0f;
            while (elapsed < duration) {
                if (card == null) {
                    yield break;
                }
                elapsed += Routine.DeltaTime;
                float k = Mathf.Clamp01(elapsed / duration);
                t.localPosition = Vector3.Lerp(start, targetLocalPos, k);
                yield return null;
            }
            if (card != null) {
                t.localPosition = targetLocalPos;
            }
        }
    }
}
