using BeauUtil;
using BeauUtil.Debugger;
using BeauUtil.Editor;
using FieldDay.Rendering;
using ScriptableBake;
using System.Collections.Generic;
using System.Text;
using UnityEditor;
using UnityEngine;

namespace FieldDay.Editor {
    static public class MeshRendererHelpers {
        [MenuItem("CONTEXT/MeshRenderer/Disable Fancy Features")]
        static private void ContextDisableLighting(MenuCommand cmd) {
            MeshRenderer renderer = (MeshRenderer) cmd.context;
            DisableFancyFeatures(renderer);
        }

        static private void DisableFancyFeatures(MeshRenderer renderer) {
            Baking.PrepareUndo(renderer, "Disabling fancy features");
            renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            renderer.receiveShadows = false;
            renderer.lightProbeUsage = UnityEngine.Rendering.LightProbeUsage.Off;
            renderer.reflectionProbeUsage = UnityEngine.Rendering.ReflectionProbeUsage.Off;
            renderer.motionVectorGenerationMode = MotionVectorGenerationMode.ForceNoMotion;
            renderer.staticShadowCaster = false;
            renderer.allowOcclusionWhenDynamic = false;
        }
    }
}