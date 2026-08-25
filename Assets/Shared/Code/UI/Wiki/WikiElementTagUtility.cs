using BeauUtil;
using FieldDay;
using SpaceFab.Materials;
using SpaceFab.Onboarding;
using SpaceFab.Research;
using System.Text;
using UnityEngine;

namespace SpaceFab.UI {
    /// <summary>
    /// Builds the onboarding ElementTag ids carried by the wiki's chips, and stamps / clears
    /// them as pages bind and unbind. Leaf tutorial scripts address a chip by calling
    /// HighlightElement with one of these ids.
    ///
    /// Chips come out of one shared pool (WikiChipPools.ChipPool) and are reused across every
    /// page, so an id can't be authored on the prefab — the page-load utilities stamp the chips
    /// they allocate and clear them again on free. A chip is therefore addressable only while
    /// the page that owns it is the one on screen, which is also the only time a highlight on
    /// it would land somewhere visible.
    ///
    /// Ids follow the project's "module:kebab-case-name" convention, under the same wiki:
    /// prefix the panel's authored tags already use (wiki:open-btn, wiki:close-btn):
    ///   wiki:material-copper-conductor           material page, one per characteristic chip
    ///   wiki:material-boron-pdopantfor-silicon   ... dynamic characteristic; context appended
    ///   wiki:property-conductor                  property page, the property chip itself
    ///   wiki:property-conductor-conductive       ... one per decomposed-observation chip
    ///
    /// Name segments come from the MaterialPropertyLabel enum and the MaterialAsset's asset
    /// name rather than from display strings, so a copy rewrite doesn't silently break a
    /// tutorial script. Everything is lowercased, matching what OnboardingScripting does to a
    /// Leaf-supplied id before hashing it.
    /// </summary>
    public static class WikiElementTagUtility {
        // Shared prefix for every wiki-owned tag id.
        private const string Prefix = "wiki:";

        // Scratch for Slug. Tag ids are built during page binds and strip rebuilds — rare and
        // single-threaded — so one shared builder saves the per-call allocation.
        private static readonly StringBuilder s_SlugBuilder = new StringBuilder(64);

        /// <summary>
        /// Id for a static characteristic chip on a material page. Null when the material can't
        /// be resolved to a name, which Stamp treats as "leave this chip unaddressable" rather
        /// than registering an id no script could have predicted.
        /// </summary>
        public static string MaterialCharacteristicId(StringHash32 materialId, MaterialPropertyLabel label) {
            string materialName = ResolveMaterialName(materialId);
            if (materialName == null) { return null; }

            return Prefix + "material-" + materialName + "-" + LabelSlug(label);
        }

        /// <summary>
        /// Id for a dynamic characteristic chip ("P-Type Dopant for X"), keyed by its context
        /// material as well as its label — a material can carry several of these, and without
        /// the context they'd all want the same id. Null if either material fails to resolve.
        /// </summary>
        public static string MaterialCharacteristicId(StringHash32 materialId, MaterialPropertyLabel label, StringHash32 contextMaterialId) {
            string baseId = MaterialCharacteristicId(materialId, label);
            if (baseId == null) { return null; }

            string contextName = ResolveMaterialName(contextMaterialId);
            if (contextName == null) { return null; }

            return baseId + "-" + contextName;
        }

        /// <summary>
        /// Id for the property chip heading a property page.
        /// </summary>
        public static string PropertyChipId(MaterialPropertyLabel property) {
            return Prefix + "property-" + LabelSlug(property);
        }

        /// <summary>
        /// Id for one decomposed-observation chip below a property page's property chip. Keyed
        /// by the owning property as well as the observation, so the same observation appearing
        /// under two properties stays separately addressable.
        /// </summary>
        public static string PropertyObservationId(MaterialPropertyLabel property, MaterialPropertyLabel observation) {
            return Prefix + "property-" + LabelSlug(property) + "-" + LabelSlug(observation);
        }

        /// <summary>
        /// Id for one decomposed-observation chip below an observation page. Keyed
        /// by the owning observation type as well as the observation, so the same observation appearing
        /// under two observations stays separately addressable.
        /// </summary>
        public static string ObservationTypeObservationId(ObservationType type, MaterialPropertyLabel observation)
        {
            return Prefix + "observations-" + LabelSlug(type) + "-" + LabelSlug(observation);
        }

        /// <summary>
        /// Id for one page thumbnail in the paginator strip, keyed by the page asset's name —
        /// the same string content scripts already pass to LockWikiPage / UnlockWikiPage, so a
        /// tutorial names a thumbnail the way it names the page ("Voltage Chamber" becomes
        /// research:page-voltage-chamber).
        /// </summary>
        public static string PageThumbId(string pageName) {
            string slug = Slug(pageName);
            if (slug.Length == 0) { return null; }
            return "wiki:page-" + slug;
        }

        /// <summary>
        /// Kebab-cases an authored name for use inside a tag id: "Voltage Chamber" becomes
        /// "voltage-chamber", "P-Type Dopant" becomes "p-type-dopant". Every run of characters
        /// that isn't a letter or digit collapses to a single dash — so spaces, underscores,
        /// punctuation and existing dashes all land on the same separator — and runs at either
        /// end are dropped. Returns an empty string for a name with nothing sluggable in it.
        /// </summary>
        public static string Slug(string name) {
            if (string.IsNullOrEmpty(name)) { return string.Empty; }

            s_SlugBuilder.Clear();
            bool pendingSeparator = false;

            for (int i = 0; i < name.Length; i++) {
                char c = name[i];
                if (!char.IsLetterOrDigit(c)) {
                    pendingSeparator = true;
                    continue;
                }

                // A separator is only emitted once a keepable character follows it, which
                // collapses runs and drops leading and trailing ones without a second pass.
                if (pendingSeparator && s_SlugBuilder.Length > 0) {
                    s_SlugBuilder.Append('-');
                }
                pendingSeparator = false;
                s_SlugBuilder.Append(char.ToLowerInvariant(c));
            }

            return s_SlugBuilder.ToString();
        }

        /// <summary>
        /// Points the chip's ElementTag at `id`, registering it with ElementTagLookup so Leaf can
        /// resolve it. A null or empty id clears the tag instead, so a caller that couldn't build
        /// one doesn't have to branch.
        /// </summary>
        public static void Stamp(ResearchObservationChip chip, string id) {
            if (chip == null) { return; }
            if (string.IsNullOrEmpty(id)) {
                Clear(chip);
                return;
            }

            ElementTag tag = EnsureTag(chip);
            tag.SetId(id);
        }

        /// <summary>
        /// Drops the chip out of ElementTagLookup, so a pooled chip parked off-screen never
        /// resolves a highlight. No-op for a chip that was never stamped.
        /// </summary>
        public static void Clear(ResearchObservationChip chip) {
            if (chip == null || chip.Tag == null) { return; }
            chip.Tag.SetId(default(StringHash32));
        }

        // Resolves the chip's ElementTag, adding one if it isn't authored on the prefab. The chip
        // prefab is shared with the Research picker and sample panel, which have no use for a
        // tag, so the component is created by the first page that wants to address the chip and
        // then cached on chip.Tag for the rest of that pooled instance's life.
        //
        // RectTransform is assigned here rather than left to ElementTag.Awake: a chip can be
        // stamped while its page group is still hidden (RefreshPageContent fills chips before it
        // shows the group), and Awake doesn't run on an inactive GameObject — the highlight would
        // then find no target to size itself against.
        private static ElementTag EnsureTag(ResearchObservationChip chip) {
            if (chip.Tag == null) {
                chip.Tag = chip.GetComponent<ElementTag>();
                if (chip.Tag == null) {
                    chip.Tag = chip.gameObject.AddComponent<ElementTag>();
                }
            }
            if (chip.Tag.RectTransform == null) {
                chip.Tag.RectTransform = chip.transform as RectTransform;
            }
            return chip.Tag;
        }

        // The material's asset name, slugged, or null when nothing is registered under the id.
        // Current material assets are single words ("Copper", "SiliconDioxide"), so this matches
        // the "research:sample-<name>" ids the Research tray builds; a name with a space in it
        // would kebab-case here rather than run together.
        private static string ResolveMaterialName(StringHash32 materialId) {
            if (materialId.IsEmpty) { return null; }

            MaterialAsset material = Find.NamedAsset<MaterialAsset>(materialId);
            if (material == null || string.IsNullOrEmpty(material.name)) { return null; }

            string slug = Slug(material.name);
            return slug.Length > 0 ? slug : null;
        }

        // Enum name slugged — "conductor", "hitempsemiconductor", "conductive". The enum is the
        // stable handle a script author can grep for; display strings are copy, and change.
        private static string LabelSlug(MaterialPropertyLabel label) {
            return Slug(label.ToString());
        }

        // Enum name slugged — "electrical", "thermal", "dopant". The enum is the
        // stable handle a script author can grep for; display strings are copy, and change.
        private static string LabelSlug(ObservationType type)
        {
            return Slug(type.ToString());
        }
    }
}
