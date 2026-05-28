using BeauUtil;
using BeauUtil.Debugger;
using FieldDay.Collections;
using UnityEngine;

namespace FieldDay.Systems {
    /// <summary>
    /// System module component.
    /// </summary>
    public abstract class SystemComponent : MonoBehaviour, ISystemModuleComponent {
#if UNITY_EDITOR
        [UnityEditor.InitializeOnLoadMethod]
        static private void CheckTypes() {
            int baseFieldCount = typeof(SystemComponent).GetFields().Length;
            foreach(var subclass in Reflect.FindAllDerivedTypes(typeof(SystemComponent))) {
                if (subclass.GetFields().Length != baseFieldCount) {
                    Log.Error("[SystemComponent] SystemComponent type '{0}' has fields, in violation of ECS best practices.", subclass.FullName);
                }
            }
        }
#endif // UNITY_EDITOR

        public abstract unsafe void RegisterSystems(ref SystemRegistrationTable ecs);
    }
}