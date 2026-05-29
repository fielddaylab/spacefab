using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace SpaceFab.Fabrication.Stations {
    /// <summary>
    /// Contract implemented by each concrete microgame. MicrogameStationInterfacer holds a reference
    /// to one of these and routes station-control state-machine lifecycle calls through it.
    /// </summary>
    public interface IMicrogame {
        // Activation gate consulted when the player presses Activate at this station.
        // Returning false triggers the wrong-station Stun penalty.
        bool CanActivateNow();

        // Called when the station-control machine begins the entry transition (EnteringMicrogame phase).
        void OnEnterBegin();

        // Called when the entry transition completes and the microgame owns input (InMicrogame phase).
        void OnEnterComplete();

        // Called when the microgame is being exited; completedNormally=false means the player cancelled.
        void OnExitBegin(bool completedNormally);

        // Side-effect-free query for the microgame's current result precision in [0,1]. Read by the
        // station-control machine the frame the microgame signals completion (before OnExitBegin) so the
        // Leaf precision gate can evaluate it before any exit animation plays. Stations with no precision
        // concept (Defrag) return 1.
        float GetResultPrecision();

        // Side-effect-free signed precision: the same measurement as GetResultPrecision but WITHOUT the
        // absolute value, so its sign encodes the error direction. Convention: 1 = perfect; < 1 means the
        // player's value overshot the target (above), > 1 means it undershot (below). Stations whose error
        // is inherently unsigned (coverage/fill) just return their GetResultPrecision. Lets the retry popup
        // pick a direction-specific message without any station-specific logic in the generic systems.
        float GetRawResultPrecision();

        // Polled each frame during ExitingMicrogame after a successful completion. Returns true
        // once the microgame's process animation has finished playing. The animation may have
        // been started either in parallel (during InMicrogame, via SignalProcessAnimationStarted)
        // or sequentially (auto-raised on BeginExit). Either way, the exit timer waits behind a
        // true return from this method or the player pressing FabricationConsts.Skip. Not
        // consulted on a cancelled exit (completedNormally=false in OnExitBegin) — cancel skips
        // the animation entirely.
        bool IsProcessAnimationComplete();

        // Called when the exit transition completes and the robot returns to AtStation.
        void OnExitComplete();
    }
}
