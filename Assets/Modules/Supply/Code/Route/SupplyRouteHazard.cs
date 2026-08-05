using BeauUtil;
using BeauUtil.UI;
using FieldDay;
using FieldDay.Assets;
using FieldDay.Components;
using FieldDay.Scripting;
using FieldDay.UI;
using SpaceFab.Materials;
using System;
using UnityEngine;
using BeauPools;

#if UNITY_EDITOR
using UnityEditor;
#endif // UNITY_EDITOR

namespace SpaceFab.Supply {
    public sealed class SupplyRouteHazard : BatchedComponent {
        [Header("Stats")]
        [Range(0, 5)] public int Cost;
        [Range(0, 3)] public int Risk;
        [Range(0, 5)] public int Time;

        [Header("Components")]
        public Collider2D Collider;
        public CursorHint Cursor;

        [NonSerialized] public StringHash32 Id;
        [NonSerialized] public int Index;
    }
}