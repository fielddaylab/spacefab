using BeauUtil.Debugger;
using System.Reflection;
using UnityEngine;
using FieldDay.Assets;
using TinyIL;
using System;
using System.Runtime.CompilerServices;





#if UNITY_EDITOR
using UnityEditor;
#endif // UNITY_EDITOR

namespace FieldDay.Rendering {
    static public class MaterialUtility {
        static public void SetDefaultUIGraphicMaterial(Material material) {
            Assert.NotNullOrDestroyed(material);
            typeof(UnityEngine.UI.Graphic).GetField("s_DefaultUI", BindingFlags.NonPublic | BindingFlags.Static).SetValue(null, material);
            Log.Msg("[MaterialUtility] Replaced default UI material with {0}", material.name);
        }

        /// <summary>
        /// Destroys the given property block.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static public void DestroyPropertyBlock(ref MaterialPropertyBlock block) {
            if (block != null) {
                DestroyPropertyBlock(block);
                block = null;
            }
        }

        /// <summary>
        /// Destroys the given property block.
        /// </summary>
        [IntrinsicIL("ldarg.0; call [arg block]::Dispose(); ret;")]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static public void DestroyPropertyBlock(MaterialPropertyBlock block) {
            throw new NotImplementedException();
        }

#if UNITY_EDITOR
        [InitializeOnLoadMethod]
        static private void EditorInitialize() {
            EditorApplication.delayCall += () => {
                Material attemptedMaterial = AssetDatabase.LoadAssetAtPath<Material>("Assets/FieldDay/_Assets/Materials/UI/UI-Premultiplied.mat");
                if (attemptedMaterial != null) {
                    SetDefaultUIGraphicMaterial(attemptedMaterial);
                }
            };
        }
#endif // UNITY_EDITOR
    }
}