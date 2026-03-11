using BeauUtil;
using BeauUtil.Debugger;
using FieldDay;
using FieldDay.SharedState;
using FieldDay.UI;
using System;
using UnityEngine;
using UnityEngine.EventSystems;

namespace SpaceFab
{
    public class InputState : SharedStateComponent, IRegistrationCallbacks
    {
        public bool InputEnabled = true;
        public PhysicsRaycaster Raycaster;
        public bool BlockAllInput = false;

        [NonSerialized] public int DesiredLayerMask;
        [NonSerialized] public int LayerMaskFilter = Bits.All32;
        [NonSerialized] public int AppliedLayerMask;

        public void OnDeregister()
        {
            // Handles cases where we might be in the middle of a transition that we are going to stop early
            if (!InputEnabled)
            {
                InputUtility.SetInputEnabled(this, true);
            }
        }

        public void OnRegister()
        {
            ReassignRaycaster();
            InputUtility.SetClickableMaskDefault(this);
        }

        private void Start()
        {
            ReassignRaycaster();
        }

        private void ReassignRaycaster()
        {
            if (Raycaster == null)
            {
                if (TransformHelper.TryGetCameraFromLayer(transform, out Camera camera))
                {
                    Raycaster = camera.GetComponent<PhysicsRaycaster>();
                }
            }
        }
    }

    public static class InputUtility
    {
        public const int DefaultLayerMask = LayerMasks.Default_Mask;

        public static void SetInputEnabled(InputState state, bool enabled)
        {
            // Log.Msg("[InputState] called SetInputEnabled({0})", enabled);
            bool changed = Ref.Replace(ref state.InputEnabled, enabled);
            if (!changed) return;

            state.Raycaster.enabled = state.InputEnabled;

            if (enabled)
            {
                Log.Msg("[InputState > SetInputEnabled] ResumeRaycasts");
                SetClickableMaskDefault(state);
            }
            else
            {
                Log.Msg("[InputState > SetInputEnabled] PauseRaycasts");
                // SetClickableMaskCustom(state, LayerMasks.UI_Mask);
            }
        }

        public static void SetClickableMaskDefault(InputState state)
        {
            state.DesiredLayerMask = DefaultLayerMask;
            state.AppliedLayerMask = CalculateFinalMask(state.DesiredLayerMask, state.LayerMaskFilter);
            state.Raycaster.eventMask = state.AppliedLayerMask;
        }

        public static void SetClickableMaskCustom(InputState state, LayerMask mask)
        {
            state.DesiredLayerMask = mask;
            state.AppliedLayerMask = CalculateFinalMask(state.DesiredLayerMask, state.LayerMaskFilter);
            state.Raycaster.eventMask = state.AppliedLayerMask;
        }

        public static void SetClickableMaskFilter(InputState state, LayerMask filter)
        {
            state.LayerMaskFilter = filter;
            state.AppliedLayerMask = CalculateFinalMask(state.DesiredLayerMask, state.LayerMaskFilter);
            state.Raycaster.eventMask = state.AppliedLayerMask;
        }

        static private int CalculateFinalMask(int desiredMask, int filter)
        {
            int mask = /*(LayerMasks.TopLayer_Mask & desiredMask) |*/ (desiredMask & filter);
            return mask;
        }

        public static bool IsClickable(InputState state, GameObject gameObject)
        {
            return RaycastUtility.IsInteractableByRaycaster(gameObject, state.Raycaster);
        }
    }
}