using BeauUtil;
using FieldDay;
using FieldDay.Components;
using UnityEngine;

namespace SpaceFab.Onboarding {
    /// <summary>
    /// Marks a GameObject as a tutorial-addressable element. The Id is what Leaf calls
    /// reference when summoning a highlight on this element. ElementTag self-registers
    /// with ElementTagLookup so the highlight system can resolve id -> component in O(1).
    /// One ElementTag may sit on a UI RectTransform host, a world Collider2D host,
    /// or both — the cached references are looked up at registration time so the
    /// highlight system doesn't GetComponent per call. World highlights size off the
    /// Collider2D's bounds, so the designer controls the highlight footprint by
    /// shaping the collider rather than by the sprite's rendered size.
    /// </summary>
    public class ElementTag : BatchedComponent, IRegistrationCallbacks {
        // SerializedHash32 because the id is authored in the inspector and the original
        // source string needs to be recoverable for debug logs / collision messages.
        public SerializedHash32 Id;

        [Header("Targets")]
        // Assign one of these in the inspector. RectTransform drives the UI highlight
        // path; Collider drives the world highlight path. If both are left null, OnRegister
        // falls back to GetComponent for each — the explicit assignment exists so a tag
        // sitting on a parent can point at a child renderer/collider, and is preferred
        // because it survives hierarchy refactors. If both are still null after fallback,
        // HighlightElement on this id logs a warning.
        public RectTransform RectTransform;
        public Collider2D Collider;

        public void OnRegister() {
            if (RectTransform == null) {
                RectTransform = GetComponent<RectTransform>();
            }
            if (Collider == null) {
                Collider = GetComponent<Collider2D>();
            }

            RegisterCurrentId();
        }

        public void OnDeregister() {
            DeregisterCurrentId();
        }

        /// <summary>
        /// Reassigns this tag's id at runtime, updating the lookup atomically: deregisters
        /// the old id, swaps in the new one, registers under the new id. Pass default to
        /// clear the tag from the lookup (e.g. when a pooled object is being returned).
        /// </summary>
        public void SetId(StringHash32 newIdHash) {
            StringHash32 currentHash = Id.Hash();
            if (currentHash == newIdHash) { return; }

            DeregisterCurrentId();
            // SerializedHash32 has an implicit operator from StringHash32, so this assignment
            // round-trips the hash without preserving a source string. Source-string recovery
            // is only needed for the inspector-authored case (warnings / debug logs); runtime
            // SetId callers pass hashes whose source string isn't meaningful here.
            Id = newIdHash;
            RegisterCurrentId();
        }

        private void RegisterCurrentId() {
            if (Id.IsEmpty) { return; }
            ElementTagLookup lookup = Find.State<ElementTagLookup>();
            if (lookup != null) {
                ElementTagLookupUtility.Register(lookup, this);
            }
        }

        private void DeregisterCurrentId() {
            if (Id.IsEmpty) { return; }
            ElementTagLookup lookup = Find.State<ElementTagLookup>();
            if (lookup != null) {
                ElementTagLookupUtility.Deregister(lookup, this);
            }
        }
    }
}
