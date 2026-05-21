using FieldDay.SharedState;
using System;

namespace SpaceFab.Research {
    /// <summary>
    /// Tracks whether any explosion is currently in progress in the Research
    /// minigame and how long until input resumes. Set by
    /// ResearchExplosionUtility.BeginExplosions; decayed and cleared by
    /// ResearchExplosionSystem. While AreAnyExploding is true, FieldDay
    /// input is paused (PauseAll), so the drag system and any other input
    /// consumer naturally idles for the duration.
    /// </summary>
    public class ResearchExplosionState : SharedStateComponent {
        // Seconds of input lock applied before any decay checks begin. Gives
        // the first explosion routine a chance to start its own work before
        // the system would consider the explosion "over."
        public float PreExplosionCooldown = 1f;

        // Seconds the system continues to wait after the last routine ends
        // before calling ResumeAll. Stops jittery resume-then-re-pause cases
        // when multiple chained routines stagger their completion.
        public float PostExplosionCooldown = 0.5f;

        [NonSerialized] public bool AreAnyExploding;
        [NonSerialized] public float StateTimer;
    }
}
