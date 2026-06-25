Shader "SpaceFab/Screen Paper"
{
    Properties
    {
        [Header(Textures)] [Space]
        [NoScaleOffset] _MainTex ("Texture", 2D) = "white" {}
        [KeywordEnum(R,G,B,A)] FD_SAMPLE ("Sample Channel", Int) = 0

        [Header(Intensity Texture)] [Space]
		[KeywordEnum(Color, Alpha, Color_Alpha)] FD_INTENSITY("Intensity Mode", Int) = 2
		_IntensityColorThreshold("Intensity Color Threshold", Range(0.001, 1)) = 1
        _IntensityColorMinThreshold("Intensity Color Min Threshold", Range(0, 1)) = 0.001
		_IntensityAlphaThreshold("Intensity Alpha Threshold", Range(0.001, 1)) = 1
        _IntensityAlphaMinThreshold("Intensity Alpha Min Threshold", Range(0, 1)) = 0

		[Header(Colors)] [Space]
        _Color ("Tint", Color) = (1,1,1,1)

        [Header(Tiling)] [Space]
        _Tiling ("Pixels Per Tile", Range(8, 1024)) = 1
        _Pivot ("Pivot", Vector) = (0, 0, 0, 0)

        [Header(Blending)] [Space]
        [Enum(UnityEngine.Rendering.BlendMode)] _SrcBlend("Source Blend Mode", Int) = 1
        [Enum(UnityEngine.Rendering.BlendMode)] _DestBlend("Destination Blend Mode", Int) = 10
        [Enum(UnityEngine.Rendering.BlendOp)] _BlendOp("Blend Operation", Int) = 0
    }

    SubShader
    {
        Tags
        {
            "Queue"="Overlay"
            "IgnoreProjector"="True"
            "RenderType"="Overlay"
            "PreviewType"="Plane"
            "CanUseSpriteAtlas"="True"
        }

        Lighting Off
        ZWrite Off
        ZTest Always

        Blend [_SrcBlend] [_DestBlend]
        BlendOp [_BlendOp]

        Pass
        {
        CGPROGRAM
            #pragma vertex OverlayVert
            #pragma fragment OverlayFrag
            #pragma target 3.0

            #pragma shader_feature_local_fragment FD_SAMPLE_R FD_SAMPLE_G FD_SAMPLE_B FD_SAMPLE_A
            #pragma shader_feature_local_fragment FD_INTENSITY_COLOR FD_INTENSITY_ALPHA FD_INTENSITY_COLOR_ALPHA

            #include "Assets/FieldDay/_Assets/Shaders/CGIncludes/PostProcess.cginc"
            #include "Assets/FieldDay/_Assets/Shaders/CGIncludes/Intensity.cginc"

            sampler2D _MainTex;
            half _Tiling;
            half4 _Pivot;

            Varyings_PP OverlayVert(Attributes_PP v, out float4 vertex : SV_Position)
            {
                Varyings_PP output;
                StereoInitialize(output);
    
                vertex = ViewportSpaceToClipSpace(v.vertex);
                output.texcoord = ComputePixelTiledTexCoords(v.vertex, float2(_Tiling, _Tiling), _Pivot.xy);
                output.viewport = v.vertex;
    
                return output;
            }

            fixed4 OverlayFrag(Varyings_PP f) : SV_Target
            {
                fixed4 color = LayerIntensityTexture(_MainTex, f.texcoord, _Color);
                return color;
            }
        ENDCG
        }
    }
}
