using FieldDay;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace SpaceFab.UI
{
    /// <summary>
    /// Additional functionality to the Button class. Allows for filtering according to allowed inputs
    /// </summary>
    public class DynamicButton : Button, IPointerEnterHandler, IPointerExitHandler
    {
        new public ButtonClickedEvent onClick = new ButtonClickedEvent();
        public UnityEvent onPointerEnter = new UnityEvent();
        public UnityEvent onPointerExit = new UnityEvent();

        protected override void Start()
        {
            base.Start();

            base.onClick.AddListener(FilterOnClick);
        }

        private void FilterOnClick()
        {
            if (!PassesFilter()) { return; }

            onClick?.Invoke();
        }

        public override void OnPointerEnter(PointerEventData eventData)
        {
            base.OnPointerEnter(eventData);

            if (!PassesFilter()) { return; }
            onPointerEnter?.Invoke();
        }

        public override void OnPointerExit(PointerEventData eventData)
        {
            base.OnPointerExit(eventData);

            if (!PassesFilter()) { return; }
            onPointerExit?.Invoke();
        }

        private bool PassesFilter()
        {
            //var input = Find.State<InputState>();
            //if ((input.AppliedLayerMask & (1 << this.gameObject.layer)) == 0) { return false; }
            // else { return true; }
            return true;
        }
    }
}