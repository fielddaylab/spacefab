Shader "SpaceFab/Screen Halftone Transition"
{
    Properties
    {
		[Header(Colors)] [Space]
        _Color ("Tint", Color) = (1,1,1,1)

        [Header(Dithering)] [Space]
        _Tiling ("Pixels Per Tile", Range(8, 512)) = 1

        [Header(Blending)] [Space]
        [Enum(UnityEngine.Rendering.BlendMode)] _SrcBlend("Source Blend Mode", Int) = 1
        [Enum(UnityEngine.Rendering.BlendMode)] _DestBlend("Destination Blend Mode", Int) = 10
        [Enum(UnityEngine.Rendering.BlendOp)] _BlendOp("Blend Operation", Int) = 0
		[Toggle(INVERT_TRANSITION)] _InvertTransition("Invert Transition", Float) = 0

        [Header(Planes (Tile Space))] [Space]
        _Plane0 ("Plane A", Vector) = (0, 0, 1, 1)
        _Plane1 ("Plane B", Vector) = (0, 0, 1, 1)
        _FadeBand ("Fade Band (Tiles)", Float) = 0.5
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
            #pragma vertex OverlayVert
            #pragma fragment OverlayFrag
            #pragma target 2.0
			#pragma multi_compile_fragment _ INVERT_TRANSITION

            #include "Assets/FieldDay/_Assets/Shaders/CGIncludes/PostProcess.cginc"
            #include "Assets/FieldDay/_Assets/Shaders/CGIncludes/Dithering.cginc"
            #include "Assets/FieldDay/_Assets/Shaders/CGIncludes/SDF.cginc"

            half _Tiling;

            half4 _Plane0;
            half4 _Plane1;
            half _FadeBand;

            Varyings_PP OverlayVert(Attributes_PP v, out float4 vertex : SV_Position)
            {
                Varyings_PP output;
                StereoInitialize(output);
    
                vertex = ViewportSpaceToClipSpace(v.vertex);
                output.texcoord = ComputePixelTiledTexCoords(v.vertex, float2(_Tiling, _Tiling), float2(0.5, 0.5));
                output.viewport = v.vertex;
    
                return output;
            }

            float ComputeAlphaForCell(float2 center)
            {
                float distanceFromPlaneA = ComputeDistanceToPackedPlane(center, _Plane0);
                float distanceFromPlaneB = ComputeDistanceToPackedPlane(center, _Plane1);

                return SdfBlendFactor(min(distanceFromPlaneA, distanceFromPlaneB), _FadeBand);
            }

            fixed4 OverlayFrag(Varyings_PP f) : SV_Target
            {
                fixed4 color = _Color;
				float2 cellOffset = float2(0.5 * (uint(f.texcoord.y) % 2), 0);

                float2 center = ComputeHalftoneCellPosition(f.texcoord + cellOffset) - cellOffset;

                float centerAlpha = ComputeAlphaForCell(center);
				#if INVERT_TRANSITION
                centerAlpha = lerp(1, 1 - centerAlpha, color.a);
				#else
				centerAlpha *= color.a;
				#endif // INVERT_TRANSITION

                float dist = distance(center, f.texcoord);
                float fillDistance = centerAlpha / SQRT_2;
                #if INVERT_TRANSITION
				color.a = step(fillDistance, dist);
				#else
				color.a = step(dist, fillDistance);
				#endif // INVERT_TRANSITION
                color.rgb *= color.a;
                return color;
            }
        ENDCG
        }
    }
}
