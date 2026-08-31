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
    public class TitleSilhouetteAnimator : MonoBehaviour {
        public float MoveX;
        public float MoveXPeriod;
        public float MoveXOffset;

        public float MoveY;
        public float MoveYPeriod;
        public float MoveYOffset;

        [NonSerialized] private Vector3 m_Anchor;
        [NonSerialized] private float m_TimeAnchor;

        private void OnEnable() {
            m_Anchor = transform.localPosition;
            m_TimeAnchor = Time.timeSinceLevelLoad;
        }

        private void OnDisable() {
            transform.localPosition = m_Anchor;
        }

        private void LateUpdate() {
            float time = Time.timeSinceLevelLoad - m_TimeAnchor;
            Vector3 pos = m_Anchor;
            pos.x += Mathf.Cos(Mathf.PI * 2 * (time * MoveXPeriod + MoveXOffset)) * MoveX;
            pos.y += Mathf.Sin(Mathf.PI * 2 * (time * MoveYPeriod + MoveYOffset)) * MoveY;
            transform.localPosition = pos;
        }
    }
}