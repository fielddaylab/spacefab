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
using FieldDay.UI;
using FieldDay.UI.Widgets;

namespace SpaceFab.Title
{
    [PreloadOrder(-10)]
    [RequireComponent(typeof(Canvas))]
    public class TitleCanvasLayer : MonoBehaviour, IScenePreload {
        public Canvas Canvas;
        public CanvasInputLayer Input;
        public CanvasGroup Fader;
        public LayoutOffset Offset;

        [Header("Close")]
        public GuiButton CloseButton;

        [NonSerialized] public Routine Animation;
        [NonSerialized] public bool Visible;

        IEnumerator<WorkSlicer.Result?> IScenePreload.Preload() {
            Canvas.enabled = false;
            Input.SetInputOverride(false);
            Fader.alpha = 0;
            Offset.Offset0 = default;
            Visible = false;
            return null;
        }

        public void Show() {
            if (!Visible) {
                Visible = true;
                Animation.Replace(this, ShowRoutine());
            }
        }

        public void Hide() {
            if (Visible) {
                Visible = false;
                Animation.Replace(this, HideRoutine());
            }
        }

        private IEnumerator ShowRoutine() {
            Canvas.enabled = true;
            Canvas.sortingOrder = 0;
            Input.SetInputOverride(false);
            Fader.alpha = 0;
            Offset.Offset0 = new Vector2(0, -24);
            yield return Routine.Combine(
                Fader.FadeTo(1, 0.2f),
                Offset.Offset0To(default, 0.2f).Ease(Curve.CubeOut)
            );
            Input.SetInputOverride(null);
        }

        private IEnumerator HideRoutine() {
            Input.SetInputOverride(false);
            Canvas.sortingOrder = 1;
            yield return Routine.Combine(
                Fader.FadeTo(0, 0.2f),
                Offset.Offset0To(new Vector2(0, 24), 0.2f).Ease(Curve.CubeIn)
            );
            Canvas.enabled = false;
        }
    }
}