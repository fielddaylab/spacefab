using FieldDay;
using FieldDay.Rendering;
using FieldDay.SharedState;
using System;
using UnityEngine;

namespace SpaceFab.Supply {
    public sealed class SupplyRouteLineMaterialAnimationState : SharedStateComponent, IRegistrationCallbacks {
        public Material[] ScrollMaterials;
        public float ScrollSpeed = 1;
        
        [NonSerialized] public float CurrentScroll;

        void IRegistrationCallbacks.OnDeregister() {
            foreach(var material in ScrollMaterials) {
                material.SetTextureOffset(DefaultShaderProps._MainTex, default);
            }
        }

        void IRegistrationCallbacks.OnRegister() {
        }
    }
}