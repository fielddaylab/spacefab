using BeauRoutine;
using BeauUtil;
using BeauUtil.Debugger;
using EasyAssetStreaming;
using FieldDay.Audio;
using FieldDay.Data;
using FieldDay.Files;
using FieldDay.SharedState;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace FieldDay.Music {
    internal sealed class MusicPlayerState : ISharedState {
        internal RingBuffer<MusicTrack> TrackQueue;
        internal UniqueIdAllocator16 IdAllocator;

        internal AudioHandle CurrentlyPlaying;
        internal UniqueId16 CurrentlyPlayingTrackId;
        internal bool CurrentLoop;
        internal float OverlapCountdown;

        internal MusicRepeatMode RepeatMode;
        internal MusicPlaybackState PlaybackState;

        internal MusicTransitionParams DefaultTransition;
    }

    public enum MusicRepeatMode : byte {
        NoRepeat,
        RepeatAll,
        RepeatSingle
    }

    public enum MusicPlaybackState : byte {
        Stopped,
        Paused,
        Playing
    }

    internal struct MusicTrack : IEquatable<MusicTrack> {
        internal UniqueId16 Id;
        internal StringHash32 EventId;
        internal MusicTransitionParams Transition;

        public bool Equals(MusicTrack other) {
            return Id == other.Id;
        }
    }

    [Serializable]
    public struct MusicTransitionParams {
        public float FadeOut;
        public float FadeIn;
        public float Overlap;
    }

    static public class MusicPlayer {
        /// <summary>
        /// Initializes the music player.
        /// </summary>
        static public void Initialize() {
            Assert.False(Game.SharedState.Has<MusicPlayerState>(), "MusicPlayer has already been initialized");

            EngineHints.LockHint("MUSIC_PLAYER_PLAYLIST_CAPACITY");
            int playlistCapacity = EngineHints.GetHintInt("MUSIC_PLAYER_PLAYLIST_CAPACITY", 16);

            MusicPlayerState playerState = new MusicPlayerState();
            playerState.TrackQueue = new RingBuffer<MusicTrack>(playlistCapacity, RingBufferMode.Fixed);
            playerState.IdAllocator = new UniqueIdAllocator16(playlistCapacity, false);

            Game.SharedState.Register(playerState);
            
            unsafe {
                Game.Systems.Register(&UpdateMusicPlayer,
                    new Systems.SysUpdate(GameLoopPhaseMask.PreUpdate | GameLoopPhaseMask.UnscaledLateUpdate, 10000).AllowDuringLoad(),
                    new Systems.SysPermissions()
                        .ReadWriteShared<MusicPlayerState>()
                    );
            }
        }

        static private void UpdateMusicPlayer(float dt) {
            Find.State(out MusicPlayerState music);

            if (music.PlaybackState != MusicPlaybackState.Playing || music.TrackQueue.Count == 0) {
                return;
            }

            if (music.CurrentlyPlaying.IsValid && !Sfx.IsActive(music.CurrentlyPlaying)) {
                UniqueId16 playing = music.CurrentlyPlayingTrackId;
                music.CurrentlyPlaying = default;
                music.CurrentlyPlayingTrackId = default;

                Assert.True(playing == music.TrackQueue[0].Id);
                if (music.RepeatMode == MusicRepeatMode.NoRepeat) {
                    music.TrackQueue.PopFront();
                    music.IdAllocator.Free(playing);
                    if (music.TrackQueue.Count == 0) {
                        Log.Msg("[MusicPlayer] Out of tracks - stopping playback");
                        music.PlaybackState = MusicPlaybackState.Stopped;
                    }
                } else {
                    music.TrackQueue.MoveFrontToBack();
                }
            }

            if (music.CurrentlyPlayingTrackId == UniqueId16.Invalid) {
                if (music.OverlapCountdown > 0) {
                    if (GameLoop.IsPhase(GameLoopPhase.UnscaledLateUpdate)) {
                        music.OverlapCountdown -= dt;
                        if (music.OverlapCountdown > 0) {
                            return;
                        }
                    } else {
                        return;
                    }
                }

                Log.Msg("[MusicPlayer] Starting playback of new track");

                MusicTrack track = music.TrackQueue.PeekFront();
                music.CurrentLoop = ShouldLoopTrack(music);

                music.CurrentlyPlayingTrackId = track.Id;
                music.CurrentlyPlaying = Sfx.Play(track.EventId, new SfxPlayArgs() {
                    Delay = 0,
                    Pitch = 1,
                    Volume = 0,
                    Pan = 0
                });
                Sfx.SetVolume(music.CurrentlyPlaying, 1, track.Transition.FadeIn);
                Sfx.SetLooping(music.CurrentlyPlaying, music.CurrentLoop);
            } else {
                bool shouldLoopCurrent = ShouldLoopTrack(music);
                if (music.CurrentLoop != shouldLoopCurrent) {
                    music.CurrentLoop = shouldLoopCurrent;
                    Sfx.SetLooping(music.CurrentlyPlaying, shouldLoopCurrent);
                }

                if (!shouldLoopCurrent) {
                    MusicTrack nextTrack = music.TrackQueue[1];
                    AudioSource currentSource = Sfx.GetSource(music.CurrentlyPlaying);
                    float timeFromEnd = currentSource.clip.length - currentSource.time;
                    if (timeFromEnd <= nextTrack.Transition.FadeOut) {
                        Log.Msg("[MusicPlayer] Starting transition to next track");
                        Sfx.Stop(music.CurrentlyPlaying, nextTrack.Transition.FadeOut);
                        music.CurrentlyPlaying = default;
                        music.CurrentlyPlayingTrackId = default;
                        music.OverlapCountdown = nextTrack.Transition.FadeOut - nextTrack.Transition.Overlap;

                        if (music.RepeatMode == MusicRepeatMode.NoRepeat) {
                            MusicTrack prevTrack = music.TrackQueue.PopFront();
                            music.IdAllocator.Free(prevTrack.Id);
                        } else {
                            music.TrackQueue.MoveFrontToBack();
                        }
                    }
                }
            }
        }

        static private bool ShouldLoopTrack(MusicPlayerState music) {
            return music.RepeatMode == MusicRepeatMode.RepeatSingle || music.TrackQueue.Count == 1;
        }

        #region Defaults

        /// <summary>
        /// Sets the default music transition.
        /// </summary>
        static public void SetDefaultTransition(MusicTransitionParams transition) {
            Find.State<MusicPlayerState>().DefaultTransition = transition;
        }

        #endregion // Defaults

        #region Playback State

        /// <summary>
        /// Returns the current playback state.
        /// </summary>
        static public MusicPlaybackState GetPlaybackState() {
            return Find.State<MusicPlayerState>().PlaybackState;
        }

        /// <summary>
        /// Sets the music playback state. This will stop, pause, or resume playback.
        /// </summary>
        static public void SetPlaybackState(MusicPlaybackState state) {
            Find.State(out MusicPlayerState music);

            if (music.PlaybackState != state) {
                music.PlaybackState = state;

                Log.Msg("[MusicPlayer] Setting playback state to '{0}'", state);

                switch(state) {
                    case MusicPlaybackState.Stopped: {
                        if (music.CurrentlyPlaying.IsValid) {
                            Log.Msg("[MusicPlayer] Fading out current track");
                            Sfx.Stop(music.CurrentlyPlaying, music.DefaultTransition.FadeOut);
                            music.CurrentlyPlaying = default;
                            music.CurrentlyPlayingTrackId = default;
                            if (music.RepeatMode == MusicRepeatMode.NoRepeat) {
                                MusicTrack track = music.TrackQueue.PopFront();
                                music.IdAllocator.Free(track.Id);
                            } else {
                                music.TrackQueue.MoveFrontToBack();
                            }
                        }
                        music.OverlapCountdown = 0;
                        break;
                    }
                    case MusicPlaybackState.Paused: {
                        if (music.CurrentlyPlaying.IsValid) {
                            Sfx.SetPaused(music.CurrentlyPlaying, true);
                        }
                        break;
                    }
                    case MusicPlaybackState.Playing: {
                        if (music.CurrentlyPlaying.IsValid) {
                            Sfx.SetPaused(music.CurrentlyPlaying, false);
                        }
                        break;
                    }
                }
            }
        }

        /// <summary>
        /// Starts playing the playlist.
        /// </summary>
        static public void Play() {
            SetPlaybackState(MusicPlaybackState.Playing);
        }

        /// <summary>
        /// Stops playing the current track.
        /// </summary>
        static public void Stop() {
            SetPlaybackState(MusicPlaybackState.Stopped);
        }

        /// <summary>
        /// Stops playing the current track.
        /// </summary>
        static public void Stop(float fadeOut) {
            Find.State(out MusicPlayerState music);

            if (music.PlaybackState != MusicPlaybackState.Stopped) {
                music.PlaybackState = MusicPlaybackState.Stopped;

                Log.Msg("[MusicPlayer] Setting playback state to 'Stopped'");

                if (music.CurrentlyPlaying.IsValid) {
                    Log.Msg("[MusicPlayer] Fading out current track");
                    Sfx.Stop(music.CurrentlyPlaying, fadeOut);
                    music.CurrentlyPlaying = default;
                    music.TrackQueue.MoveFrontToBack();
                }
                music.OverlapCountdown = 0;
            }
        }

        #endregion // Playback State

        #region Repeat Mode

        /// <summary>
        /// Retrieves the current playlist repeat mode.
        /// </summary>
        static public MusicRepeatMode GetRepeatMode() {
            return Find.State<MusicPlayerState>().RepeatMode;
        }

        /// <summary>
        /// Sets the current playlist repeat mode.
        /// </summary>
        static public void SetRepeatMode(MusicRepeatMode repeatMode) {
            Find.State<MusicPlayerState>().RepeatMode = repeatMode;
        }

        #endregion // Repeat Mode

        #region Queue

        /// <summary>
        /// Enqueues a track to be played.
        /// </summary>
        static public UniqueId16 QueueTrack(StringHash32 eventId) {
            Find.State(out MusicPlayerState music);
            return QueueTrackInternal(music, new MusicTrack() {
                EventId = eventId,
                Transition = music.DefaultTransition
            });
        }

        /// <summary>
        /// Enqueues a track to be played.
        /// </summary>
        static public UniqueId16 QueueTrack(StringHash32 eventId, MusicTransitionParams transition) {
            Find.State(out MusicPlayerState music);
            return QueueTrackInternal(music, new MusicTrack() {
                EventId = eventId,
                Transition = transition
            });
        }

        static internal UniqueId16 QueueTrackInternal(MusicPlayerState player, MusicTrack track) {
            UniqueId16 id = player.IdAllocator.Alloc();
            track.Id = id;
            player.TrackQueue.PushBack(track);
            return id;
        }

        /// <summary>
        /// Sets the current looping track.
        /// </summary>
        static public void SetLoopingTrack(StringHash32 eventId, MusicTransitionParams transition) {
            if (eventId.IsEmpty) {
                Stop(transition.FadeOut);
                return;
            }

            Find.State(out MusicPlayerState music);
            SetLoopingTrackInternal(music, new MusicTrack() {
                EventId = eventId,
                Transition = transition
            });
        }

        /// <summary>
        /// Sets the current looping track.
        /// </summary>
        static public void SetLoopingTrack(StringHash32 eventId) {
            if (eventId.IsEmpty) {
                Stop();
                return;
            }

            Find.State(out MusicPlayerState music);
            SetLoopingTrackInternal(music, new MusicTrack() {
                EventId = eventId,
                Transition = music.DefaultTransition
            });
        }

        static internal void SetLoopingTrackInternal(MusicPlayerState player, MusicTrack track) {
            player.RepeatMode = MusicRepeatMode.RepeatSingle;
            if (player.TrackQueue.Count > 0) {
                while (player.TrackQueue.Count > 1) {
                    MusicTrack undone = player.TrackQueue.PopBack();
                    player.IdAllocator.Free(undone.Id);
                }

                MusicTrack current = player.TrackQueue[0];
                if (current.EventId == track.EventId) {
                    return;
                }

                player.IdAllocator.Free(current.Id);
                player.TrackQueue.PopFront();
            }

            if (player.CurrentlyPlaying.IsValid) {
                Log.Msg("[MusicPlayer] Fading out current track");
                Sfx.Stop(player.CurrentlyPlaying, track.Transition.FadeOut);
                player.CurrentlyPlaying = default;
                player.CurrentlyPlayingTrackId = default;
                player.OverlapCountdown = track.Transition.FadeOut - track.Transition.Overlap;
            }

            QueueTrackInternal(player, track);

            if (player.PlaybackState == MusicPlaybackState.Stopped) {
                player.PlaybackState = MusicPlaybackState.Playing;
            }
        } 

        /// <summary>
        /// Removes a track from playback.
        /// </summary>
        static public bool DequeueTrack(UniqueId16 trackId) {
            Find.State(out MusicPlayerState music);

            if (!music.IdAllocator.Free(trackId)) {
                return false;
            }

            if (music.CurrentlyPlayingTrackId == trackId) {
                if (music.CurrentlyPlaying.IsValid) {
                    Log.Msg("[MusicPlayer] Fading out current track");
                    Sfx.Stop(music.CurrentlyPlaying, music.DefaultTransition.FadeOut);
                    music.CurrentlyPlaying = default;
                }
                music.CurrentlyPlayingTrackId = default;
                music.TrackQueue.PopFront();
                return true;
            }

            for(int i = 0; i < music.TrackQueue.Count; i++) {
                if (music.TrackQueue[i].Id == trackId) {
                    music.TrackQueue.RemoveAt(i);
                    return true;
                }
            }

            Assert.Fail("Id was valid but no tracks with id found");
            return false;
        }

        /// <summary>
        /// Stops playing the current track and clears the queue.
        /// </summary>
        static public void ClearQueue() {
            SetPlaybackState(MusicPlaybackState.Stopped);
            
            Find.State(out MusicPlayerState music);

            while(music.TrackQueue.TryPopBack(out var track)) {
                music.IdAllocator.Free(track.Id);
            }

            music.TrackQueue.Clear();
        }

        #endregion // Queue
    }
}