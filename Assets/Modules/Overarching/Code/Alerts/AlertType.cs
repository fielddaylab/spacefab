using System;

namespace SpaceFab.Overarching
{
    /// <summary>
    /// Per-minigame alert flags. Multiple bits can be set simultaneously; each set bit spawns
    /// one icon above the corresponding zone (see OverarchingAlertSystem). Locked additionally
    /// blocks hover + click via SelectMinigameZoneSystem.
    /// </summary>
    [Flags]
    public enum AlertType
    {
        None = 0,
        NeedsAttention = 1 << 0,
        Incomplete = 1 << 1,
        Locked = 1 << 2,
        Complete = 1 << 3,
        // Minigame the player has never started. Distinct from Incomplete, which is a minigame
        // that was started but not yet completed.
        NotStarted = 1 << 4,
    }
}
