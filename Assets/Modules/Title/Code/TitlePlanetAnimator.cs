using BeauRoutine;
using BeauUtil;
using FieldDay;
using FieldDay.Scenes;
using SpaceFab.Save;
using SpaceFab;
using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using FieldDay.Debugging;
using SpaceFab.Comic;
using FieldDay.Assets;
using FieldDay.UI.Widgets;

namespace SpaceFab.Title
{
    public class TitlePlanetAnimator : MonoBehaviour {
        public float RotationSpeed;
        
        private void LateUpdate() {
            transform.Rotate(0, 0, RotationSpeed * Frame.DeltaTime, Space.Self);
        }
    }
}