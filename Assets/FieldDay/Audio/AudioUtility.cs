#if UNITY_WEBGL && !UNITY_EDITOR
#define USE_JSLIB
#endif // UNITY_WEBGL && !UNITY_EDITOR

using System.Runtime.InteropServices;
using BeauUtil.Debugger;
using UnityEngine;

namespace FieldDay.Audio {
    static public class AudioUtility {
        /// <summary>
        /// Attempts to wake up native audio playback.
        /// </summary>
        static public void WakeUpNativeAudio() {
#if USE_JSLIB
            if (NativeWebAudio_WakeUp()) {
                Log.Msg("[AudioUtility] Web audio was suspended");
            }
#endif // USE_JSLIB
        }

        /// <summary>
        /// Returns if the current audio player is active.
        /// </summary>
        static public bool IsActive() {
#if USE_JSLIB
            return NativeWebAudio_IsActive();
#else
            return Application.runInBackground || Application.isFocused;
#endif // USE_JSLIB
        }

#if USE_JSLIB
        [DllImport("__Internal")]
        static private extern bool NativeWebAudio_WakeUp();

        [DllImport("__Internal")]
        static private extern bool NativeWebAudio_IsActive();
#endif // USE_JSLIB
    }
}