using BeauPools;
using BeauUtil;
using FieldDay.Scenes;
using FieldDay.SharedState;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace SpaceFab.Overarching
{
    /// <summary>
    /// Shared pools the Overarching scene draws from. Today: alert icons stacked above minigame
    /// zones. Pattern mirrors ResearchPools / DesignPools — one nested SerializablePool subclass
    /// per payload type, an Active list for iteration / cleanup, Prewarm in IScenePreload.
    /// </summary>
    public class OverarchingPools : SharedStateComponent, IScenePreload
    {
        [Serializable] public sealed class AlertIconPool : SerializablePool<AlertIconView> { }

        [Header("Alerts")]
        public AlertIconPool AlertPool;

        // Currently-allocated alert icons across every minigame zone. Grown / shrunk by
        // OverarchingAlertSystem on each AlertVisualsDirty pass; the list is the canonical
        // "free these before the next refresh" set.
        [NonSerialized] public List<AlertIconView> ActiveAlertIcons;

        IEnumerator<WorkSlicer.Result?> IScenePreload.Preload()
        {
            AlertPool.Prewarm();
            ActiveAlertIcons = new List<AlertIconView>(8);
            return null;
        }
    }
}
