using BeauUtil;
using FieldDay.Assets;
using FieldDay.Components;
using Leaf;
using SpaceFab.Overarching;
using System;
using UnityEngine;

namespace SpaceFab {
    [CreateAssetMenu(menuName = "SpaceFab/Scripting/Scoped Scripts Asset")]
    public sealed class ScopedScriptsAsset : NamedAsset {
        [Serializable]
        public struct Binding {
            public Mask Scope;
            public LeafAsset Script;
        }

        [Flags]
        public enum Mask {
            None = 0,

            Onboarding = 0x01,
            Research = 0x02,
            Design = 0x04,
            Fab = 0x08,
            Supply = 0x10
        }

        public Binding[] Bindings;
    }
}
