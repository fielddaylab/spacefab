#ifndef FD_SDF_INCLUDED
#define FD_SDF_INCLUDED

/// Configuration Defines

/// Types

/// Helpers

#define SdfBlendFactorAA(distance)  saturate(1.0 - (distance))
#define SdfBlendFactor(distance, band)  (1 - saturate((distance) / (band)))

#endif // FD_SDF_INCLUDED