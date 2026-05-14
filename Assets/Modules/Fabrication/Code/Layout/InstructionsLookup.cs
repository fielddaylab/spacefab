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
    public enum KeyType
    {
        Space,
        LRArrows,
        FullArrows,
        Mouse,
        ADKeys
    }

    [CreateAssetMenu(menuName = "SpaceFab/Fabrication/InstructionLookup")]
    public class InstructionsLookup : GlobalAsset
    {
        [Header("Furnace Instructions")]
        public InstructionsSet FurnaceInstructions;

        [Header("Resist Instructions")]
        public InstructionsSet ResistInstructions;

        [Header("Photolithography Instructions")]
        public InstructionsSet PhotolithographyInstructions;

        [Header("Etch Instructions")]
        public InstructionsSet EtchInstructions;

        [Header("Ion Instructions")]
        public InstructionsSet IonInstructions;

        [Header("Sputter Instructions")]
        public InstructionsSet SputterInstructions;
    }

    /// <summary>
    /// Instruction set class for use in displaying the correct image and instruction
    /// </summary>
    [System.Serializable]
    public class InstructionsSet
    {
        public KeyType SputterKey;
        public string SputterInstruction;
        public string SputterSubtitle;
    }

    /// <summary>
    /// Returns corresponding instruction set for the interfacer as described by the lookup
    /// </summary>
    public static class InstructionsLookupUtility
    {
        public static InstructionsSet LookupInstructions(MicrogameStationInterfacer interfacer, InstructionsLookup lookup)
        {
            if (interfacer.Id == FabricationConsts.FURNACE_STATION_ID) return lookup.FurnaceInstructions;
            else if (interfacer.Id == FabricationConsts.RESIST_STATION_ID) return lookup.ResistInstructions;
            else if (interfacer.Id == FabricationConsts.PHOTOLITHOGRAPHY_STATION_ID) return lookup.PhotolithographyInstructions;
            else if (interfacer.Id == FabricationConsts.ETCH_STATION_ID) return lookup.EtchInstructions;
            else if (interfacer.Id == FabricationConsts.ION_STATION_ID) return lookup.IonInstructions;
            else if (interfacer.Id == FabricationConsts.SPUTTER_STATION_ID) return lookup.SputterInstructions;
            else return null;
        }
    }
}