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
    public class OverarchingRenderPose : BatchedComponent {
        public OverarchingRenderPlane[] Planes;
        public ActiveGroup Activate;
    }
}