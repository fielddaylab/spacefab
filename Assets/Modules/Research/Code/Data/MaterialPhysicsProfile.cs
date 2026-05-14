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

        // Base conductivity. 0 = perfect insulator, 1 = baseline conductor,
        // up to 2 for highly conductive materials. Multiplied into current.
        [Range(0f, 2f)] public float ConductionMultiplier = 1f;

        // Temperature sensitivity. Current scales as
        // (1 + temperature * (ThermalMultiplier - 1)). Values > 1 boost
        // current with heat (semiconductor-like); values < 1 reduce it.
        [Range(0f, 2f)] public float ThermalMultiplier = 1f;

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
        // voltage and temperature. Linear in voltage, scaled by a temperature-
        // dependent factor and the material's conductivity.
        public static float GetCurrent(MaterialPhysicsProfile profile, float voltage, float temperature)
        {
            if (profile == null) return 0f;
            float conduction = profile.ConductionMultiplier;
            if (profile.IsHighMobility)
            {
                conduction *= 1.5f;
            }
            float thermal = 1f + temperature * (profile.ThermalMultiplier - 1f);
            return voltage * thermal * conduction;
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
