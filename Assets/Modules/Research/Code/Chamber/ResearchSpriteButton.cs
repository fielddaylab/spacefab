using FieldDay.Components;
using FieldDay.UI;
using UnityEngine;

namespace SpaceFab.Research
{
    /// <summary>
    /// World-space sprite button for chamber UI: a SpriteRenderer plus a
    /// Collider2D plus a CursorHint that owns the click event. Consumers
    /// subscribe to Cursor.onClick. Clicks route through Unity's EventSystem
    /// via a Physics2DRaycaster on the camera, so the camera must have one
    /// and the Collider2D must be on a layer the raycaster sees.
    /// </summary>
    public class ResearchSpriteButton : BatchedComponent
    {
        public SpriteRenderer Sprite;
        public Collider2D Collider;
        public CursorHint Cursor;
    }
}
