using FieldDay.Assets;
using SpaceFab.Design;
using SpaceFab.Fabrication.Sequence;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace SpaceFab
{
    [CreateAssetMenu(menuName ="SpaceFab/Overarching/Contract Asset Wrapper")]
    public class ContractAssetsWrapper : NamedAsset
    {
        public ContractDef ContractDef;

        // Ordered list of Design levels for this contract. The player works through them in
        // sequence; the contract is solved only once every level is solved. Index 0 is the first
        // level. Was a single LevelData before contracts gained multiple Design levels.
        public LevelData[] DesignLevels;

        // Sequence definition for the Fabrication minigame under this contract. Populated into
        // SequenceState on minigame entry via FabricationMinigameState.ImportState.
        public FabricationSequenceLevel FabricationLevel;
    }
}
