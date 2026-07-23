using BeauUtil;
using System;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using UnityEngine;
using System.Collections.Generic;
using System.Reflection;
using BeauUtil.Debugger;

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

            Log.Trace("[EditorStaticResource] Registered resource '{0}::{1}'", create.Method.DeclaringType.FullName, create.Method.Name);

            s_CreateActions.Add(create);

            if (quitMode == QuitMode.ExecuteDuringQuit) {
                s_QuitActions.Add(destroy);
            }

            s_UnloadActions.Add(destroy);
#endif // UNITY_EDITOR
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static public bool IsEditor() {
#if UNITY_EDITOR
            return !EditorApplication.isPlayingOrWillChangePlaymode;
#else
            return false;
#endif // UNITY_EDITOR
        }

#if UNITY_EDITOR
        static private HashSet<MethodInfo> s_Registered = new HashSet<MethodInfo>();
        static private List<Action> s_CreateActions = new List<Action>();
        static private List<Action> s_QuitActions = new List<Action>();
        static private List<Action> s_UnloadActions = new List<Action>();

        [InitializeOnLoadMethod]
        static private void SetupAll() {
            EditorApplication.quitting += HandleQuit;
            AssemblyReloadEvents.beforeAssemblyReload += HandleAssemblyUnload;
            EditorApplication.playModeStateChanged += HandleEditorStateChange;

            foreach (var method in TypeCache.GetMethodsWithAttribute(typeof(EditorStaticResource))) {
                method.Invoke(null, null);
            }
        }

        static private void HandleQuit() {
            if (s_Quitting) {
                return;
            }

            s_Quitting = true;
            Log.Trace("[EditorStaticResource] Quit detected");
            InvokeActions(s_QuitActions, "destroy");
        }

        static private void HandleEditorStateChange(PlayModeStateChange state) {
            if (s_Quitting) {
                return;
            }

            if (state == PlayModeStateChange.ExitingEditMode) {
                Log.Trace("[EditorStaticResource] Exiting edit mode detected");
                InvokeActions(s_UnloadActions, "destroy");
            } else if (state == PlayModeStateChange.EnteredEditMode) {
                Log.Trace("[EditorStaticResource] Entering edit mode detected");
                InvokeActions(s_CreateActions, "create");
            }
        }

        static private void HandleAssemblyUnload() {
            if (s_Quitting) {
                return;
            }

            Log.Trace("[EditorStaticResource] Domain unload detected");
            InvokeActions(s_UnloadActions, "destroy");
        }

        static private void InvokeActions(List<Action> actions, string funcType) {
            foreach (var action in actions) {
                Log.Trace("[EditorStaticResource] Calling {0} '{1}::{2}'...", funcType, action.Method.DeclaringType.FullName, action.Method.Name);
                action();
            }
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