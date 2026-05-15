using BeauPools;
using BeauRoutine;
using FieldDay.Components;
using System.Collections;
using UnityEngine;

namespace SpaceFab.Research {
    /// <summary>
    /// Pooled particle-effect holder. One instance wraps one or more
    /// ParticleSystem renderers plus an optional Routine used to drive any
    /// supplemental animation. Allocated from ResearchPools, played via
    /// ResearchVfxUtility, and reclaimed by ResearchVfxMonitorSystem once all
    /// particle systems and the animation routine have finished.
    /// </summary>
    public class ResearchVfxInstance : BatchedComponent, IPoolAllocHandler {
        public ParticleSystem[] Particles;
        public Routine Animation;

        void IPoolAllocHandler.OnAlloc() {
        }

        void IPoolAllocHandler.OnFree() {
            ResearchVfxUtility.StopAndClear(this);
        }
    }

    /// <summary>
    /// Static helpers for ResearchVfxInstance. PlayFromPool is the common
    /// entry point: allocate from a pool, place at the given world position,
    /// and start playing. The remaining helpers are used by the monitor
    /// system and the explosion routine to manage lifecycle.
    /// </summary>
    public static class ResearchVfxUtility {
        // Allocates an instance from the given pool, moves it to the world
        // position of the supplied transform (XY only — Z is preserved), and
        // starts playing every particle system on it.
        public static ResearchVfxInstance PlayFromPool(IPool<ResearchVfxInstance> pool, Transform position) {
            ResearchVfxInstance instance = pool.Alloc();

            Vector3 src = position.position;
            Vector3 dst = instance.transform.position;
            dst.x = src.x;
            dst.y = src.y;
            instance.transform.position = dst;

            Play(instance);
            return instance;
        }

        // Starts every particle system on the instance. Used by PlayFromPool
        // and by callers that want to retrigger an already-positioned instance.
        public static void Play(ResearchVfxInstance instance) {
            for (int i = 0; i < instance.Particles.Length; i++) {
                instance.Particles[i].Play();
            }
        }

        // Starts every particle system and binds a supplemental animation
        // routine. The routine is treated as part of the lifetime: IsPlaying
        // returns true while it is running, so the monitor will not free.
        public static void Play(ResearchVfxInstance instance, IEnumerator routine) {
            instance.Animation.Replace(instance, routine);
            for (int i = 0; i < instance.Particles.Length; i++) {
                instance.Particles[i].Play();
            }
        }

        // True while the animation routine is running or any particle system
        // still has emitters or live particles. Used by the monitor sweep.
        public static bool IsPlaying(ResearchVfxInstance instance) {
            if (instance.Animation) {
                return true;
            }
            for (int i = 0; i < instance.Particles.Length; i++) {
                ParticleSystem ps = instance.Particles[i];
                if (ps.isEmitting || ps.particleCount > 0) {
                    return true;
                }
            }
            return false;
        }

        // Stops emission. Live particles continue to animate until they die.
        public static void Stop(ResearchVfxInstance instance) {
            instance.Animation.Stop();
            for (int i = 0; i < instance.Particles.Length; i++) {
                instance.Particles[i].Stop(true, ParticleSystemStopBehavior.StopEmitting);
            }
        }

        // Stops emission and clears live particles. Used on pool release.
        public static void StopAndClear(ResearchVfxInstance instance) {
            instance.Animation.Stop();
            for (int i = 0; i < instance.Particles.Length; i++) {
                instance.Particles[i].Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            }
        }

        // Returns the instance to its pool if it belongs to one; otherwise
        // halts everything in place. Safe to call from any context.
        public static void Kill(ResearchVfxInstance instance) {
            if (!Pool.TryFree(instance)) {
                StopAndClear(instance);
            }
        }
    }
}
