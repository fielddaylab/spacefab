using UnityEngine;

namespace SpaceFab.Research
{
    public class Electron : MonoBehaviour
    {
        // current travel distance of the electron relative to its active segment
        public float TravelDistance;
        // index of the circuit segment the electron is currently traversing
        public int FlowSegmentIndex;
        public SpriteRenderer Sprite;
    }
}