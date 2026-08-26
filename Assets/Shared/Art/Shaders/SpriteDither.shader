Shader "SpaceFab/Sprite Dither"
{
    Properties
    {
		[Header(Textures)] [Space]
        [PerRendererData] [NoScaleOffset] _MainTex ("Sprite Texture", 2D) = "white" {}
        
		[Header(Colors)] [Space]
		_Color ("Tint", Color) = (1,1,1,1)

		[Header(Dithering)] [Space]
        _DitherScale ("Dither Scale", Range(1, 32)) = 1
        
		[Header(Features)] [Space]
		[MaterialToggle] PixelSnap ("Pixel snap", Float) = 0
        
		[HideInInspector] _RendererColor ("RendererColor", Color) = (1,1,1,1)
        [HideInInspector] _Flip ("Flip", Vector) = (1,1,1,1)
		[HideInInspector] _LerpColor ("Lerp Color", Color) = (1, 1, 1, 0)
		[HideInInspector] _AdditiveColor ("Additive Color", Color) = (1, 1, 1, 0)

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
            #pragma vertex DefaultSpriteVert
            #pragma fragment DitherFrag
            #pragma target 2.0
            #pragma multi_compile_instancing
            #pragma multi_compile_fog
            #pragma shader_feature_local_vertex _ PIXELSNAP_ON
			#pragma shader_feature_local_fragment _ FD_SPRITE_ALPHACLIP
			#pragma shader_feature_local_fragment _ FD_PREMULTIPLY_ALPHA
            #pragma shader_feature_local _ FD_ENABLE_FOG

            #include "Assets/FieldDay/_Assets/Shaders/CGIncludes/Sprites.cginc"
			#include "Assets/FieldDay/_Assets/Shaders/CGIncludes/Dithering.cginc"

			half _DitherScale;

			fixed4 DitherFrag(Varyings_Sprite v, float_vpos screenPos: VPOS) : SV_Target
			{
				InstancingInitialize(v);
				fixed4 color = SampleSpriteTexture(v.texcoord) * v.color;
				color.a = invstep(GetBayerThreshold8(screenPos.xy / _DitherScale), color.a);
				SpriteAlphaClip(color);
				PremultiplyAlpha(color);
				return color;
			}
        ENDCG
        }
    }
}
