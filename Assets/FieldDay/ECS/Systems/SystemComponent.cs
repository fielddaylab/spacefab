using BeauUtil;
using BeauUtil.Debugger;
using System;
using System.Diagnostics;
using System.Reflection;
using UnityEngine;

namespace FieldDay.Systems {
    /// <summary>
    /// System module component.
    /// </summary>
    public abstract class SystemComponent : MonoBehaviour, ISystemModuleComponent {
        /// <summary>
        /// Indicates that a field within a system class is not stateful.
        /// Use only for work buffers and readonly constants.
        /// </summary>
        [Conditional("UNITY_EDITOR")]
        [AttributeUsage(AttributeTargets.Field, AllowMultiple = false, Inherited = false)]
        protected sealed class NotStateful : Attribute { }

        public abstract unsafe void RegisterSystems(ref SystemRegistrationTable ecs);

        #region Validation

#if UNITY_EDITOR
        [UnityEditor.InitializeOnLoadMethod]
        [UnityEditor.MenuItem("Field Day/Testing/Audit SystemComponents")]
        static private void CheckTypes() {
            foreach (var subclass in Reflect.FindAllDerivedTypes(typeof(SystemComponent))) {
                int subclassFieldCount = GetAllStatefulFieldsCount(subclass);
                if (subclassFieldCount != 0) {
                    Log.Error("[SystemComponent] SystemComponent type '{0}' has {1} stateful fields, in violation of ECS best practices. Use alternate means to drive behavior, or add a [NotStateful] attribute to the fields that are not stateful.", subclass.FullName, subclassFieldCount);
                }
            }
        }

        static private int GetAllStatefulFieldsCount(Type type) {
            const BindingFlags search = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static | BindingFlags.DeclaredOnly;
            const FieldAttributes excludeFields = FieldAttributes.Literal;

            var fields = type.GetFields(search);
            int nonWorkFieldCount = 0;
            foreach (var field in fields) {
                if ((field.Attributes & excludeFields) != 0 || field.IsDefined(typeof(NotStateful))) {
                    continue;
                }

                nonWorkFieldCount++;
            }
            return nonWorkFieldCount;
        }
#endif // UNITY_EDITOR

        #endregion // Validation
    }
}