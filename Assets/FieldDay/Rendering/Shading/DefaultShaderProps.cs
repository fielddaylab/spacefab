using UnityEngine;

namespace FieldDay.Rendering {
    static public class DefaultShaderProps {
        static public int _MainTex { get; private set; }

        static public int _Color { get; private set; }

        static public int _LerpColor { get; private set; }

        static public int _AdditiveColor { get; private set; }

        static public int _IntensityColorThreshold { get; private set; }

        static public int _IntensityColorMinThreshold { get; private set; }

        static public int _IntensityAlphaThreshold { get; private set; }

        static public int _IntensityAlphaMinThreshold { get; private set; }

        static internal void Initialize() {
            _MainTex = Shader.PropertyToID("_MainTex");
            _Color = Shader.PropertyToID("_Color");
            _LerpColor = Shader.PropertyToID("_LerpColor");
            _AdditiveColor = Shader.PropertyToID("_AdditiveColor");
            _IntensityColorThreshold = Shader.PropertyToID("_IntensityColorThreshold");
            _IntensityColorMinThreshold = Shader.PropertyToID("_IntensityColorMinThreshold");
            _IntensityAlphaThreshold = Shader.PropertyToID("_IntensityAlphaThreshold");
            _IntensityAlphaMinThreshold = Shader.PropertyToID("_IntensityAlphaMinThreshold");
        }
    }
}