#ifndef FD_POSTPROCESS_INCLUDED
#define FD_POSTPROCESS_INCLUDED

#include "./Common.cginc"
#include "./Tiling.cginc"

/// Configuration Defines

/// Types

struct Attributes_PP
{
    half2 vertex   : POSITION;
};

struct Varyings_PP
{
    float2 texcoord         : TEXCOORD0;
    float2 viewport         : TEXCOORD1;
    VaryingsStereo()
};

/// Uniforms

// color
fixed4 _Color;

/// Helpers

/// Programs

Varyings_PP DefaultPostProcessVertex(Attributes_PP v, out float4 vertex : SV_Position)
{
    Varyings_PP output;
    StereoInitialize(output);
    
    vertex = ViewportSpaceToClipSpace(v.vertex);
    output.texcoord = v.vertex;
    output.viewport = v.vertex;
    
    return output;
}

fixed4 DefaultPostProcessFrag(Varyings_PP f) : SV_Target
{
    fixed4 color = _Color;
    PremultiplyAlpha(color);
    return color;
}

fixed4 DebugPostProcessFrag(Varyings_PP f) : SV_Target
{
    fixed4 color = fixed4(frac(f.texcoord.xy + 1), 0, _Color.a);
    PremultiplyAlpha(color);
    return color;
}

#endif // FD_POSTPROCESS_INCLUDED