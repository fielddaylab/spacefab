using SpaceFab.Materials;

namespace SpaceFab.Research {
    /// <summary>
    /// Maps MaterialPropertyLabel enum values to human-readable strings for
    /// the hypothesis / observation UI. Two flavors: short observation
    /// phrasing (sentence-style, chip-sized) and uppercase property phrasing
    /// (used in the hypothesis header). Authoring is intentionally a switch
    /// for now — a NamedAsset / localization table can replace this once
    /// the label vocabulary stabilizes.
    /// </summary>
    public static class MaterialPropertyLabelDisplay {
        // Chip label for an observation. Falls back to the enum name when
        // a label hasn't been given a friendly string yet.
        public static string GetObservationName(MaterialPropertyLabel label) {
            switch (label) {
                case MaterialPropertyLabel.Conductive: return "Conducts electricity";
                case MaterialPropertyLabel.NonConductive: return "Prevents current flow";
                case MaterialPropertyLabel.HeatActivated: return "Heat increases current";
                case MaterialPropertyLabel.HeatDeactivated: return "Heat decreases current";
                case MaterialPropertyLabel.HeatUnaffected: return "Heat does not affect current";
                case MaterialPropertyLabel.HeatVulnerable: return "Heat causes breakdown";
                case MaterialPropertyLabel.HeatResistant: return "Resists high temperatures";
                case MaterialPropertyLabel.AtomicRadiusCompliant: return "Smaller atomic radius";
                case MaterialPropertyLabel.LightEmitting: return "Emits light when active";
                case MaterialPropertyLabel.HighMobility: return "High electron mobility";
                case MaterialPropertyLabel.VoltageResistant: return "Withstands extreme voltage";
                case MaterialPropertyLabel.ValenceOneLessThan: return "1 less valence electron";
                case MaterialPropertyLabel.ValenceOneMoreThan: return "1 more valence electron";
                default: return label.ToString();
            }
        }

        // Header phrasing for a persistent property. Used in the hypothesis
        // panel's "FIND A ..." title.
        public static string GetPropertyName(MaterialPropertyLabel label) {
            switch (label) {
                case MaterialPropertyLabel.ConductorNaive: return "CONDUCTOR";
                case MaterialPropertyLabel.InsulatorNaive: return "INSULATOR";
                case MaterialPropertyLabel.Insulator: return "INSULATOR";
                case MaterialPropertyLabel.Conductor: return "CONDUCTOR";
                case MaterialPropertyLabel.Semiconductor: return "SEMICONDUCTOR";
                case MaterialPropertyLabel.HiTempConductor: return "HIGH-TEMP CONDUCTOR";
                case MaterialPropertyLabel.HiTempSemiConductor: return "HIGH-TEMP SEMICONDUCTOR";
                case MaterialPropertyLabel.PDopantFor: return "P-TYPE DOPANT";
                case MaterialPropertyLabel.NDopantFor: return "N-TYPE DOPANT";
                case MaterialPropertyLabel.LightEmittingSemiconductor: return "LIGHT-EMITTING SEMICONDUCTOR";
                case MaterialPropertyLabel.HighVoltageSemiconductor: return "HIGH VOLTAGE SEMICONDUCTOR";
                case MaterialPropertyLabel.HighMobilitySemiconductor: return "HIGH MOBILITY SEMICONDUCTOR";
                default: return label.ToString().ToUpperInvariant();
            }
        }
    }
}
