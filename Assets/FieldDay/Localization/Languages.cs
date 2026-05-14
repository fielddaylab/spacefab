using System;

namespace FieldDay.Localization {
    static public class Languages {
        #region Constants

        static public readonly LanguageId English = new LanguageId("en");
        static public readonly LanguageId Spanish = new LanguageId("es");
        static public readonly LanguageId French = new LanguageId("fr");
        static public readonly LanguageId German = new LanguageId("de");
        static public readonly LanguageId Italian = new LanguageId("it");
        static public readonly LanguageId Dutch = new LanguageId("nl");
        static public readonly LanguageId Japanese = new LanguageId("ja");
        static public readonly LanguageId Arabic = new LanguageId("ar");

        #endregion // Constants

        #region Features

        /// <summary>
        /// Returns the features of the given language.
        /// Doesn't need to be particularly performant, as the result will be cached later in Loc.cs
        /// </summary>
        static public LanguageFeatures GetFeatures(LanguageId id) {
            if (id == Arabic) {
                return LanguageFeatures.IsRTL | LanguageFeatures.HasCustomGlyphShapingRules;
            }
            return default;
        }

        #endregion // Features
    }

    /// <summary>
    /// Language feature set.
    /// </summary>
    [Flags]
    public enum LanguageFeatures : ushort {
        IsRTL = 0x01,
        HasCustomGlyphShapingRules = 0x02
    }
}