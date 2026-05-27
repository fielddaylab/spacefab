namespace SpaceFab.Overarching
{
    /// <summary>
    /// Stable identifier for a minigame zone. The first four entries (Design / Research /
    /// Fabrication / Supply) map 1:1 to MinigameSaveStates' fields
    ///
    /// COUNT must stay at the end — it sizes OverarchingAlertState.Masks. When a new minigame
    /// is added, append it before COUNT, then also extend MinigameSaveStates and the switch in
    /// OverarchingAlertUtility.ApplyAutoRuleFromSaveStates.
    /// </summary>
    public enum MinigameId
    {
        Design,
        Research,
        Fabrication,
        Supply,
        COUNT,
    }
}
