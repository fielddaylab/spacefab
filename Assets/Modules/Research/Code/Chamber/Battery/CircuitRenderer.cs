using FieldDay.Components;
using System;
using UnityEngine;

namespace SpaceFab.Research
{
    /// <summary>
    /// Visual rig for the Battery chamber's circuit: the flow tube, the bulb
    /// with its on/off sprites, and the bulb shines that rotate while the
    /// circuit is active. CircuitAnimationSystem advances the flow frame and
    /// rotates the shines; chamber systems drive light strength + flow speed
    /// via CircuitUtility.
    /// </summary>
    public class CircuitRenderer : BatchedComponent
    {
        [System.Serializable]
        public class FlowSegment
        {
            public float Offset;
            public Vector2[] Points;
        }

        public Electron[] Electrons;
        public FlowSegment[] FlowSegmentPoints;

        public SpriteRenderer Bulb;
        public Sprite BulbOffSprite;
        public Sprite BulbOnSprite;

        public SpriteRenderer[] BulbShines;
        public float AnimSpeedMultiplier = 4f;

        // Magnitude drives flow density. Set by CircuitUtility.SetFlowStrength.
        [NonSerialized] public float CircuitCurrent;

        // Accumulator for the animation system; drives frame advance.
        [NonSerialized] public float CircuitSpriteTimer;

        // Current flow-sprite frame index. Driven by the animation system.
        [NonSerialized] public int CircuitSpriteIndex;
    }

    /// <summary>
    /// Drives the bulb on/off + shine alpha and the flow tube alpha + speed.
    /// Pure setters; the animation system reads CircuitSpriteSpeed and
    /// advances frames + rotates shines on its own.
    /// </summary>
    public static class CircuitUtility
    {
        // Bulb on if |strength| crosses a threshold; shine alpha scales as
        // strength squared so weak currents dim the shines toward zero faster
        // than the bulb fade.
        public static void SetLightStrength(CircuitRenderer circuit, float strength)
        {
            if (circuit == null) return;

            float magnitude = Mathf.Abs(strength);
            if (circuit.Bulb != null)
            {
                circuit.Bulb.sprite = magnitude > 0.1f ? circuit.BulbOnSprite : circuit.BulbOffSprite;
            }

            if (circuit.BulbShines != null)
            {
                float shineAlpha = magnitude * magnitude;
                for (int i = 0; i < circuit.BulbShines.Length; i++)
                {
                    SpriteRenderer shine = circuit.BulbShines[i];
                    if (shine == null) continue;
                    shine.enabled = magnitude > 0f;
                    Color c = shine.color;
                    c.a = shineAlpha;
                    shine.color = c;
                }
            }
        }

        // Sets the flow tube's density. The animation system reads CircuitSpriteSpeed each frame.
        public static void SetFlowStrength(CircuitRenderer circuit, float strength)
        {
            if (circuit == null) return;

            for (int i = 0; i < circuit.Electrons.Length; i++)
            {
                if (strength == 0)
                    circuit.Electrons[i].gameObject.SetActive(false);
                else if (strength < 0.3f)
                    circuit.Electrons[i].gameObject.SetActive(i % 2 == 0);
                else
                    circuit.Electrons[i].gameObject.SetActive(true);
            }
            circuit.CircuitCurrent = strength;
        }
    }
}
