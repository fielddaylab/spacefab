Shader "FieldDay/Misc/Depth Decal"
{
    Properties
    {
		[Header(Culling and Clipping)] [Space]
		[Enum(UnityEngine.Rendering.CullMode)] _CullMode ("Cull Mode", Int) = 2

		[Header(Depth)] [Space]
		[Enum(UnityEngine.Rendering.CompareFunction)] _ZTestMode("ZTest Mode", Int) = 4
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
        ZWrite On
		ZTest [_ZTestMode]
		ColorMask 0

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
