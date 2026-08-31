Shader "FieldDay/Screen/Color Overlay"
{
    Properties
    {
		[Header(Colors)] [Space]
		_Color ("Tint", Color) = (1,1,1,1)
        
		[Header(Blending)] [Space]
        [Enum(UnityEngine.Rendering.BlendMode)] _SrcBlend("Source Blend Mode", Int) = 1
        [Enum(UnityEngine.Rendering.BlendMode)] _DestBlend("Destination Blend Mode", Int) = 10
        [Enum(UnityEngine.Rendering.BlendOp)] _BlendOp("Blend Operation", Int) = 0
		[Toggle(FD_PREMULTIPLY_ALPHA)] _PremultiplyAlpha("Premultiply Alpha", Float) = 1

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

        Cull Off
        Lighting Off
        ZWrite [_ZWriteMode]
		ZTest [_ZTestMode]
        Blend [_SrcBlend] [_DestBlend]
        BlendOp [_BlendOp]

        Pass
        {
        CGPROGRAM
            #pragma vertex DefaultPostProcessVertex
            #pragma fragment DefaultPostProcessFrag
            #pragma target 2.0
			#pragma shader_feature_local_fragment _ FD_PREMULTIPLY_ALPHA

            #include "../CGIncludes/PostProcess.cginc"
        ENDCG
        }
    }
}
