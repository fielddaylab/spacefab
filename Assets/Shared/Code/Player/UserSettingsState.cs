using BeauUtil;
using FieldDay;
using FieldDay.Audio;
using FieldDay.Data;
using FieldDay.Rendering;
using FieldDay.SharedState;
using SpaceFab.Save;
using SpaceFab;
using System;
using UnityEngine;

namespace SpaceFab
{
    public class UserSettingsState : SharedStateComponent, IRegistrationCallbacks, ISaveStateChunkObject
    {
        [NonSerialized] public string PlayerCode = null;
        [NonSerialized] public float MasterVolume;
        [Range(0, 1)] public float DefaultMasterVol;
        [NonSerialized] public float MusicVolume;
        [Range(0, 1)] public float DefaultMusicVol;
        [NonSerialized] public float SFXVolume;
        [Range(0, 1)] public float DefaultSFXVol;
        [NonSerialized] public bool CameraDriftEnabled = true;
        [NonSerialized] public bool HighQualityMode;
        [NonSerialized] public bool FullscreenEnabled = false;

        public void OnDeregister()
        {
            SpacefabGame.SaveBuffer.DeregisterHandler("UserSettingsState");
        }

        public void OnRegister()
        {
            MasterVolume = DefaultMasterVol;
            MusicVolume = DefaultMusicVol;
            SFXVolume = DefaultSFXVol;
            PlayerCode = PlayerPrefs.GetString("LatestPlayerCode", null);
            SpacefabGame.SaveBuffer.RegisterHandler("UserSettingsState", this);
        }

        public void Read(object self, ref ByteReader reader, SaveStateChunkConsts consts)
        {
            float volume = reader.Read<float>();
            SettingsUtility.SetMasterVolume(this, volume);

            float musicVol = reader.Read<float>();
            SettingsUtility.SetAudioBusVolume(this, SettingsUtility.MUSIC_BUS_ID, musicVol);

            float sfxVol = reader.Read<float>();
            SettingsUtility.SetAudioBusVolume(this, SettingsUtility.SFX_BUS_ID, sfxVol);

            bool cameraDrift = reader.Read<bool>();
            SettingsUtility.SetCameraDrift(this, cameraDrift);

            bool highQuality = reader.Read<bool>();
            SettingsUtility.SetQualityMode(this, highQuality);

            bool fullscreen = reader.Read<bool>();
            SettingsUtility.SetFullscreen(this, fullscreen);
        }

        public void Write(object self, ref ByteWriter writer, SaveStateChunkConsts consts)
        {
            writer.Write((float)MasterVolume);

            writer.Write((float)MusicVolume);
            writer.Write((float)SFXVolume);

            writer.Write((bool)CameraDriftEnabled);
            writer.Write((bool)HighQualityMode);
            writer.Write((bool)FullscreenEnabled);
        }
    }

    public static class SettingsUtility
    {
        public static StringHash32 MUSIC_BUS_ID = "Music";
        public static StringHash32 SFX_BUS_ID = "Sfx";

        static public readonly CastableEvent<bool> OnSubtitlesEnabledUpdated = new CastableEvent<bool>();

        public static void SetQualityMode(UserSettingsState state, bool mode)
        {
            state.HighQualityMode = mode;
        }
        public static void SetCameraDrift(UserSettingsState state, bool drift)
        {
            state.CameraDriftEnabled = drift;
        }

        public static void SetFullscreen(UserSettingsState state, bool fullscreen)
        {
            state.FullscreenEnabled = fullscreen;
            ScreenUtility.SetFullscreen(fullscreen);
        }

        public static void SetMasterVolume(UserSettingsState state, float set)
        {
            if (set < 0 || set > 1.0f)
            {
                throw new ArgumentOutOfRangeException("[SettingsUtility] Set volume " + set + " invalid! Must be 0 to 1");
            }
            state.MasterVolume = set;
            Sfx.SetBusVolume(AudioBus.Master, set);
        }

        public static void SetAudioBusVolume(UserSettingsState state, StringHash32 busId, float set)
        {
            if (set < 0 || set > 1.0f)
            {
                throw new ArgumentOutOfRangeException("[SettingsUtility] Set volume " + set + " invalid! Must be 0 to 1");
            }
            SetAudioSetting(state, busId, set);
            Sfx.SetBusVolume(busId, set);
        }

        public static void SetAudioSetting(UserSettingsState state, StringHash32 busId, float set)
        {
            if (busId == MUSIC_BUS_ID)
            {
                state.MusicVolume = set;
            }
            else if (busId == SFX_BUS_ID)
            {
                state.SFXVolume = set;
            }
        }
    }
}