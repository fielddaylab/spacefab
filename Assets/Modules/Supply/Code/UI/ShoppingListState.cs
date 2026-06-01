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
            SpacefabGame.Events.DeregisterAllForContext(this);
        }

        public void OnRegister()
        {
            Dirty = true;
            SpacefabGame.Events.Register(GameEvents.SupplyRouteDrawingClose, () => Dirty = true, this);
        }
    }
}