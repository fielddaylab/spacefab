Shader "FieldDay/Misc/Depth Decal"
{
    Properties
    {
		[Header(Culling and Clipping)] [Space]
		[Enum(UnityEngine.Rendering.CullMode)] _CullMode ("Cull Mode", Int) = 2

		[Header(Depth)] [Space]
		[Enum(UnityEngine.Rendering.CompareFunction)] _ZTestMode("ZTest Mode", Int) = 4

        [Header(Z Offset)] [Space]
        _ZOffsetFactor("Z Offset Factor", Range(-1, 1)) = -1
        _ZOffsetUnits("Z Offset Units", Range(-1, 1)) = 1
    }

    SubShader
    {
        Tags
        {
            "Queue"="Background"
            "IgnoreProjector"="True"
            "RenderType"="Opaque"
        }

        Cull [_CullMode]
        Lighting Off
        Blend Off
        ZWrite On
		ZTest [_ZTestMode]
		ColorMask 0
        Offset [_ZOffsetFactor], [_ZOffsetUnits]

        Pass
        {
        CGPROGRAM
            #pragma vertex DefaultDepthPrePassVert
            #pragma fragment DefaultDepthPrePassFrag
            #pragma target 2.0

            #include "../CGIncludes/Depth.cginc"
        ENDCG
        }
    }
}
