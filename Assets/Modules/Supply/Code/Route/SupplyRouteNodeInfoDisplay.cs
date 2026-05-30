using BeauUtil;
using FieldDay;
using FieldDay.Assets;
using FieldDay.Components;
using FieldDay.UI;
using SpaceFab.Materials;
using System;
using UnityEngine;

namespace SpaceFab.Supply {
    public sealed class SupplyRouteNodeInfoDisplay : BatchedComponent {
        [Header("Material")]
        public Transform MaterialBubble;
        public SpriteRenderer InputMaterialIcon;
        public SpriteRenderer OutputMaterialIcon;
        public CursorHint Cursor;
    }
}