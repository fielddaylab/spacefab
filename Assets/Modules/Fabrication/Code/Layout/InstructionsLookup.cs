using FieldDay.Assets;
using SpaceFab.Fabrication.Stations;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace SpaceFab.Fabrication.Layout
{
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
        public KeyType FurnaceKey;
        public string FurnaceInstruction;
        public string FurnaceSubtitle;

        [Header("Resist Instructions")]
        public KeyType ResistKey;
        public string ResistInstruction;
        public string ResistSubtitle;

        [Header("Photolithography Instructions")]
        public KeyType PhotolithographyKey;
        public string PhotolithographyInstruction;
        public string PhotolithographySubtitle;

        [Header("Etch Instructions")]
        public KeyType EtchKey;
        public string EtchInstruction;
        public string EtchSubtitle;

        [Header("Ion Instructions")]
        public KeyType IonKey;
        public string IonInstruction;
        public string IonSubtitle;

        [Header("Sputter Instructions")]
        public KeyType SputterKey;
        public string SputterInstruction;
        public string SputterSubtitle;
    }

    public static class InstructionsLookupUtility
    {
        public static void LookupInstructions(MicrogameStationInterfacer interfacer)
        {

        }
    }
}