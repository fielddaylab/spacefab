using System;
using BeauUtil.Debugger;
using FieldDay;
using FieldDay.Components;
using FieldDay.SharedState;
using Leaf.Runtime;
using SpaceFab.Save;
using UnityEngine;
using UnityEngine.UI;

namespace SpaceFab.Overarching {
    public class MinigameZoneStatusIcon : BatchedComponent {
        public SpriteRenderer Fill;
        public SpriteRenderer Icon;
        public Transform Face;
        public Transform Shadow;
        public float TravelDistance;
    }
}
