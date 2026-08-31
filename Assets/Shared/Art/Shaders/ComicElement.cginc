#ifndef COMICS_INCLUDED
#define COMICS_INCLUDED

#include "Assets/FieldDay/_Assets/Shaders/CGIncludes/Common.cginc"

/// Configuration Defines

/// Types

struct Attributes_Comic
{
    float2 vertex   : POSITION;
    float4 texcoord : TEXCOORD0;
};

struct Varyings_Comic
{
    fixed4 color    : COLOR;
    float4 texcoord : TEXCOORD0;
};

/// Instancing

/// Uniforms

fixed4 _Color;

sampler2D _MainTex;

/// Helpers

/// Programs

Varyings_Comic DefaultComicVert(Attributes_Comic v, out float4 vertex : SV_Position)
{
    Varyings_Comic output;

    vertex = UnityObjectToClipPos(float4(v.vertex, 0, 0));
    
    output.texcoord = v.texcoord;
    output.color = _Color;

    return output;
}

fixed4 DefaultComicFrag(Varyings_Comic v) : SV_Target
{
    fixed4 color = tex2D(_MainTex, v.texcoord) * v.color;
    PremultiplyAlpha(color);
    return color;
}

#endif // COMICS_INCLUDED