using BeauUtil;
using FieldDay.Assets;
using SpaceFab.Fabrication.Stations;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace SpaceFab.Fabrication.Layout
{
    /// <summary>
    /// Key type image the microgame will display
    /// </summary>
    public enum KeyImage
    {
        Space,
        LRArrows,
        FullArrows,
        Mouse,
        ADKeys
    }

    [CreateAssetMenu(menuName = "SpaceFab/Fabrication/InstructionLookup")]
    public class InstructionLookup : GlobalAsset
    {
        public InstructionSet FurnaceInstructions;

        public InstructionSet ResistInstructions;

        public InstructionSet PhotolithographyInstructions;

        public InstructionSet EtchInstructions;

        public InstructionSet IonInstructions;

        public InstructionSet SputterInstructions;
    }

    /// <summary>
    /// Instruction set class for use in displaying the correct image and instruction
    /// </summary>
    [System.Serializable]
    public class InstructionSet
    {
        public KeyImage UIKey;
        public string Instruction;
        public string Subtitle;
    }

    /// <summary>
    /// Utility class for InstructionsLookup
    /// </summary>
    public static class InstructionLookupUtility
    {
        /// <summary>
        /// Returns corresponding instruction set for the interfacer as described by the lookup
        /// </summary>
        public static InstructionSet LookupInstructions(SerializedHash32 stationID, InstructionLookup lookup)
        {
            if (stationID == FabricationConsts.FURNACE_STATION_ID) return lookup.FurnaceInstructions;
            else if (stationID == FabricationConsts.RESIST_STATION_ID) return lookup.ResistInstructions;
            else if (stationID == FabricationConsts.PHOTOLITHOGRAPHY_STATION_ID) return lookup.PhotolithographyInstructions;
            else if (stationID == FabricationConsts.ETCH_STATION_ID) return lookup.EtchInstructions;
            else if (stationID == FabricationConsts.ION_STATION_ID) return lookup.IonInstructions;
            else if (stationID == FabricationConsts.SPUTTER_STATION_ID) return lookup.SputterInstructions;
            else return null;
        }
    }
}