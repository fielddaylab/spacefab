using BeauUtil.Debugger;
using FieldDay.SharedState;
using SpaceFab.Fabrication.Stations;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace SpaceFab.Fabrication.Layout
{
    /// <summary>
    /// Holds station layout data.
    /// Necessary for navigation and station shuffling.
    /// </summary>
    public class LayoutState : SharedStateComponent
    {
        public MicrogameStationInterfacer[] StationInterfacers;
        public StationSlot[] StationSlots;
    }

    public static class LayoutUtility
    {
        public static void ShuffleStations(LayoutState layoutState)
        {
            // TODO
        }

        public static void AssignStationToSlot(LayoutState layoutState, MicrogameStationInterfacer stationInterfacer, int slotIndex)
        {
            if (layoutState.StationSlots.Length <= slotIndex)
            {
                StationSlotUtility.AssignStation(ref layoutState.StationSlots[slotIndex], ref stationInterfacer);
            }
            else
            {
                Log.Error("[LayoutState] Attempted to assign a station to slot " + slotIndex + ", but index is out of bounds!");
            }
        }
    }
}