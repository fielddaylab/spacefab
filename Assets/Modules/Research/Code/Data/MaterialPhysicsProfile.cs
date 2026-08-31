using BeauUtil;
using FieldDay.Assets;
using SpaceFab.Materials;
using UnityEngine;

namespace SpaceFab.Research
{
    /// <summary>
    /// Physics data for a MaterialAsset, paired by id. Holds the
    /// conduction / thermal / voltage / temperature stability values the
    /// chamber systems read to drive their visuals and stability checks.
    /// Kept as a parallel asset so MaterialAsset stays free of Research-
    /// specific physics fields.
    /// </summary>
    [CreateAssetMenu(menuName = "SpaceFab/Research/Material Physics Profile")]
    public class MaterialPhysicsProfile : NamedAsset
    {
        // The MaterialAsset this profile describes. [AssetName] renders the
        // inspector field as a dropdown of authored MaterialAsset assets.
        [AssetName(typeof(MaterialAsset), true)]
        public SerializedHash32 MaterialId;

        // Conductivity with no heat applied. 0 = perfect insulator,
        // 1 = baseline conductor, up to 2 for highly conductive materials.
        [Range(0f, 2f)] public float BaseConduction = 1f;

        // Conductivity at maximum temperature. Greater than BaseConduction for
        // semiconductor-like materials that only conduct once heated; less for
        // metals that lose conductivity with heat; equal for heat-insensitive
        // materials. Conduction interpolates between the two on an eased ramp.
        [Range(0f, 2f)] public float HeatedConduction = 1f;

        // Maximum temperature before the material is unstable. Used by the
        // future Thermal chamber.
        [Range(0f, 1f)] public float MaxTemperature = 1f;

        // Maximum |voltage| before the material is unstable. Battery's
        // stability check clears the slot when |voltage| exceeds this.
        [Range(0f, 1f)] public float MaxVoltage = 1f;

        // Boosts current by 1.5x. Surfaces as a HighMobility observation.
        public bool IsHighMobility;

        // Surfaces as a LightEmitting observation. Currently informational;
        // the Junction chamber will read this.
        public bool IsLightEmitting;
    }

    /// <summary>
    /// Pure functions over MaterialPhysicsProfile. Take the profile (and any
    /// per-call parameters) so callers don't need a Find.NamedAsset lookup
    /// inside the utility.
    /// </summary>
    public static class MaterialPhysicsUtility
    {
        // Returns the current that flows through the material at the given
        // voltage and temperature. Linear in voltage; conductivity interpolates
        // between the material's cold and hot values on an eased temperature
        // ramp, so a near-zero BaseConduction can still reach a discernible
        // current when heated.
        public static float GetCurrent(MaterialPhysicsProfile profile, float voltage, float temperature)
        {
            if (profile == null) return 0f;

            // Squared ramp: conduction hugs its cold value through the first
            // heat step, then climbs sharply. Reads to the player as a
            // threshold effect rather than a gradual fade.
            float t = Mathf.Clamp01(temperature);
            t *= t;

            float conduction = Mathf.Lerp(profile.BaseConduction, profile.HeatedConduction, t);
            if (profile.IsHighMobility)
            {
                conduction *= 1.5f;
            }

            // Clamped so downstream consumers can treat current as a normalized
            // magnitude; the high-mobility boost and a 2.0 HeatedConduction can
            // otherwise push the raw product well past 1.
            return Mathf.Clamp(voltage * conduction, -1f, 1f);
        }

        // True if the material is stable at the given voltage. Battery
        // clears the slot when this returns false.
        public static bool IsStableAtVoltage(MaterialPhysicsProfile profile, float voltage)
        {
            if (profile == null) return true;
            return Mathf.Abs(voltage) <= profile.MaxVoltage;
        }

        // True if the material is stable at the given temperature.
        public static bool IsStableAtTemperature(MaterialPhysicsProfile profile, float temperature)
        {
            if (profile == null) return true;
            return temperature <= profile.MaxTemperature;
        }
    }
}
