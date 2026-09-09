// Portions from Unity built-in shader source, under MIT license.

#ifndef FD_LINES_INCLUDED
#define FD_LINES_INCLUDED

#include "./Common.cginc"
#include "./Fog.cginc"
#include "./Dithering.cginc"

/// Configuration Defines

// UNITY_INSTANCING_ENABLED     (Unity) Enables instanced rendering
// FD_SPRITE_ALPHACLIP          Enables alpha clipping using the _AlphaCutoff uniform

/// Types

struct Attributes_Line
{
    float4 vertex   : POSITION;
    fixed4 color    : COLOR;
    float2 texcoord : TEXCOORD0;
    AttributesInstancing()
};

struct Varyings_Line
{
    fixed4 color    : COLOR;
    float2 texcoord : TEXCOORD0;
    VaryingsFog(1)
    VaryingsStereo()
    VaryingsInstancing()
};

/// Instancing

/// Uniforms

fixed4 _Color;

#if FD_SPRITE_ALPHACLIP
half _AlphaCutoff;
#endif // FD_SPRITE_ALPHACLIP

sampler2D _MainTex;
float4 _MainTex_ST;

#if ETC1_EXTERNAL_ALPHA
sampler2D _AlphaTex;
#endif // ETC1_EXTERNAL_ALPHA

/// Helpers

inline fixed4 SampleMainNoExternalAlpha(float2 uv)
{
    return tex2D(_MainTex, uv);
}

inline fixed4 SampleMainWithExternalAlpha(float2 uv)
{
    fixed4 color = tex2D(_MainTex, uv);

#if ETC1_EXTERNAL_ALPHA
    fixed4 alpha = tex2D(_AlphaTex, uv);
    color.a = lerp(color.a, alpha.r, _EnableExternalAlpha);
#endif

    return color;
}

#if ETC1_EXTERNAL_ALPHA
    #define SampleSpriteTexture    SampleMainWithExternalAlpha
#else
    #define SampleSpriteTexture    SampleMainNoExternalAlpha
#endif // ETC1_EXTERNAL_ALPHA

#ifdef FD_SPRITE_ALPHACLIP
    #define SpriteAlphaClip(color) clip((color).a - _AlphaCutoff)
#else
    #define SpriteAlphaClip(color)
#endif // UNITY_UI_ALPHACLIP

/// Programs
    
#define LineFragCommonFooter(varyings, color, fragPos) \
    DitheredAlphaApply(color, fragPos); \
    SpriteAlphaClip(color); \
    FogApply(color, varyings); \
    PremultiplyAlpha(color);

Varyings_Line DefaultLineVert(Attributes_Line v, out float4 vertex : SV_Position)
{
    Varyings_Line output;
    InstancingInitialize(v);
    InstancingTransfer(v, output);
    StereoInitialize(output);

    vertex = UnityObjectToClipPos(v.vertex);
    output.texcoord = TRANSFORM_TEX(v.texcoord, _MainTex);
    output.color = v.color * _Color;
    
    FogTransfer(output, vertex);
        
    return output;
}

fixed4 DefaultLineFrag(Varyings_Line v, float_vpos fragPos : VPOS) : SV_Target
{
    InstancingInitialize(v);
    fixed4 color = SampleSpriteTexture(v.texcoord) * v.color;
    LineFragCommonFooter(v, color, fragPos);
    return color;
}

#endif // FD_SPRITES_INCLUDED