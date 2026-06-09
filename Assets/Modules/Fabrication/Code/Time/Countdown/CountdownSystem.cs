using FieldDay;
using FieldDay.Systems;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace SpaceFab.Fabrication {
    /// <summary>
    /// Update phase 10 (after AttemptLeadInSystem, AttemptSystem, and PostAttemptSystem
    /// </summary>
    public class CountdownSystem : SystemComponent {
        public override unsafe void RegisterSystems(ref SystemRegistrationTable ecs) {
            ecs.Register(&ProcessWork,
                new SysUpdate(GameLoopPhase.Update, 10, UpdateMasks.AttemptLeadInMask | UpdateMasks.AttemptMask | UpdateMasks.PostAttemptMask),
                new SysPermissions()
                    .ReadWriteShared<CountdownState>()
            );
        }

        static private void ProcessWork(float deltaTime) {
            Find.State(out CountdownState countdownState);

            if (countdownState.CountdownRequestedThisFrame)
            {
                // begin countdown
                CountdownUtility.BeginCountdown(countdownState);
                SpacefabGame.Events.Dispatch(GameEvents.FabGenerateWafer);
            }

            if (countdownState.IsCountingDown)
            {
                countdownState.AccruedTime += deltaTime;

                int roundedTime = Mathf.FloorToInt(countdownState.AccruedTime);
                if (roundedTime < 3)
                {
                    countdownState.CountDownText.text = (3 - roundedTime).ToString();
                }
                else
                {
                    countdownState.CountDownText.text = "GO!";
                }
            }
        }
    }
}
