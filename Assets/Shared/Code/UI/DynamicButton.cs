using FieldDay;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace SpaceFab.UI
{
    /// <summary>
    /// Additional functionality to the Button class. Allows for filtering according to allowed inputs
    /// </summary>
    public class DynamicButton : Button
    {
        new public ButtonClickedEvent onClick = new ButtonClickedEvent();

        protected override void Start()
        {
            base.Start();

            base.onClick.AddListener(FilterOnClick);
        }

        private void FilterOnClick()
        {
            var input = Find.State<InputState>();
            if ((input.AppliedLayerMask & (1 << this.gameObject.layer)) == 0) { return; }

            onClick?.Invoke();
        }
    }
}