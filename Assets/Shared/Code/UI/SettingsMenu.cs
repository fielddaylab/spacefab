
using BeauUtil;
using FieldDay;
using FieldDay.Components;
using FieldDay.Rendering;
using TMPro;
using UnityEngine.UI;

namespace SpaceFab
{
    public class SettingsMenu : BatchedComponent, IRegistrationCallbacks
    {
        public Toggle FullscreenToggle;
        public Slider VolumeSlider;
        public Slider MusicSlider;
        public Slider SFXSlider;

        public void OnDeregister()
        {
            FullscreenToggle.onValueChanged.RemoveAllListeners();

            VolumeSlider.onValueChanged.RemoveAllListeners();
            MusicSlider.onValueChanged.RemoveAllListeners();
            SFXSlider.onValueChanged.RemoveAllListeners();

            Game.Rendering.OnFullscreenChanged.Deregister(OnFullscreenUpdated);
        }

        public void OnRegister()
        {
            UserSettingsState state = Find.State<UserSettingsState>();

            Game.Rendering.OnFullscreenChanged.Register(OnFullscreenUpdated);

            FullscreenToggle.SetIsOnWithoutNotify(ScreenUtility.GetFullscreen());
            UpdateFullscreen(FullscreenToggle.isOn);
            FullscreenToggle.onValueChanged.AddListener(UpdateFullscreen);

            VolumeSlider.onValueChanged.AddListener(UpdateVolume);
            MusicSlider.onValueChanged.AddListener((float vol) => UpdateBusVolume(SettingsUtility.MUSIC_BUS_ID, vol));
            SFXSlider.onValueChanged.AddListener((float vol) => UpdateBusVolume(SettingsUtility.SFX_BUS_ID, vol));
            InitializeVolumeSettings();
        }

        private void InitializeVolumeSettings()
        {
            UserSettingsState settings = Find.State<UserSettingsState>();
            VolumeSlider.value = settings.MasterVolume;
            MusicSlider.value = settings.MusicVolume;
            SFXSlider.value = settings.SFXVolume;
        }

        private void UpdateFullscreen(bool toggle)
        {
            SettingsUtility.SetFullscreen(Find.State<UserSettingsState>(), toggle);
        }

        private void UpdateVolume(float volume)
        {
            SettingsUtility.SetMasterVolume(Find.State<UserSettingsState>(), volume / 10);
        }

        private void UpdateBusVolume(StringHash32 bus, float volume)
        {
            SettingsUtility.SetAudioBusVolume(Find.State<UserSettingsState>(), bus, volume / 10);
        }

        private void OnFullscreenUpdated(bool fullscreen)
        {
            FullscreenToggle.SetIsOnWithoutNotify(fullscreen);
        }
    }
}