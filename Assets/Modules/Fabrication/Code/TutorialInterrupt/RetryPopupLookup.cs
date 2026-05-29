using BeauUtil;
using FieldDay.Assets;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace SpaceFab.Fabrication
{
    /// <summary>
    /// Which error direction a retry-popup entry applies to. The measured direction comes from the sign of
    /// the microgame's raw precision (IMicrogame.GetRawResultPrecision): raw &lt; 1 = Above, raw &gt; 1 = Below.
    /// </summary>
    public enum RetryDirection
    {
        Either, // "< OR >" — matches any direction. Default; the only entry a station needs unless it wants
                // direction-specific messaging.
        Below,  // "<" — the player's final value was below the target.
        Above,  // ">" — the player's final value was above the target.
    }

    /// <summary>
    /// Per-station text shown in the retry popup when a microgame fails its precision gate. Each station may
    /// author a single Either entry (covers both directions) or separate Below/Above entries for directional
    /// feedback; authoring both is never required. Looked up by station id + measured direction via
    /// RetryPopupLookupUtility.
    /// </summary>
    [CreateAssetMenu(menuName = "SpaceFab/Fabrication/RetryPopupLookup")]
    public class RetryPopupLookup : GlobalAsset
    {
        public RetryPopupSet[] FurnaceRetry;
        public RetryPopupSet[] ResistRetry;
        public RetryPopupSet[] PhotolithographyRetry;
        public RetryPopupSet[] EtchRetry;
        public RetryPopupSet[] IonRetry;
        public RetryPopupSet[] SputterRetry;
    }

    /// <summary>
    /// Main + secondary popup text for one microgame retry case, tagged with the error direction it applies
    /// to (defaults to Either, i.e. both directions).
    /// </summary>
    [Serializable]
    public class RetryPopupSet
    {
        public RetryDirection Direction = RetryDirection.Either;
        public string MainText;
        public string SecondaryText;
    }

    /// <summary>
    /// Utility class for RetryPopupLookup.
    /// </summary>
    public static class RetryPopupLookupUtility
    {
        /// <summary>
        /// Returns the retry-popup text for the station and measured direction. A direction-specific entry
        /// wins; otherwise the Either ("< OR >") entry is used. Returns null if the station has no usable
        /// entry.
        /// </summary>
        public static RetryPopupSet Lookup(SerializedHash32 stationID, RetryPopupLookup lookup, RetryDirection direction)
        {
            RetryPopupSet[] entries = GetEntries(stationID, lookup);
            if (entries == null) { return null; }

            RetryPopupSet either = null;
            for (int i = 0; i < entries.Length; i++) {
                RetryPopupSet entry = entries[i];
                if (entry == null) { continue; }
                if (entry.Direction == direction) { return entry; }
                if (entry.Direction == RetryDirection.Either) { either = entry; }
            }
            return either;
        }

        // Returns the authored entry array for the station, or null if the station has none.
        private static RetryPopupSet[] GetEntries(SerializedHash32 stationID, RetryPopupLookup lookup)
        {
            if (lookup == null) return null;
            if (stationID == FabricationConsts.FURNACE_STATION_ID) return lookup.FurnaceRetry;
            else if (stationID == FabricationConsts.RESIST_STATION_ID) return lookup.ResistRetry;
            else if (stationID == FabricationConsts.PHOTOLITHOGRAPHY_STATION_ID) return lookup.PhotolithographyRetry;
            else if (stationID == FabricationConsts.ETCH_STATION_ID) return lookup.EtchRetry;
            else if (stationID == FabricationConsts.ION_STATION_ID) return lookup.IonRetry;
            else if (stationID == FabricationConsts.SPUTTER_STATION_ID) return lookup.SputterRetry;
            else return null;
        }
    }
}
