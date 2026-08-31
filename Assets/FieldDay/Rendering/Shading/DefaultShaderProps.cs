using UnityEngine;

namespace FieldDay.Rendering {
    static public class DefaultShaderProps {
        static public int MainTex { get; private set; }
        static public int MainTexScaleOffset { get; private set; }

        static public int Color { get; private set; }

        static public int LerpColor { get; private set; }

        static public int AdditiveColor { get; private set; }

        static public int IntensityColorThreshold { get; private set; }

        static public int IntensityColorMinThreshold { get; private set; }

        static public int IntensityAlphaThreshold { get; private set; }

        static public int IntensityAlphaMinThreshold { get; private set; }

        static public int ZWrite { get; private set; }

        static public int ZTest { get; private set; }

        static public int Cull { get; private set; }

        #if UNITY_EDITOR
        [UnityEditor.InitializeOnLoadMethod]
        #endif // UNITY_EDITOR
        static internal void Initialize() {
            MainTex = Shader.PropertyToID("_MainTex");
            MainTexScaleOffset = Shader.PropertyToID("_MainTex_ST");
            Color = Shader.PropertyToID("_Color");
            LerpColor = Shader.PropertyToID("_LerpColor");
            AdditiveColor = Shader.PropertyToID("_AdditiveColor");
            IntensityColorThreshold = Shader.PropertyToID("_IntensityColorThreshold");
            IntensityColorMinThreshold = Shader.PropertyToID("_IntensityColorMinThreshold");
            IntensityAlphaThreshold = Shader.PropertyToID("_IntensityAlphaThreshold");
            IntensityAlphaMinThreshold = Shader.PropertyToID("_IntensityAlphaMinThreshold");
            ZWrite = Shader.PropertyToID("_ZWrite");
            ZTest = Shader.PropertyToID("_ZTest");
            Cull = Shader.PropertyToID("_Cull");
        }
    }
}