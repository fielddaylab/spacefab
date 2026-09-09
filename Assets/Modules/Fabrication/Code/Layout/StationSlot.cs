using FieldDay.Components;
using SpaceFab.Fabrication.Stations;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace SpaceFab.Fabrication.Layout {
    /// <summary>
    /// Interfacer and positional slot for a Station.
    /// </summary>
    public class StationSlot : BatchedComponent
    {
        [NonSerialized] public MicrogameStationInterfacer AssignedStationInterfacer;
    }

    public static class StationSlotUtility
    {
        public static void AssignStation(ref StationSlot slot, ref MicrogameStationInterfacer stationInterfacer)
        {
            slot.AssignedStationInterfacer = stationInterfacer;
            stationInterfacer.transform.position = slot.transform.position;
        }
    }
}

