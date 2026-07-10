using System;
using FieldDay.Components;
using UnityEngine;

namespace SpaceFab.Parallax {
    public sealed class ParallaxLayer : BatchedComponent {
        [NonSerialized] public Transform Root;
        public float Scale = 1;
    }
}