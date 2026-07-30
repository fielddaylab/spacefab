using BeauRoutine;
using BeauUtil;
using FieldDay;
using FieldDay.Components;
using FieldDay.SharedState;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace SpaceFab.Overarching
{
    public class OverarchingRenderPlane : BatchedComponent {
        public float Distance;

        protected override void OnEnable() {
            base.OnEnable();

            transform.localScale = new Vector3(Distance, Distance, Distance);
            transform.localPosition = new Vector3(0, 0, Distance);
            transform.SetParent(Game.Rendering.PrimaryCamera.transform, false);
        }
    }
}