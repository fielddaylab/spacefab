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

        // Called when the exit transition completes and the robot returns to AtStation.
        void OnExitComplete();
    }
}
