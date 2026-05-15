using FieldDay;
using FieldDay.Systems;
using SpaceFab;

namespace SpaceFab.Research {
    /// <summary>
    /// Tail end of the explosion lifecycle. Each LateUpdate while
    /// AreAnyExploding is true, walks every ResearchSlot and checks whether
    /// any still owns a running explosion routine. If any are still going,
    /// the timer is held at PostExplosionCooldown. If none are, the timer
    /// counts down; when it hits zero the system flips AreAnyExploding off
    /// and re-enables FieldDay input via ResumeAll.
    /// </summary>
    public class ResearchExplosionSystem : SystemComponent {
        public override unsafe void RegisterSystems(ref SystemRegistrationTable ecs) {
            ecs.Register(&ProcessWork,
                new SysUpdate(GameLoopPhase.LateUpdate, 100, UpdateMasks.ResearchMask),
                new SysPermissions()
                    .ReadWriteShared<ResearchExplosionState>()
            );
        }

        private static void ProcessWork(float deltaTime) {
            Find.State(out ResearchExplosionState explosionState);

            if (!explosionState.AreAnyExploding) {
                return;
            }

            // Any slot with a still-running routine forces the timer to
            // reset; the iteration short-circuits on the first hit.
            bool anyRunning = false;
            foreach (var slot in Find.Components<ResearchSlot>()) {
                if (slot.ExplosionRoutine) {
                    anyRunning = true;
                    break;
                }
            }

            if (anyRunning) {
                explosionState.StateTimer = explosionState.PostExplosionCooldown;
                return;
            }

            explosionState.StateTimer -= deltaTime;
            if (explosionState.StateTimer <= 0f) {
                explosionState.AreAnyExploding = false;
                Game.Input.ResumeAll();
            }
        }
    }
}
