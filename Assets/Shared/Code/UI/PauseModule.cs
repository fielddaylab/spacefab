
using TMPro;
using System;
using BeauRoutine;
using FieldDay;
using UnityEngine;
using FieldDay.UI;
using FieldDay.Audio;
using FieldDay.UI.Widgets;
using SpaceFab.UI;

namespace SpaceFab
{
    public class PauseModule : BaseGuiModule, IRegistrationCallbacks
    {
        #region Inspector

        public BaseRaycasterInputLayer InputLayer;
        
        [Header("Buttons")]
        public GuiButton CloseButton;

        [Header("Display")]
        public SettingsMenu SettingsMenu;
        public TMP_Text PlayerCodeDisplay;

        #endregion // Inspector

        [NonSerialized] public int StashedUpdateMask;
        [NonSerialized] public bool GamePaused;
        [NonSerialized] public Routine FadeRoutine;

        public void OnRegister()
        {
            PlayerCodeDisplay.SetTextAndActive(PlayerPrefs.GetString("LatestPlayerCode", null));
            CloseButton.OnClick.Register(() => PauseUtility.SetPaused(this, false));
        }

        public void OnDeregister()
        {
            FadeRoutine.Stop();
        }
    }

    public static class PauseUtility
    {
        public static void SetPaused(PauseModule state, bool paused)
        {
            if (state.GamePaused == paused)
            {
                return;
            }
            
            state.GamePaused = paused;

            if (paused)
            {
                SpacefabGame.Events.Dispatch(GameEvents.ClickPauseGame);
            }
            else
            {
                SpacefabGame.Events.Dispatch(GameEvents.ClickResumeGame);
            }

            Routine.Settings.Paused = paused;
            GameLoop.TimeScale = paused ? 0 : 1;
            Sfx.SetMixStateEnabled("PauseMix", paused, false);

            if (paused)
            {
                state.StashedUpdateMask = GameLoop.UpdateMask;
                GameLoop.SuspendUpdates(UpdateMasks.EntireGame);
                GameLoop.ResumeUpdates(UpdateMasks.PauseUpdateMask);
                PopupUtility.PushState();
                state.InputLayer.TryPushPriority();
                Game.Events.Dispatch(GameEvents.OnGamePaused);
            }
            else
            {
                GameLoop.ResumeUpdates(state.StashedUpdateMask);
                state.InputLayer.TryPopPriority();
                PopupUtility.PopState();
                Game.Events.Dispatch(GameEvents.OnGameResumed);
            }
        }
    }
}
