namespace SpaceFab.Design
{
    /// <summary>
    /// Phase of the Simulate-mode evaluation machine. The active phase decides which
    /// control requests are legal this frame and what ProcessWork advances.
    /// </summary>
    public enum SimulatePhase
    {
        Idle,             // Simulate mode entered, graph already built by ModeTransitionSystem. Accepts Play / PlaySingle / Cancel.
        PreparingTest,    // Reset per-test sim state for the current row; push row inputs.
        Propagating,      // Walking OrderedEdges depth-by-depth, painting flow. Accepts Pause / Restart* / Cancel.
        Paused,           // Frozen between depth boundaries. Accepts Resume / Restart* / Cancel.
        ResolvingTest,    // End-of-row: score outputs, write row verdict, decide Next or SuiteComplete.
        SuiteComplete,    // All tests run; results panel visible. Accepts Dismiss / Play / PlaySingle / Cancel.
        Cancelling,       // One-shot: wipe sim visuals, clear flow/inversion on grid, exit Simulate mode.
    }

    /// <summary>
    /// Scope of the current (or most recent) run. Decides what happens when a row finishes:
    /// SingleTest → SuiteComplete immediately; FullSuite → advance to the next row until the end.
    /// </summary>
    public enum RunScope
    {
        SingleTest,
        FullSuite,
    }

    /// <summary>
    /// Per-row verdict recorded in SimulateRunState.RowVerdicts. Untested is the default state;
    /// Correct/Incorrect/Unstable are set by ProcessResolvingTest after a row's outputs are scored.
    /// </summary>
    public enum TestRowVerdict
    {
        Untested,
        InProgress,
        Correct,
        Incorrect,
        Unstable,
    }
}
