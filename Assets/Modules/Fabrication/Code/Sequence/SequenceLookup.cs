using BeauUtil;
using FieldDay.Assets;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace SpaceFab.Fabrication.Sequence
{
    /// <summary>
    /// The three valid sequence-chunk categories, each corresponding to a layer of the wafer being
    /// fabricated. A chunk determines the background visuals/text shown on a step's hint card.
    /// </summary>
    public enum SequenceChunk
    {
        N,      // N-type doping chunk.
        P,      // P-type doping chunk.
        Metal   // Metal deposition chunk.
    }

    /// <summary>
    /// The full finite set of sequence steps. Each value maps 1:1 to a station (resolved via
    /// SequenceLookup.GetStationForStep) and to a foreground card sprite/text.
    /// </summary>
    public enum SequenceStepID
    {
        AddStencil_Oxide,
        AddStencil_Sputter,
        ApplyResist,
        DrawPattern,
        EtchPattern,
        FillStencil_Sputter,
        FillStencil_Dope,
    }

    /// <summary>
    /// Card-display data for a single sequence step. Looked up from SequenceLookup by step id at
    /// runtime to render the step's foreground (station-specific) visuals and text, and to resolve
    /// the step's target station id.
    /// </summary>
    [Serializable]
    public struct SequenceStepEntry
    {
        // Which step this entry describes. Matches FabricationStep.StepId authored on a level asset.
        public SequenceStepID StepId;

        // The station the player must visit for this step. Authored as SerializedHash32 so it can
        // be set from the inspector; matches MicrogameStationInterfacer.Id at runtime.
        public SerializedHash32 StationId;

        // Foreground image shown on the step's hint card.
        public Sprite StepSprite;

        // Foreground text shown on the step's hint card.
        public string StepText;
    }

    /// <summary>
    /// Card-display data for a sequence chunk (N / P / Metal). Looked up from SequenceLookup by
    /// chunk at runtime to render the step's background (chunk-specific) visuals and text.
    /// </summary>
    [Serializable]
    public struct SequenceChunkEntry
    {
        public SequenceChunk Chunk;
        public Sprite ChunkSprite;
        public string ChunkText;
    }

    /// <summary>
    /// Global asset holding all card-display lookups for the Fabrication sequence feature:
    /// per-step foreground (sprite + text + station mapping), per-chunk background (sprite + text),
    /// and the shared glitch overlay applied when a step's card is glitched.
    /// Accessed via Find.GlobalAsset&lt;SequenceLookup&gt;().
    /// </summary>
    [CreateAssetMenu(menuName = "SpaceFab/Fabrication/Sequence Lookup")]
    public class SequenceLookup : GlobalAsset
    {
        [SerializeField] private SequenceStepEntry[] m_Steps;
        [SerializeField] private SequenceChunkEntry[] m_Chunks;

        // Overlay visuals applied to any step whose card is glitched. One overlay for all glitched
        // steps; covers everything (station name, description, visuals).
        [SerializeField] private Sprite m_GlitchOverlaySprite;
        [SerializeField] private string m_GlitchOverlayText;

        // Cached lookups, built lazily on first access.
        private Dictionary<SequenceStepID, SequenceStepEntry> m_StepsByIdCache;
        private Dictionary<SequenceChunk, SequenceChunkEntry> m_ChunksByKeyCache;

        public Sprite GlitchOverlaySprite => m_GlitchOverlaySprite;
        public string GlitchOverlayText => m_GlitchOverlayText;

        // Returns the step entry for the given step id, or the default entry if not found.
        // TODO: real lookup path. Scaffold returns default.
        public SequenceStepEntry GetStep(SequenceStepID stepId)
        {
            // TODO: EnsureCaches(); m_StepsByIdCache.TryGetValue(stepId, out var entry); return entry.
            return default;
        }

        // Returns the station id for the given step. Convenience over GetStep(stepId).StationId.
        // TODO: scaffold returns default.
        public SerializedHash32 GetStationForStep(SequenceStepID stepId)
        {
            // TODO: return GetStep(stepId).StationId.
            return default;
        }

        // Returns the chunk entry for the given chunk, or the default entry if not found.
        // TODO: scaffold returns default.
        public SequenceChunkEntry GetChunk(SequenceChunk chunk)
        {
            // TODO: EnsureCaches(); m_ChunksByKeyCache.TryGetValue(chunk, out var entry); return entry.
            return default;
        }

        // Builds the two caches on first use. Called at the top of each lookup method.
        private void EnsureCaches()
        {
            // TODO: if caches exist, return. Otherwise populate from m_Steps / m_Chunks.
        }
    }
}
