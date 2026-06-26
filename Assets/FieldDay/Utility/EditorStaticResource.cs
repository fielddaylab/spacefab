using BeauUtil;
using System;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using UnityEngine;
using System.Collections.Generic;
using System.Reflection;



#if UNITY_EDITOR
using UnityEditor;
#endif // UNITY_EDITOR

namespace FieldDay {
    [Conditional("UNITY_EDITOR")]
    [AttributeUsage(AttributeTargets.Method, AllowMultiple = false, Inherited =  false)]
    public sealed class EditorStaticResource : Attribute {
        [Conditional("UNITY_EDITOR")]
        static public void SetupLifetime(Action create, Action destroy, QuitMode quitMode = QuitMode.SkipDuringQuit) {
#if UNITY_EDITOR
            if (!s_Registered.Add(create.Method)) {
                return;
            }

            EditorApplication.playModeStateChanged += (state) => {
                if (state == PlayModeStateChange.ExitingEditMode) {
                    destroy();
                } else if (state == PlayModeStateChange.EnteredEditMode) {
                    create();
                }
            };

            if (quitMode == QuitMode.ExecuteDuringQuit) {
                EditorApplication.quitting += destroy;
            }

            AppDomain.CurrentDomain.DomainUnload += (_, __) => {
                if (!s_Quitting) {
                    destroy();
                }
            };
#endif // UNITY_EDITOR
        }

#if UNITY_EDITOR
        static private HashSet<MethodInfo> s_Registered = new HashSet<MethodInfo>();

        [InitializeOnLoadMethod]
        static private void SetupAll() {
            foreach(var method in TypeCache.GetMethodsWithAttribute(typeof(EditorStaticResource))) {
                method.Invoke(null, null);
            }

            Application.quitting += () => s_Quitting = true;
        }

        static private bool s_Quitting = false;

        static public bool IsQuitting {
            get { return s_Quitting; }
        }
#else
        public const bool IsQuitting = false;
#endif // UNITY_EDITOR

        public enum QuitMode {
            SkipDuringQuit,
            ExecuteDuringQuit,
        }
    }
}