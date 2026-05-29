using FieldDay;
using FieldDay.SharedState;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace SpaceFab.Supply
{
    /// <summary>
    ///  Holds data relating to the player's "shopping list",
    ///  i.e. material properties specified in the contract
    /// </summary>
    public class ShoppingListState : SharedStateComponent, IRegistrationCallbacks
    {
        // Flags
        public bool Dirty; // marked at load and whenever gathered materials is updated

        public void OnDeregister()
        {
        }

        public void OnRegister()
        {
            Dirty = true;

            // TODO: when the supply route-finalized event is authored (carrying the
            // confirmed route index), register here so the list refreshes on commit:
            // SpacefabGame.Events.Register(GameEvents.SupplyRouteFinalized, (int routeIndex) => Dirty = true, this);
        }
    }
}