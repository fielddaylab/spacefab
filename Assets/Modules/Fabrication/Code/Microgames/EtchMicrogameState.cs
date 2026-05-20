using FieldDay;
using FieldDay.SharedState;
using SpaceFab.Fabrication.Layout;
using SpaceFab.Fabrication.Sequence;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace SpaceFab.Fabrication.Microgames
{
    public enum EtchMicrogamePhase
    {
        Idle,
        Entering,
        Active,
        Exiting
    }

    /// <summary>
    /// Holds in-flight data for the Plasma Etcher ("Etch-a-sketch") microgame: the beam's
    /// position over the pattern, the per-cell correct/incorrect tally, and lifecycle flags
    /// consumed by EtchMicrogameSystem.
    /// </summary>
    public class EtchMicrogameState : SharedStateComponent
    {
        // True while this microgame owns input/simulation. Set by EnterBegin, cleared by ExitComplete.
        // EtchMicrogameSystem reads this to gate its ProcessWork.
        [HideInInspector] public bool IsActive;
        [HideInInspector] public bool InputAccepted;
        public GameObject EtchUI;
        public EtchMicrogamePhase Phase;

        public LineRenderer PreviewBeam;
        public LineRenderer PlayerBeam;

        [HideInInspector] public readonly List<Vector3> PreviewPoints = new();
        [HideInInspector] public readonly List<Vector3> PlayerPoints = new();
        [HideInInspector] public Vector3[] CachedPreviewPoints;
        [HideInInspector] public Vector2 Direction;

        [HideInInspector] public int PreviewVisibleCount;
        [HideInInspector] public float PreviewProgress;
    }

    /// <summary>
    /// Paired utility for EtchMicrogameState. Drives the Plasma Etcher microgame's lifecycle hooks
    /// invoked from EtchMicrogame (the Unity-side IMicrogame component).
    /// </summary>
    public static class EtchMicrogameUtility
    {
        public static bool CanActivate()
        {
            // TODO: gate based on sequence / wafer state. Default true.
            return true;
        }

        public static void EnterBegin()
        {
            Find.State(out EtchMicrogameState state);

            state.Phase = EtchMicrogamePhase.Entering;
            state.IsActive = true;
            state.InputAccepted = false;
            state.EtchUI.SetActive(true);

            state.Direction = Vector2.right;

            if (state.PreviewPoints.Count == 0)
            {
                int previewCount = state.PreviewBeam.positionCount;
                for (int i = 0; i < previewCount; i++)
                {
                    state.PreviewPoints.Add(state.PreviewBeam.GetPosition(i));
                }
            }
  
            state.PreviewProgress = 0f;
            state.PreviewVisibleCount = 0;
            state.PreviewBeam.positionCount = 0;
            
            state.PlayerPoints.Clear();
            state.PlayerBeam.positionCount = 0;
        }

        public static void EnterComplete()
        {
            Find.State(out EtchMicrogameState state);

            state.Phase = EtchMicrogamePhase.Active;
            state.InputAccepted = true;

            Vector3 start = state.PreviewPoints[0];
            state.PlayerPoints.Add(start);
            state.PlayerBeam.positionCount = 1;
            state.PlayerBeam.SetPosition(0, start);
        }

        // On normal completion, compute precision and commit it to the wafer at the current step.
        // On cancel, nothing is recorded.
        public static void ExitBegin(bool completedNormally)
        {
            Find.State(out EtchMicrogameState state);
            state.Phase = EtchMicrogamePhase.Exiting;
            if (!completedNormally) { return; }

            MicrogameUtility.CommitStepPrecision(ComputePrecision());
        }

        // TODO: track process animation state (parallel or sequential) and return true once the
        // animation has finished playing. Scaffold returns true so the exit gate doesn't stall
        // before per-microgame animations are authored.
        public static bool IsProcessAnimationComplete()
        {
            return true;
        }

        public static void ExitComplete()
        {
            Find.State(out EtchMicrogameState state, out MicrogameCanvasState canvasState);

            state.IsActive = false;
            state.Phase = EtchMicrogamePhase.Idle;
            state.EtchUI.SetActive(false);

            state.PlayerPoints.Clear();
            state.PlayerBeam.positionCount = 0;

            canvasState.HideUI();
        }

        // Etch-a-sketch-specific precision math: fraction of target-pattern cells the beam
        // correctly traversed, minus cells incorrectly traversed. Scaffold returns 0.
        private static float ComputePrecision()
        {
            Find.State(out EtchMicrogameState state);

            if (state.PlayerPoints.Count == 0) { return 0f; }

            float playerToPreview = AverageDistance(state.PlayerPoints, state.PreviewBeam);
            float previewToPlayer = AverageDistance(state.PreviewPoints, state.PlayerBeam);
            float previewToPlayerWeight = 0.7f; // weight on missing target points is higher than extra points off-target

            float totalError = (playerToPreview * (1 - previewToPlayerWeight) + previewToPlayer * previewToPlayerWeight) * 0.5f;
            float precision = 1f - totalError;

            return Mathf.Clamp01(precision);
            }

        private static float AverageDistance(List<Vector3> points, LineRenderer targetLine)
        {
            if (points.Count == 0)
                return 1f;

            float total = 0f;

            for (int i = 0; i < points.Count; i++)
            {
                total += DistanceToPreview(points[i], targetLine);
            }

            return total / points.Count;
        }

        private static float DistanceToPreview(Vector3 point, LineRenderer line)
        {
            int count = line.positionCount;
            if (count <= 0)
                return 0f;

            if (count == 1)
                return Vector3.Distance(point, line.GetPosition(0));

            float bestSqr = float.MaxValue;
            Vector3 prev = line.GetPosition(0);

            for (int i = 1; i < count; i++)
            {
                Vector3 next = line.GetPosition(i);
                float sqr = PointToSegmentDistanceSqr(point, prev, next);
                if (sqr < bestSqr)
                    bestSqr = sqr;
                prev = next;
            }

            return Mathf.Sqrt(bestSqr);
        }

        private static float PointToSegmentDistanceSqr(Vector3 p, Vector3 a, Vector3 b)
        {
            Vector3 ab = b - a;
            float abSqr = Vector3.Dot(ab, ab);

            if (abSqr <= 0.000001f)
                return (p - a).sqrMagnitude;

            float t = Vector3.Dot(p - a, ab) / abSqr;
            t = Mathf.Clamp01(t);

            Vector3 projection = a + t * ab;
            return (p - projection).sqrMagnitude;
        }
    }
}
