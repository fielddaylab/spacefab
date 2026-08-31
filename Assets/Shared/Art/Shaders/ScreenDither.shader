Shader "SpaceFab/Screen Dither"
{
    Properties
    {
		[Header(Colors)] [Space]
        _Color ("Tint", Color) = (1,1,1,1)

        [Header(Dithering)] [Space]
        _DitherScale ("Dither Scale", Range(1, 32)) = 1

        [Header(Blending)] [Space] 
		[Enum(UnityEngine.Rendering.BlendMode)] _SrcBlend("Source Blend Mode", Int) = 1
        [Enum(UnityEngine.Rendering.BlendMode)] _DestBlend("Destination Blend Mode", Int) = 10
        [Enum(UnityEngine.Rendering.BlendOp)] _BlendOp("Blend Operation", Int) = 0
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

        Lighting Off
        ZWrite Off

        ZTest Always
        Blend [_SrcBlend] [_DestBlend]
        BlendOp [_BlendOp]

        Pass
        {
        CGPROGRAM
            #pragma vertex DefaultPostProcessVertex
            #pragma fragment DitherFrag
            #pragma target 3.0

            #include "Assets/FieldDay/_Assets/Shaders/CGIncludes/PostProcess.cginc"
            #include "Assets/FieldDay/_Assets/Shaders/CGIncludes/Dithering.cginc"

            half _DitherScale;

            fixed4 DitherFrag(Varyings_PP f, float_vpos screenPos: VPOS) : SV_Target
            {
                half4 color = _Color;
                color.a = invstep(GetBayerThreshold8(screenPos.xy / _DitherScale), color.a);
                color.rgb *= color.a;
                return color;
            }
        ENDCG
        }
    }
}
