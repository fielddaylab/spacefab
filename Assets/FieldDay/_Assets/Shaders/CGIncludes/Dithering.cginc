#ifndef FD_DITHERING_INCLUDED
#define FD_DITHERING_INCLUDED

#include "./Common.cginc"

/// Keywords

/// Configuration Defines

// FD_DITHER_TWO            Dithering will use the 2x2 matrix
// FD_DITHER_FOUR           Dithering will use the 4x4 matrix
// FD_DITHER_EIGHT          Dithering will use the 8x8 matrix

/// bayer

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

#if FD_DITHER_TWO
	#define GetBayerThreshold(pixelPos) GetBayerThreshold2(pixelPos)
#elif FD_DITHER_FOUR
	#define GetBayerThreshold(pixelPos) GetBayerThreshold4(pixelPos)
#elif FD_DITHER_EIGHT
	#define GetBayerThreshold(pixelPos) GetBayerThreshold8(pixelPos)
#else
	#define GetBayerThreshold(pixelPos) (0)
#endif //

/// halftone

inline float2 ComputeHalftoneCellPosition(float2 normalizedCellCoords)
{
    return floor(normalizedCellCoords) + 0.5;
}

/// UNIFORMS

half _DitherAlphaScale;

/// HELPERS

#if FD_DITHER_TWO || FD_DITHER_FOUR || FD_DITHER_EIGHT
    #define DitheredAlphaApply(color, pixelPos)   (color).a = invstep(GetBayerThreshold((pixelPos).xy / _DitherAlphaScale), (color).a)
#else
    #define DitheredAlphaApply(color, pixelPos)
#endif // FD_DITHERING

#endif // FD_DITHERING_INCLUDED