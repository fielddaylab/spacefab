using BeauUtil;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace SpaceFab.Fabrication
{
    public static class FabricationConsts
    {
        // converts time elapsed during fabrication attempt (in seconds) into overarching cycles
        public static int CyclesPerSecond;

        // Well-known station ids. Authored on station prefabs via MicrogameStationInterfacer.m_Id.
        // The Defrag station is universal: visiting it does not advance the sequence or run an
        // alignment check; it clears the glitch flag on the current step's card.
        public static readonly StringHash32 DEFRAG_STATION_ID = "station:defrag";
    }
}