using BeauUtil;
using BeauUtil.Debugger;
using FieldDay.Collections;
using UnityEngine;

namespace FieldDay.Systems {
    /// <summary>
    /// System module component.
    /// </summary>
    public abstract class SystemComponent : MonoBehaviour, ISystemModuleComponent {
        public abstract unsafe void RegisterSystems(ref SystemRegistrationTable ecs);
    }
}