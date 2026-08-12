using System.Collections.Generic;
using BeauUtil;
using BeauUtil.Debugger;
using FieldDay;
using FieldDay.Scenes;
using FieldDay.SharedState;

namespace SpaceFab.Onboarding {
    /// <summary>
    /// Singleton id -> ElementTag index, populated incrementally as ElementTag
    /// instances register and deregister. Onboarding scripting resolves Leaf-supplied
    /// ids through this lookup before asking the highlight system to act on them.
    /// Ids are unique by design — duplicate registration logs a warning and the second
    /// tag is skipped (first registered wins). Collisions are treated as an authoring
    /// error rather than a fatal failure so dynamically-tagged pooled objects (e.g.
    /// the design-mode input overlays) survive a level with bad data.
    /// </summary>
    public class ElementTagLookup : ISharedState, IRegistrationCallbacks {
        [System.NonSerialized] public Dictionary<StringHash32, ElementTag> ById;

        public void OnRegister() {
            ById = new Dictionary<StringHash32, ElementTag>(32);
        }

        public void OnDeregister() {
            if (ById != null) {
                ById.Clear();
                ById = null;
            }
        }
    }

    /// <summary>
    /// Register / deregister / query operations for ElementTagLookup. ElementTag's
    /// IRegistrationCallbacks calls into Register / Deregister directly; the highlight
    /// utility uses TryGet to resolve Leaf-supplied ids.
    /// </summary>
    public static class ElementTagLookupUtility {
        [InvokePreBoot]
        static private void Create() {
            Game.SharedState.Register(new ElementTagLookup());
        }
        
        public static void Register(ElementTagLookup lookup, ElementTag tag) {
            Assert.NotNullOrDestroyed(lookup);
            Assert.NotNullOrDestroyed(tag);

            StringHash32 id = tag.Id.Hash();
            if (lookup.ById.TryGetValue(id, out ElementTag existing)) {
                // Duplicate id — treat as authoring error: warn and skip. The first tag stays
                // addressable; the second tag is simply not registered (its lookup queries will miss).
                Log.Warn("[Onboarding] Duplicate ElementTag id '{0}' (existing on '{1}', rejected on '{2}')",
                    tag.Id.Source(), existing.name, tag.name);
                return;
            }

            lookup.ById.Add(id, tag);
        }

        public static void Deregister(ElementTagLookup lookup, ElementTag tag) {
            Assert.NotNullOrDestroyed(lookup);
            Assert.NotNullOrDestroyed(tag);

            StringHash32 id = tag.Id.Hash();
            if (lookup.ById.TryGetValue(id, out ElementTag existing) && existing == tag) {
                lookup.ById.Remove(id);
            }
        }

        public static bool TryGet(ElementTagLookup lookup, StringHash32 id, out ElementTag tag) {
            return lookup.ById.TryGetValue(id, out tag);
        }
    }
}
