#ifndef FD_DEPTH_INCLUDED
#define FD_DEPTH_INCLUDED

#include "./Common.cginc"

struct Attributes_DepthOnly
{
    float4 vertex : POSITION;
};

struct Varyings_DepthOnly
{
    VaryingsStereo()
};

Varyings_DepthOnly DefaultDepthPrePassVert(Attributes_DepthOnly v, out float4 vertex : SV_Position)
{
    Varyings_DepthOnly output;
    StereoInitialize(output);
    vertex = UnityObjectToClipPos(v.vertex);
    return output;
}

fixed4 DefaultDepthPrePassFrag(Varyings_DepthOnly v) : SV_Target
{
    return fixed4(0, 0, 0, 0);
}

#endif // FD_DEPTH_INCLUDED