Shader "FieldDay/Lines/Intensity Texture"
{
    Properties
    {
		[Header(Textures)] [Space]
        _MainTex ("Intensity Texture", 2D) = "white" {}
        [KeywordEnum(R,G,B,A)] FD_SAMPLE ("Sample Channel", Int) = 0

		[Header(Intensity Texture)] [Space]
		[KeywordEnum(Color, Alpha, Color_Alpha)] FD_INTENSITY("Intensity Mode", Int) = 2
		_IntensityColorThreshold("Intensity Color Threshold", Range(0.001, 1)) = 1
        _IntensityColorMinThreshold("Intensity Color Min Threshold", Range(0, 1)) = 0.001
		_IntensityAlphaThreshold("Intensity Alpha Threshold", Range(0.001, 1)) = 1

		[Header(Colors)] [Space]
        _Color ("Tint", Color) = (1,1,1,1)

		[Header(Blending)] [Space]
        [Enum(UnityEngine.Rendering.BlendMode)] _SrcBlend("Source Blend Mode", Int) = 1
        [Enum(UnityEngine.Rendering.BlendMode)] _DestBlend("Destination Blend Mode", Int) = 10
        [Enum(UnityEngine.Rendering.BlendOp)] _BlendOp("Blend Operation", Int) = 0
		[Toggle(FD_PREMULTIPLY_ALPHA)] _PremultiplyAlpha("Premultiply Alpha", Float) = 1

		[Header(Culling and Clipping)] [Space]
		[Enum(UnityEngine.Rendering.CullMode)] _CullMode ("Cull Mode", Int) = 2
        [Toggle(FD_SPRITE_ALPHACLIP)] _EnableAlphaClip("Use Alpha Clip", Int) = 0
		_AlphaCutoff("Alpha Cutoff", Range(0, 1)) = 0

		[Header(Depth)] [Space]
		[Enum(Off,0,On,1)] _ZWriteMode("ZWrite", Int) = 0
		[Enum(UnityEngine.Rendering.CompareFunction)] _ZTestMode("ZTest Mode", Int) = 4

        [Header(Effects)] [Space]
        [Toggle(FD_ENABLE_FOG)] _EnableFog("Enable Fog", Int) = 0
    }

    SubShader
    {
        Tags
        {
            "Queue"="Transparent"
            "IgnoreProjector"="True"
            "RenderType"="Transparent"
            "PreviewType"="Plane"
            "CanUseSpriteAtlas"="True"
        }

        Cull [_CullMode]
        Lighting Off
        ZWrite [_ZWriteMode]
		ZTest [_ZTestMode]
        Blend [_SrcBlend] [_DestBlend]
        BlendOp [_BlendOp]

        Pass
        {
        CGPROGRAM
            #pragma vertex DefaultLineVert
            #pragma fragment LineFragIntensity
            #pragma target 2.0
            #pragma multi_compile_instancing
            #pragma multi_compile_fog
            #pragma shader_feature_local_fragment _ FD_SPRITE_ALPHACLIP
			#pragma shader_feature_local_fragment _ FD_PREMULTIPLY_ALPHA
            #pragma shader_feature_local_fragment FD_SAMPLE_R FD_SAMPLE_G FD_SAMPLE_B FD_SAMPLE_A
            #pragma shader_feature_local _ FD_ENABLE_FOG
			#pragma shader_feature_local_fragment FD_INTENSITY_COLOR FD_INTENSITY_ALPHA FD_INTENSITY_COLOR_ALPHA

            #include "../CGIncludes/Lines.cginc"
			#include "../CGIncludes/Intensity.cginc"

            fixed4 LineFragIntensity(Varyings_Line v) : SV_Target
            {
				InstancingInitialize(v);

				half4 color = LayerIntensityTexture(_MainTex, v.texcoord, v.color);
                SpriteAlphaClip(color);

                FogApply(color, v);

				PremultiplyAlpha(color);
                return color;
            }
        ENDCG
        }
    }
}
