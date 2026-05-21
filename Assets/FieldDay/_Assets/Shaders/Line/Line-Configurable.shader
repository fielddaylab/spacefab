Shader "FieldDay/Lines/Configurable"
{
    Properties
    {
		[Header(Textures)] [Space]
        _MainTex ("Sprite Texture", 2D) = "white" {}
        
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
            #pragma fragment DefaultLineFrag
            #pragma target 2.0
            #pragma multi_compile_instancing
            #pragma multi_compile_fog
            #pragma shader_feature_local_fragment _ FD_SPRITE_ALPHACLIP
			#pragma shader_feature_local_fragment _ FD_PREMULTIPLY_ALPHA
            #pragma shader_feature_local _ FD_ENABLE_FOG

            #include "../CGIncludes/Lines.cginc"
        ENDCG
        }
    }
}
