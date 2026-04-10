using BeauUtil.Debugger;
using FieldDay;
using FieldDay.SharedState;
using SpaceFab.Fabrication.Stations;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using static Unity.Mathematics.math;

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

        [HideInInspector] public bool NeedsReshuffling;
    }

    public static class LayoutUtility
    {
        public static void ShuffleStations(LayoutState layoutState)
        {
            // index = station slot, value at index = station interfacer index found within that slot
            int numElements = layoutState.StationInterfacers.Length;
            int[] stationOrder = new int[numElements];

            // init shuffle order
            for (int i = 0; i < numElements; i++)
            {
                stationOrder[i] = i;
            }

            // Fisher-Yates shuffle
            for (int i = 0; i < numElements - 2; i++)
            {
                int swapIndex = Random.Range(i, numElements);
                stationOrder[swapIndex] = stationOrder[i];
            }

            // Apply shuffle
            for (int i = 0; i < numElements; i++)
            {
                AssignStationToSlot(layoutState, layoutState.StationInterfacers[stationOrder[i]], i);
            }
        }

        public static void AssignStationToSlot(LayoutState layoutState, MicrogameStationInterfacer stationInterfacer, int slotIndex)
        {
            if (slotIndex <= )
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