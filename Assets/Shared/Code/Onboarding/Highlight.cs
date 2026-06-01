using System.Collections;
using BeauRoutine;
using UnityEngine;
using UnityEngine.UI;

namespace SpaceFab.Onboarding {
    /// <summary>
    /// Pooled tutorial highlight visual. Carries one 9-sliced UI Image child and one
    /// 9-sliced world SpriteRenderer child; Bind enables exactly one of them depending
    /// on the target ElementTag's cached references. Bind does the one-time structural
    /// setup (parenting, anchors, draw mode, pulse) and a first alignment; thereafter
    /// LateUpdate re-aligns whenever the target's x or y has moved, so a highlight tracks
    /// targets that are repositioned within their parent (e.g. UI layout) — not just ones
    /// that move via Transform inheritance. The pulse animation runs on a BeauRoutine
    /// started in Bind and stopped in Release.
    /// </summary>
    public class Highlight : MonoBehaviour {
        // Which visual path / target source a bound highlight is tracking. Selected in Bind
        // from the target ElementTag's cached references; drives the per-frame re-align.
        private enum TargetKind {
            None,
            UI,
            WorldSprite,
            WorldCollider,
        }

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

        // Bind-time state retained for per-frame re-alignment.
        [System.NonSerialized] private TargetKind m_TargetKind;
        [System.NonSerialized] private RectTransform m_TargetRect;
        [System.NonSerialized] private SpriteRenderer m_TargetSprite;
        [System.NonSerialized] private Collider2D m_TargetCollider;
        [System.NonSerialized] private float m_Margin;
        // Last target position the highlight aligned to (UI: target.localPosition; world:
        // target world position). Compared each frame to detect movement.
        [System.NonSerialized] private Vector2 m_LastTargetPos;

        /// <summary>
        /// Positions / sizes / parents the highlight to wrap the given tag's target,
        /// activates the matching visual path, and starts the pulse animation. The target
        /// references are retained so LateUpdate can re-align if the target moves.
        /// </summary>
        public void Bind(ElementTag tag, float margin) {
            DisableBothPaths();
            ClearTarget();

            if (tag.RectTransform != null) {
                m_TargetKind = TargetKind.UI;
                m_TargetRect = tag.RectTransform;
                m_Margin = margin;
                BindUI(tag.RectTransform, margin);
                m_ActiveVisualRoot = m_UIRoot;
            } else if (tag.SpriteRenderer != null) {
                m_TargetKind = TargetKind.WorldSprite;
                m_TargetSprite = tag.SpriteRenderer;
                m_Margin = margin / 128f;
                BindWorldSprite(tag.SpriteRenderer, m_Margin);
                m_ActiveVisualRoot = m_WorldRoot.transform;
            } else if (tag.Collider != null) {
                m_TargetKind = TargetKind.WorldCollider;
                m_TargetCollider = tag.Collider;
                m_Margin = margin / 128f;
                BindWorldCollider(tag.Collider, m_Margin);
                m_ActiveVisualRoot = m_WorldRoot.transform;
            } else {
                Debug.LogWarning(string.Format(
                    "[Onboarding] ElementTag '{0}' has no RectTransform, SpriteRenderer, or Collider2D assigned.",
                    tag.Id.Source()), tag);
                return;
            }

            // Seed the movement baseline so LateUpdate only re-aligns on an actual change.
            TryGetTargetPos(out m_LastTargetPos);

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
            ClearTarget();
            m_ActiveVisualRoot = null;
            gameObject.SetActive(false);
        }

        // Re-align to the target whenever its x or y has changed since the last alignment.
        // LateUpdate (not Update) so it runs after any layout / gameplay movement this frame.
        private void LateUpdate() {
            if (m_TargetKind == TargetKind.None) { return; }

            // Target destroyed out from under us (scene swap, pooled host returned): nothing
            // to track. Release is the normal teardown path; this is just defensive.
            if (!TryGetTargetPos(out Vector2 currentPos)) { return; }

            if (currentPos != m_LastTargetPos) {
                m_LastTargetPos = currentPos;
                Realign();
            }
        }

        // Reads the position the movement check compares against, per target kind. UI uses
        // the target's localPosition (it moves within its parent on layout); world targets
        // use world position. Returns false when the target reference has been destroyed.
        private bool TryGetTargetPos(out Vector2 pos) {
            switch (m_TargetKind) {
                case TargetKind.UI:
                    if (m_TargetRect == null) { break; }
                    pos = m_TargetRect.localPosition;
                    return true;
                case TargetKind.WorldSprite:
                    if (m_TargetSprite == null) { break; }
                    pos = m_TargetSprite.transform.position;
                    return true;
                case TargetKind.WorldCollider:
                    if (m_TargetCollider == null) { break; }
                    pos = m_TargetCollider.transform.position;
                    return true;
            }
            pos = default;
            return false;
        }

        // Re-runs only the position/size math for the active target kind. Deliberately does
        // NOT touch parenting, anchors, draw mode, scale, or active state — those are set once
        // in Bind. Keeping scale out of the re-align is what prevents this from fighting the
        // pulse routine, which animates m_ActiveVisualRoot.localScale every frame.
        private void Realign() {
            switch (m_TargetKind) {
                case TargetKind.UI:
                    AlignUI(m_TargetRect, m_Margin);
                    break;
                case TargetKind.WorldSprite:
                    AlignWorldSprite(m_TargetSprite, m_Margin);
                    break;
                case TargetKind.WorldCollider:
                    AlignWorldCollider(m_TargetCollider, m_Margin);
                    break;
            }
        }

        // Clears retained target references so a pooled-and-reused Highlight doesn't track a
        // stale target between binds.
        private void ClearTarget() {
            m_TargetKind = TargetKind.None;
            m_TargetRect = null;
            m_TargetSprite = null;
            m_TargetCollider = null;
        }

        // Wraps a UI target. Parents the UI root next to the target (same parent +
        // higher sibling index) so it shares the target's canvas, then forces centered
        // anchors / pivot so the highlight's own rect math is independent of however
        // the target is anchored. Position / size are applied by AlignUI, which also runs
        // each frame the target moves (the UI root is parented to the target's PARENT, so
        // it does not inherit the target's own movement within that parent).
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

            m_UIRoot.localScale = Vector3.one;
            m_UIRoot.gameObject.SetActive(true);
            SetUIAlpha(m_PulseAlphaMax);

            AlignUI(target, margin);
        }

        // Position + size only — safe to call every frame without disturbing the pulse
        // (which owns localScale) or the parenting / anchor setup done in BindUI.
        private void AlignUI(RectTransform target, float margin) {
            // target.rect.size is the target's rendered size; its center in the target's
            // own local space is at (0.5 - pivot) * size. Adding that to target.localPosition
            // (where the target's pivot sits in the shared parent space) gives the rect
            // center in the parent space — which is where the highlight's pivot belongs.
            Vector2 centered = new Vector2(0.5f, 0.5f);
            Vector2 targetSize = target.rect.size;
            Vector2 pivotToCenter = Vector2.Scale(centered - target.pivot, targetSize);
            m_UIRoot.localPosition = target.localPosition + (Vector3) pivotToCenter;
            m_UIRoot.sizeDelta = targetSize + new Vector2(margin * 2f, margin * 2f);
        }

        // Wraps a world target via its Collider2D bounds. Parents the world root to the
        // target so it inherits movement, then converts the collider's world-space AABB
        // into the parent's local space so the sliced sprite's `size` (which is applied
        // pre-Transform) draws at exactly the collider's footprint.
        private void BindWorldCollider(Collider2D target, float margin) {
            m_WorldRoot.SetParent(target.transform, false);
            m_WorldRoot.localRotation = Quaternion.identity;
            m_WorldRoot.localScale = Vector3.one;

            m_WorldSprite.drawMode = SpriteDrawMode.Sliced;
            m_WorldRoot.gameObject.SetActive(true);
            SetWorldAlpha(m_PulseAlphaMax);

            AlignWorldCollider(target, margin);
        }

        // Position + size only. Re-runnable each frame: it writes m_WorldRoot.localPosition
        // (not scale) so it coexists with the pulse, and the parent set in BindWorldCollider
        // still holds. Useful when the collider's footprint shifts within the target (the
        // root inherits the target transform's own movement, but a bounds.center offset that
        // changes — e.g. an animated collider — would otherwise drift).
        private void AlignWorldCollider(Collider2D target, float margin) {
            // Collider2D.bounds reads the physics-engine AABB, which only refreshes on a
            // physics sync. The project runs with Physics2D.autoSyncTransforms off, and a
            // highlight can be bound the same frame its target was repositioned (e.g. tray
            // samples are laid out in SpawnTray, then highlighted from OnSetupComplete before
            // any FixedUpdate). Without this sync, bounds.center reflects the pre-move position
            // and the highlight lands on whatever sibling occupied that spot.
            Physics2D.SyncTransforms();
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

            m_WorldSprite.size = localSize + new Vector2(margin * 2f, margin * 2f);
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
            m_WorldRoot.gameObject.SetActive(true);
            SetWorldAlpha(m_PulseAlphaMax);

            AlignWorldSprite(target, margin);
        }

        // Size only — the sprite root sits at the target's local origin and inherits the
        // target's movement via parenting, so position needs no per-frame update; this keeps
        // the footprint correct if the target's sprite (and thus its intrinsic size) changes.
        // Does not touch scale, so it coexists with the pulse.
        private void AlignWorldSprite(SpriteRenderer target, float margin) {
            // sprite.bounds.size is the sprite's intrinsic size in its own local units;
            // m_WorldSprite.size in Sliced mode is also pre-Transform, so this matches the
            // visible footprint exactly when the highlight inherits the target's scale.
            Vector2 localSize = target.sprite != null ? (Vector2) target.sprite.bounds.size : Vector2.one;
            m_WorldSprite.size = localSize + new Vector2(margin * 2f, margin * 2f);
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
