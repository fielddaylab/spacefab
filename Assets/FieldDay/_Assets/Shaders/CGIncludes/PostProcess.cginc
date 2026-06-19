#ifndef FD_POSTPROCESS_INCLUDED
#define FD_POSTPROCESS_INCLUDED

#include "./Common.cginc"

/// Configuration Defines

/// Types

struct Attributes_UI
{
    half2 vertex   : POSITION;
};

struct Varyings_UI
{
    float2 texcoord         : TEXCOORD0;
    VaryingsStereo()
};

/// Uniforms

// color
fixed4 _Color;

/// Helpers

/// Programs

Varyings_UI DefaultPostProcessVertex(Attributes_UI v, out float4 vertex : SV_Position)
{
    Varyings_UI output;
    StereoInitialize(output);
    
    vertex = float4(ViewportSpaceToClipSpace(v.vertex), 0, 1);
    output.texcoord = v.vertex;
    
    return output;
}

fixed4 DefaultPostProcessProgram(Varyings_UI f) : SV_Target
{
    return float4(f.texcoord.rg, 0, 1);
}

#endif // FD_POSTPROCESS_INCLUDED