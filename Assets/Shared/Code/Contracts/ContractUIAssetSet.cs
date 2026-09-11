using BeauUtil;
using BeauUtil.Debugger;
using FieldDay.Assets;
using System;
using UnityEngine;

namespace SpaceFab {
    [CreateAssetMenu(menuName = "SpaceFab/Contract UI Assets")]
    public sealed class ContractUIAssetSet : GlobalAsset {
        [Serializable]
        public struct DifficultyBoxConfig {
            public Color32 Background;
            public string Label;
        }

        [Serializable]
        public struct TypeConfig {
            public Color32 Background;
            public Color32 Outline;
            public Sprite Icon;
            public bool ShowSparkles;
            public string Label;
        }

        [Serializable]
        public struct ClientConfig {
            public SerializedHash32 Id;
            public Sprite Icon;
            public string Text;
        }

        public TypeConfig[] Types;
        public DifficultyBoxConfig[] Difficulties;
        public ClientConfig[] Clients;
    }

    static public partial class ContractUIUtility {
        static public ContractUIAssetSet.ClientConfig GetClientInfo(ContractUIAssetSet assetSet, StringHash32 clientName) {
            foreach(var set in assetSet.Clients) {
                if (set.Id == clientName) {
                    return set;
                }
            }

            Log.Error("Unknown contract client '{0}'", clientName);
            return assetSet.Clients[0];
        }
    }
}