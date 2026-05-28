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
    }
}
