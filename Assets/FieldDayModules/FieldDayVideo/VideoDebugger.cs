using BeauUtil;
using EasyAssetStreaming;
using FieldDay.Files;
using System;
using UnityEngine;
using UnityEngine.Video;

namespace FieldDay.Video {
    [RequireComponent(typeof(VideoPlayer))]
    public sealed class VideoDebugger : MonoBehaviour {
        [StreamingVideoPath] public string Url;

        [NonSerialized] public VideoPlayer Player;

        private void Awake() {
            this.CacheComponent(ref Player);
            Player.url = FileSystem.ResolvePathToUrl(Url, FileLocation.Streaming);
            Player.Play();
        }
    }
}