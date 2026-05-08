using BeauUtil;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace SpaceFab.Fabrication
{
    public static class FabricationConsts
    {
        // converts time elapsed during fabrication attempt (in seconds) into overarching cycles
        public static int CyclesPerSecond;

        // Well-known station ids. Authored on station prefabs via MicrogameStationInterfacer.m_Id.
        // The Defrag station is universal: visiting it does not advance the sequence or run an
        // alignment check; it clears the glitch flag on the current step's card.
        public static readonly StringHash32 DEFRAG_STATION_ID = "station:defrag";

        // Duration of the checkpoint-rollback lead-in pause, in seconds. Game-wide constant;
        // consumed by RestoreLeadIn. Tunable later if the visual treatment demands a different feel.
        public static readonly float CHECKPOINT_LEAD_IN_SECONDS = 1.0f;

        #region Input Mappings

        // World Interact

        public const KeyCode Up0 = KeyCode.W;
        public const KeyCode Up1 = KeyCode.UpArrow;

        public const KeyCode Down0 = KeyCode.S;
        public const KeyCode Down1 = KeyCode.DownArrow;

        public const KeyCode Activate = KeyCode.Space;

        // Skips the post-microgame process animation during ExitingMicrogame. Reuses the same
        // physical key as Down0 (Cancel) intentionally — Cancel is consumed only during
        // InMicrogame while Skip is consumed only during ExitingMicrogame, so the two never
        // overlap in time.
        public const KeyCode Skip = KeyCode.S;

        // Movement

        public const KeyCode Left0 = KeyCode.A;
        public const KeyCode Left1 = KeyCode.LeftArrow;

        public const KeyCode Right0 = KeyCode.D;
        public const KeyCode Right1 = KeyCode.RightArrow;

        #endregion // Input Mappings
    }
}