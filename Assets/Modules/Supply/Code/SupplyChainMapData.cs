using BeauUtil;
using FieldDay.Assets;
using System;
using UnityEngine;

namespace SpaceFab.Supply
{
    [CreateAssetMenu(menuName = "SpaceFab/Supply/Map Data")]
    public class SupplyChainMapData : ScriptableObject {
        [Serializable]
        public struct NodeData {
            public StringHash32 Name;
            public Vector2 Position;
        }

        [Serializable]
        public struct NodeOverride {
            public StringHash32 Name;
            public int Time;
            public int Cost;
            public int Risk;
        }

        // TODO: backgrounds
        // TODO: additional assets
        // [AssetName(typeof(StreamedPack))] public StringHash32 AdditionalAssets;

        [AssetName(typeof(SupplyShipAsset))] public StringHash32[] ShipIds;
        public Vector2 CameraBounds;
        public NodeData[] Positions;
        public NodeOverride[] Overrides;
    }
}
