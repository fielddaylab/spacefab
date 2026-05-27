using System.Collections.Generic;
using BeauUtil;
using BeauUtil.Debugger;
using FieldDay;
using FieldDay.SharedState;

namespace SpaceFab.Onboarding {
    /// <summary>
    /// Singleton id -> ElementTag index, populated incrementally as ElementTag
    /// instances register and deregister. Onboarding scripting resolves Leaf-supplied
    /// ids through this lookup before asking the highlight system to act on them.
    /// Ids are unique by design: registration of a duplicate id asserts in editor and
    /// logs a warning in builds (the first-registered tag wins).
    /// </summary>
    public class ElementTagLookup : SharedStateComponent, IRegistrationCallbacks {
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
        public static void Register(ElementTagLookup lookup, ElementTag tag) {
            if (lookup.ById == null || tag == null) { return; }

            StringHash32 id = tag.Id.Hash();
            if (lookup.ById.TryGetValue(id, out ElementTag existing)) {
                // Duplicate ids are a setup bug — assert in editor so it surfaces during
                // scene work, and skip the registration so the first tag remains addressable.
                Assert.Fail("[Onboarding] Duplicate ElementTag id '{0}' (existing on '{1}', rejected on '{2}')",
                    tag.Id.Source(), existing.name, tag.name);
                return;
            }

            lookup.ById.Add(id, tag);
        }

        public static void Deregister(ElementTagLookup lookup, ElementTag tag) {
            if (lookup.ById == null || tag == null) { return; }

            StringHash32 id = tag.Id.Hash();
            if (lookup.ById.TryGetValue(id, out ElementTag existing) && existing == tag) {
                lookup.ById.Remove(id);
            }
        }

        public static bool TryGet(ElementTagLookup lookup, StringHash32 id, out ElementTag tag) {
            if (lookup.ById == null) {
                tag = null;
                return false;
            }
            return lookup.ById.TryGetValue(id, out tag);
        }
    }
}
