using System.Collections;
using BeauRoutine;
using UnityEngine;
using UnityEngine.UI;

namespace SpaceFab.Onboarding {
    /// <summary>
    /// Pooled tutorial highlight visual. Carries one 9-sliced UI Image child and one
    /// 9-sliced world SpriteRenderer child; Bind enables exactly one of them depending
    /// on the target ElementTag's cached references. Position / size / sort order are
    /// set once at Bind — the highlight is parented to the target's transform so
    /// normal Transform inheritance handles any subsequent target movement. The pulse
    /// animation runs on a BeauRoutine started in Bind and stopped in Release.
    /// </summary>
    public class Highlight : MonoBehaviour {
        [Header("UI Path")]
        [SerializeField] private RectTransform m_UIRoot;
        [SerializeField] private Image m_UIImage;

        [Header("World Path")]
        [SerializeField] private Transform m_WorldRoot;
        [SerializeField] private SpriteRenderer m_WorldSprite;

        [Header("Pulse")]
        [SerializeField] private float m_PulseScale = 1.08f;
        [SerializeField] private float m_PulseDuration = 0.6f;
        [SerializeField, Range(0f, 1f)] private float m_PulseAlphaMin = 0.6f;
        [SerializeField, Range(0f, 1f)] private float m_PulseAlphaMax = 1f;

        [System.NonSerialized] private Routine m_PulseRoutine;
        [System.NonSerialized] private Transform m_ActiveVisualRoot;

        /// <summary>
        /// Positions / sizes / parents the highlight to wrap the given tag's target,
        /// activates the matching visual path, and starts the pulse animation.
        /// </summary>
        public void Bind(ElementTag tag, float margin) {
            DisableBothPaths();

            if (tag.RectTransform != null) {
                BindUI(tag.RectTransform, margin);
                m_ActiveVisualRoot = m_UIRoot;
            } else if (tag.SpriteRenderer != null) {
                BindWorldSprite(tag.SpriteRenderer, margin / 128f);
                m_ActiveVisualRoot = m_WorldRoot.transform;
            } else if (tag.Collider != null) {
                BindWorldCollider(tag.Collider, margin / 128f);
                m_ActiveVisualRoot = m_WorldRoot.transform;
            } else {
                Debug.LogWarning(string.Format(
                    "[Onboarding] ElementTag '{0}' has no RectTransform, SpriteRenderer, or Collider2D assigned.",
                    tag.Id.Source()), tag);
                return;
            }

            gameObject.SetActive(true);
            m_PulseRoutine.Replace(this, PulseRoutine());
        }

        /// <summary>
        /// Stops the pulse and deactivates both visual paths so the pool can park
        /// the GameObject. The pool layer reparents back to PoolTransform.
        /// </summary>
        public void Release() {
            m_PulseRoutine.Stop();
            DisableBothPaths();
            m_ActiveVisualRoot = null;
            gameObject.SetActive(false);
        }

        // Wraps a UI target. Parents the UI root next to the target (same parent +
        // higher sibling index) so it shares the target's canvas, then forces centered
        // anchors / pivot so the highlight's own rect math is independent of however
        // the target is anchored. Position is taken from the target's rendered rect
        // center; size is taken from the target's rendered rect plus margin.
        private void BindUI(RectTransform target, float margin) {
            m_UIRoot.SetParent(target.parent, false);
            m_UIRoot.SetSiblingIndex(target.GetSiblingIndex() + 1);

            // Force the highlight onto the standard centered layout regardless of how
            // the target is anchored — that way size and position are simple, predictable
            // values rather than offsets that depend on the target's anchor configuration.
            Vector2 centered = new Vector2(0.5f, 0.5f);
            m_UIRoot.anchorMin = centered;
            m_UIRoot.anchorMax = centered;
            m_UIRoot.pivot = centered;

            // target.rect.size is the target's rendered size; its center in the target's
            // own local space is at (0.5 - pivot) * size. Adding that to target.localPosition
            // (where the target's pivot sits in the shared parent space) gives the rect
            // center in the parent space — which is where the highlight's pivot belongs.
            Vector2 targetSize = target.rect.size;
            Vector2 pivotToCenter = Vector2.Scale(centered - target.pivot, targetSize);
            m_UIRoot.localPosition = target.localPosition + (Vector3) pivotToCenter;
            m_UIRoot.sizeDelta = targetSize + new Vector2(margin * 2f, margin * 2f);

            m_UIRoot.localScale = Vector3.one;
            m_UIRoot.gameObject.SetActive(true);
            SetUIAlpha(m_PulseAlphaMax);
        }

        // Wraps a world target via its Collider2D bounds. Parents the world root to the
        // target so it inherits movement, then converts the collider's world-space AABB
        // into the parent's local space so the sliced sprite's `size` (which is applied
        // pre-Transform) draws at exactly the collider's footprint.
        private void BindWorldCollider(Collider2D target, float margin) {
            m_WorldRoot.SetParent(target.transform, false);
            m_WorldRoot.localRotation = Quaternion.identity;
            m_WorldRoot.localScale = Vector3.one;

            Bounds worldBounds = target.bounds;

            // Center the highlight on the collider's world center, expressed in the parent's
            // local space — the collider's pivot is not necessarily the transform's pivot.
            Vector3 localCenter = target.transform.InverseTransformPoint(worldBounds.center);
            m_WorldRoot.localPosition = new Vector3(localCenter.x, localCenter.y, 0f);

            // Convert world-space bounds size into the parent's local units by dividing
            // out the parent's lossy scale. This is what makes the sliced sprite render
            // at the collider's actual on-screen footprint instead of being scaled up
            // a second time by the parent transform.
            Vector3 lossy = target.transform.lossyScale;
            Vector2 localSize = new Vector2(
                worldBounds.size.x / Mathf.Max(Mathf.Abs(lossy.x), 0.0001f),
                worldBounds.size.y / Mathf.Max(Mathf.Abs(lossy.y), 0.0001f)
            );

            m_WorldSprite.drawMode = SpriteDrawMode.Sliced;
            m_WorldSprite.size = localSize + new Vector2(margin * 2f, margin * 2f);

            // Match the target's sort layer so the highlight participates in the same
            // sort group, then nudge one order higher so it draws as an overlay. If the
            // target has no SpriteRenderer (e.g. a bare collider on a parent transform),
            // fall back to leaving the highlight's prefab sort settings in place.
            SpriteRenderer targetRenderer = target.GetComponent<SpriteRenderer>();
            if (targetRenderer != null) {
                m_WorldSprite.sortingLayerID = targetRenderer.sortingLayerID;
                m_WorldSprite.sortingOrder = targetRenderer.sortingOrder + 1;
            }

            m_WorldRoot.gameObject.SetActive(true);
            SetWorldAlpha(m_PulseAlphaMax);
        }

        // Wraps a world target via its SpriteRenderer. Parents the world root to the target
        // so it inherits movement, then sizes the sliced highlight to the sprite's intrinsic
        // local size (sprite.bounds.size, in the sprite's own units — independent of the
        // target's draw mode or scale). Centered on the sprite's local pivot since the
        // sprite's transform pivot already places it correctly under the target.
        private void BindWorldSprite(SpriteRenderer target, float margin) {
            m_WorldRoot.SetParent(target.transform, false);
            m_WorldRoot.localPosition = Vector3.zero;
            m_WorldRoot.localRotation = Quaternion.identity;
            m_WorldRoot.localScale = Vector3.one;

            m_WorldSprite.drawMode = SpriteDrawMode.Sliced;
            // sprite.bounds.size is the sprite's intrinsic size in its own local units;
            // m_WorldSprite.size in Sliced mode is also pre-Transform, so this matches the
            // visible footprint exactly when the highlight inherits the target's scale.
            Vector2 localSize = target.sprite != null ? (Vector2) target.sprite.bounds.size : Vector2.one;
            m_WorldSprite.size = localSize + new Vector2(margin * 2f, margin * 2f);

            m_WorldSprite.sortingLayerID = target.sortingLayerID;
            m_WorldSprite.sortingOrder = target.sortingOrder + 1;

            m_WorldRoot.gameObject.SetActive(true);
            SetWorldAlpha(m_PulseAlphaMax);
        }

        private void DisableBothPaths() {
            if (m_UIRoot != null) {
                m_UIRoot.gameObject.SetActive(false);
            }
            if (m_WorldRoot != null) {
                m_WorldRoot.gameObject.SetActive(false);
            }
        }

        // Single combined pulse: scale and alpha share the same yoyo loop so the
        // visual reads as one breathing motion. Yoyo wave with cosine-style easing
        // keeps the motion symmetric without manually chaining out / back tweens.
        private IEnumerator PulseRoutine() {
            yield return Tween.Float(0f, 1f, ApplyPulsePhase, m_PulseDuration)
                .Ease(Curve.Smooth)
                .YoyoLoop();
        }

        // phase: 0 -> 1 over m_PulseDuration; yoyo loop sends 1 -> 0 on the way back.
        private void ApplyPulsePhase(float phase) {
            float scale = Mathf.Lerp(1f, m_PulseScale, phase);
            if (m_ActiveVisualRoot != null) {
                m_ActiveVisualRoot.localScale = new Vector3(scale, scale, 1f);
            }

            float alpha = Mathf.Lerp(m_PulseAlphaMin, m_PulseAlphaMax, phase);
            if (m_UIRoot != null && m_UIRoot.gameObject.activeSelf) {
                SetUIAlpha(alpha);
            } else if (m_WorldRoot != null && m_WorldRoot.gameObject.activeSelf) {
                SetWorldAlpha(alpha);
            }
        }

        private void SetUIAlpha(float a) {
            if (m_UIImage == null) { return; }
            Color c = m_UIImage.color;
            c.a = a;
            m_UIImage.color = c;
        }

        private void SetWorldAlpha(float a) {
            if (m_WorldSprite == null) { return; }
            Color c = m_WorldSprite.color;
            c.a = a;
            m_WorldSprite.color = c;
        }
    }
}
