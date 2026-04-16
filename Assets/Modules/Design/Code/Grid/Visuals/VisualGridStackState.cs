using FieldDay;
using FieldDay.SharedState;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace SpaceFab.Design.Visuals
{
    public class VisualGridStackState : SharedStateComponent, IRegistrationCallbacks
    {
        public SpriteRenderer GridRenderer;
        public VisualGridStack VisualGridStack;
        public GameObject CellVisualsPrefab;
        public Transform CellVisualsContainer;

        // TODO: set to true when:
        // LayoutChanged
        // Visual Feedback Routine
        // ResetFlow States
        // Result closed clicked
        [HideInInspector] public bool VisualsNeedRefreshing;

        public void OnDeregister()
        {
        }

        public void OnRegister()
        {
            VisualGridStack = new VisualGridStack();
        }
    }
}