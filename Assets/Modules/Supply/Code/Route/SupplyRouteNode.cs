using BeauUtil;
using FieldDay;
using FieldDay.Assets;
using FieldDay.Components;
using SpaceFab.Materials;
using System;
using UnityEngine;

namespace SpaceFab.Supply {
    public sealed class SupplyRouteNode : BatchedComponent, IRegistrationCallbacks {
        public SupplyRouteNodeType Type;

        [Header("Stats")]
        [Range(0, 4)] public int Time;
        [Range(0, 4)] public int Cost;
        [Range(0, 4)] public int Risk;

        [Header("Materials")]
        [AssetName(typeof(MaterialAsset), true)] public StringHash32 MaterialType;
        [AssetName(typeof(MaterialAsset), true)] public StringHash32 ConversionInputType;

        [Header("Components")]
        public Collider2D Collider;

        [NonSerialized] public StringHash32 Id;
        [NonSerialized] public int Index;

        void IRegistrationCallbacks.OnRegister() {
            Id = name;
        }

        void IRegistrationCallbacks.OnDeregister() {
        }
    }

    public enum SupplyRouteNodeType : byte {
        Home,
        Producer,
        Converter
    }
}