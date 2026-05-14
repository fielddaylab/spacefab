using FieldDay.Assets;
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
        Mouse
    }

    [CreateAssetMenu(menuName = "SpaceFab/Fabrication/InstructionLookup")]
    public class InstructionsLookup : GlobalAsset
    {
        [Header("Furnace Instructions")]
        public KeyType FurnaceKey;
        public string FurnaceInstruction;
        public string FurnaceSubtitle;

        
    }

    public static class InstructionsLookupUtility
    {
        public static void LookupInstructions()
        {

        }
    }
}