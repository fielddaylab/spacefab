#ifndef FD_GEOMETRY_INCLUDED
#define FD_GEOMETRY_INCLUDED

#include "./Common.cginc"

/// Configuration Defines

/// Types

/// Uniforms

#define SQRT_2 (1.414213562373095)

/// Helpers

inline float ComputeDistanceToPackedPlane(float2 position, float4 plane)
{
    return dot(position - plane.xy, normalize(plane.zw));
}

/// Programs

#endif // FD_GEOMETRY_INCLUDED