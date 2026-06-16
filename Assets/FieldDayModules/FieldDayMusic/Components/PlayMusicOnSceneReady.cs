using BeauUtil;
using FieldDay.Assets;
using FieldDay.Audio;
using UnityEngine;

namespace FieldDay.Music {
    /// <summary>
    /// Plays a music asset when the scene is ready.
    /// </summary>
    public sealed class PlayMusicOnSceneReady : MonoBehaviour {
        [AudioEvent] public StringHash32 EventId;
        [AssetName(typeof(MusicAsset))] public StringHash32 PlaylistId;

        private void Awake() {
            Game.Scenes.QueueOnLoad(PlayMusic);
        }

        private void PlayMusic() {
            if (PlaylistId.IsEmpty) {
                MusicPlayer.SetLoopingTrack(EventId);
            } else {
                MusicPlayer.SetPlaylistFromAssetId(PlaylistId);
            }
        }
    }
}