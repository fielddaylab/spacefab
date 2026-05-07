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
        public LevelData DesignLevelData;

        // Sequence definition for the Fabrication minigame under this contract. Populated into
        // SequenceState on minigame entry via FabricationMinigameState.ImportState.
        public FabricationSequenceLevel FabricationLevel;

        public int Payout;
    }
}
