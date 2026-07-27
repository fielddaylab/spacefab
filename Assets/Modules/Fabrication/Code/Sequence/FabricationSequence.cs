using BeauUtil;
using FieldDay.Assets;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace SpaceFab.Fabrication.Sequence
{
    /// <summary>
    /// A single step in a fabrication sequence. Carries a chunk (N / P / Metal; determines card
    /// background) and a step id (determines card foreground AND the target station via
    /// SequenceLookup). Plus the wafer postcondition and checkpoint flag.
    /// </summary>
    [Serializable]
    public struct FabricationStep
    {
        // Card background category. Determines the background sprite/text looked up from
        // SequenceLookup at runtime.
        public SequenceChunk Chunk;

        // Which step this is. Determines the card foreground sprite/text AND the target station
        // (SequenceLookup.GetStationForStep). One source of truth; no redundant StationId field.
        // Authored in inspector as a SequenceStepID dropdown (finite, enumerated set).
        public SequenceStepID StepId;

        // Expected wafer state at the moment this step's microgame completes. If the actual wafer
        // doesn't match, FabWaferMisalignment is dispatched.
        public WaferSnapshot ExpectedWaferAfter;

        // If true, completing this step captures a checkpoint (time + wafer + robot slot) that the
        // sequence can roll back to on a later misalignment.
        public bool IsCheckpoint;
    }

    /// <summary>
    /// ScriptableObject definition for a fabrication sequence: ordered steps and glitch authoring.
    /// Referenced by ContractAssetsWrapper. Card visuals are looked up at runtime via the global
    /// SequenceLookup asset (not authored per-level). The checkpoint-rollback lead-in is game-wide
    /// and lives on SequenceUtility.
    /// </summary>
    [CreateAssetMenu(menuName = "SpaceFab/Fabrication/Sequence")]
    public class FabricationSequence : NamedAsset
    {
        [SerializeField] private FabricationStep[] m_Steps;
        public FabricationStep[] Steps => m_Steps;
    }
}
