using FieldDay.SharedState;
using FieldDay.Systems;
using FieldDay;
using System.Collections.Generic;
using UnityEngine;

namespace SpaceFab.Design
{
    /// <summary>
    /// Minimal placeholder for a crucial node in the evaluation graph. Fields will grow as the
    /// graph-construction and flow-propagation logic is ported from the prototype's EvaluationMgr.
    /// </summary>
    public struct CrucialNode
    {
        public GridCoord Coord;
        public int EvalDepth;
        // TODO: CurrFlowState, TempTransformedType, NoReturnList, etc. — port from prototype.
    }

    /// <summary>
    /// Minimal placeholder for a crucial edge in the evaluation graph. One entry per depth-sorted
    /// edge; DepthStepSystem walks the subset at runState.CurrentDepth on paint frames.
    /// </summary>
    public struct CrucialEdge
    {
        public GridCoord OriginCoord;
        public GridCoord OtherCoord;
        public int EvalDepth;
        public bool CycleDetected;
        // TODO: Path list of intermediate GraphNodes, etc. — port from prototype.
    }

    /// <summary>
    /// Cached evaluation-graph output, built once per Simulate-mode entry and reused across all
    /// tests until the player exits Simulate. The grid is read-only during Simulate, so the cache
    /// does not invalidate mid-session. ModeTransitionSystem clears IsBuilt on Simulate exit.
    /// </summary>
    public class SimulateGraphState : SharedStateComponent, IRegistrationCallbacks
    {
        [HideInInspector] public bool IsBuilt;
        [HideInInspector] public List<CrucialNode> CrucialNodes;
        [HideInInspector] public List<CrucialEdge> OrderedEdges;
        [HideInInspector] public int MaxDepth;

        public void OnRegister()
        {
            IsBuilt = false;
            CrucialNodes = null;
            OrderedEdges = null;
            MaxDepth = 0;
        }

        public void OnDeregister()
        {
        }
    }

    /// <summary>
    /// Static helpers for constructing and clearing the cached evaluation graph.
    /// </summary>
    public static class SimulateGraphUtility
    {
        // Builds CrucialNodes and depth-sorted OrderedEdges from the current grid layout.
        // Sets IsBuilt = true and MaxDepth to the largest EvalDepth observed.
        public static void Build(SimulateGraphState graphState, GridStackState gridStackState)
        {
            // TODO: port ConstructGraph / GraphConstructNodes / GraphConstructEdges /
            //       SetAllNodesAllPaths from EvaluationMgr into this utility.
            //       Populate CrucialNodes, OrderedEdges, MaxDepth; set IsBuilt = true.
        }

        // Invalidates the cache. Called by ModeTransitionSystem when Simulate mode ends.
        public static void Clear(SimulateGraphState graphState)
        {
            // TODO: null the lists; IsBuilt = false; MaxDepth = 0.
        }
    }
}
