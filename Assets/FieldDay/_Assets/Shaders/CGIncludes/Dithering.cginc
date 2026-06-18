#ifndef FD_DITHERING_INCLUDED
#define FD_DITHERING_INCLUDED

cbuffer FDBayerMatrices
{
    float4 fd_BayerMatrix8[16];
    float4 fd_BayerMatrix4[4];
    float4 fd_BayerMatrix2;
};

#define GetSquareLookupIndex(pixelPos, dimension) ((int(pixelPos.x) & (dimension - 1)) + ((int(pixelPos.y) & (dimension - 1)) * dimension))

inline float GetBayerThreshold2(float2 pixelPos)
{
    return fd_BayerMatrix2[GetSquareLookupIndex(pixelPos, 2)];
}

inline float GetBayerThreshold4(float2 pixelPos)
{
    int index = GetSquareLookupIndex(pixelPos, 4);
    return fd_BayerMatrix4[index >> 2][index & 3];
}

inline float GetBayerThreshold8(float2 pixelPos)
{
    int index = GetSquareLookupIndex(pixelPos, 8);
    return fd_BayerMatrix8[index >> 2][index & 3];
}

#endif // FD_DITHERING_INCLUDED