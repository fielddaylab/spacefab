#ifndef FD_TILING_INCLUDED
#define FD_TILING_INCLUDED

/// Configuration Defines

/// Types

/// Helpers

inline float2 ComputePixelTiledTexCoords(float2 texCoord, float2 tileSize, float2 pivot)
{
    float2 tiles = (_ScreenParams.xy / tileSize);
    return (texCoord * tiles) - (pivot * frac(tiles));
}

inline float2 GetTileCenter(float2 texcoord)
{
    return floor(texcoord) + 0.5;
}

#endif // FD_TILING_INCLUDED