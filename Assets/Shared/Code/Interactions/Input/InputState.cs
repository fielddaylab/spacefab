using BeauUtil;
using BeauUtil.Debugger;
using FieldDay;
using FieldDay.HID;
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
            InputUtility.TryReassignRaycaster(this);
            InputUtility.SetClickableMaskDefault(this);
        }

        private void Start()
        {
            InputUtility.TryReassignRaycaster(this);
        }
    }

    public static class InputUtility
    {
        public const int DefaultLayerMask = LayerMasks.Default_Mask | LayerMasks.UI_Mask| LayerMasks.Interrupt_UI_Mask;

        public static void SetInputEnabled(InputState state, bool enabled)
        {
            EnsureRaycasterAssigned(state);

            // Log.Msg("[InputState] called SetInputEnabled({0})", enabled);
            bool changed = Ref.Replace(ref state.InputEnabled, enabled);

            // Always sync the raycaster to the desired state, even when InputEnabled didn't change.
            // After a scene reload the new InputState defaults to InputEnabled=true while its
            // camera's raycaster may still be off; a redundant SetInputEnabled(true) must still
            // guarantee the raycaster is on rather than early-returning on the no-change path.
            if (state.Raycaster != null)
            {
                state.Raycaster.enabled = state.InputEnabled;
            }

            // The clickable-mask reset only needs to run on an actual transition.
            if (!changed) return;

            if (enabled)
            {
                Log.Msg("[InputState > SetInputEnabled] ResumeRaycasts");
                SetClickableMaskDefault(state);
            }
            else
            {
                Log.Msg("[InputState > SetInputEnabled] PauseRaycasts");
                SetClickableMaskCustom(state, 0);
            }
        }

        public static void SetClickableMaskDefault(InputState state)
        {
            EnsureRaycasterAssigned(state);
            state.DesiredLayerMask = DefaultLayerMask;
            state.AppliedLayerMask = CalculateFinalMask(state.DesiredLayerMask, state.LayerMaskFilter);
            state.Raycaster.eventMask = state.AppliedLayerMask;
        }

        public static void SetClickableMaskCustom(InputState state, LayerMask mask)
        {
            EnsureRaycasterAssigned(state);
            state.DesiredLayerMask = mask;
            state.AppliedLayerMask = CalculateFinalMask(state.DesiredLayerMask, state.LayerMaskFilter);
            state.Raycaster.eventMask = state.AppliedLayerMask;
        }

        public static void SetClickableMaskFilter(InputState state, LayerMask filter)
        {
            EnsureRaycasterAssigned(state);
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

        public static bool TryReassignRaycaster(InputState state)
        {
            if (state.Raycaster == null)
            {
                if (TransformHelper.TryGetCameraFromLayer(state.transform, out Camera camera))
                {
                    state.Raycaster = camera.GetComponent<PhysicsRaycaster>();
                    return true;
                }
            }

            return false;
        }

        public static void EnsureRaycasterAssigned(InputState state)
        {
            if (state.Raycaster == null) 
            {
                TryReassignRaycaster(state);
            }
        }
    }
}