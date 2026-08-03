using BeauUtil;
using BeauUtil.UI;
using FieldDay;
using FieldDay.Components;
using FieldDay.Scenes;
using FieldDay.UI;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

namespace SpaceFab.Overarching
{
    public class MinigameZoneOverlay : BatchedComponent, IScenePreload
    {
        [Header("Highlight")]
        public SpriteRenderer HighlightOutline;
        public SpriteRenderer HighlightFill;

        [Header("Name")]
        public GameObject NameBadge;
        public SpriteRenderer NameFill;

        [Header("Complete Icon")]
        public GameObject CompletedBadge;

        [Header("Colors")]
        public Color ThemeColor;

        private void Awake() {
            
        }

        IEnumerator<WorkSlicer.Result?> IScenePreload.Preload() {
            NameBadge.SetActive(false);
            CompletedBadge.SetActive(false);

            HighlightOutline.enabled = HighlightFill.enabled = false;
            return null;
        }
    }
}