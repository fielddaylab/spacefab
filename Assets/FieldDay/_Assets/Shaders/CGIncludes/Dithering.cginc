#ifndef FD_DITHERING_INCLUDED
#define FD_DITHERING_INCLUDED

const float BayerMatrix2x2[4] =
{
    0 / 4, 2 / 4,
    3 / 4, 1 / 4
};

const float BayerMatrix4x4[16] =
{
    00 / 16, 08 / 16, 02 / 16, 10 / 16,
    12 / 16, 04 / 16, 14 / 16, 06 / 16,
    03 / 16, 11 / 16, 01 / 16, 09 / 16,
    15 / 16, 07 / 16, 13 / 16, 05 / 16
};

const float BayerMatrix8x8[64] =
{
    00 / 64, 32 / 64, 08 / 64, 40 / 64, 02 / 64, 32 / 64, 10 / 64, 42 / 64,
    48 / 64, 16 / 64, 56 / 64, 24 / 64, 50 / 64, 18 / 64, 58 / 64, 26 / 64,
    12 / 64, 44 / 64, 04 / 64, 36 / 64, 14 / 64, 46 / 64, 06 / 64, 38 / 64,
    60 / 64, 28 / 64, 52 / 64, 20 / 64, 62 / 64, 30 / 64, 54 / 64, 22 / 64,
    03 / 64, 35 / 64, 11 / 64, 43 / 64, 01 / 64, 33 / 64, 09 / 64, 41 / 64,
    51 / 64, 19 / 64, 59 / 64, 27 / 64, 49 / 64, 17 / 64, 57 / 64, 25 / 64,
    15 / 64, 47 / 64, 07 / 64, 39 / 64, 13 / 64, 45 / 64, 05 / 64, 37 / 64,
    63 / 64, 31 / 64, 55 / 64, 23 / 64, 61 / 64, 29 / 64, 53 / 64, 21 / 64
};

#define GetSquareLookupIndex(pixelPos, dimension) ((uint(pixelPos.x) & (dimension - 1)) + dimension * (uint(pixelPos.y) & (dimension - 1)))

inline float GetBayerThreshold2x2(float2 pixelPos)
{
    return BayerMatrix2x2[GetSquareLookupIndex(pixelPos, 2)];
}

inline float GetBayerThreshold4x4(float2 pixelPos)
{
    return BayerMatrix4x4[GetSquareLookupIndex(pixelPos, 4)];
}

inline float GetBayerThreshold8x8(float2 pixelPos)
{
    return BayerMatrix8x8[GetSquareLookupIndex(pixelPos, 8)];
}

#endif // FD_DITHERING_INCLUDED