using System;
using System.Collections;
using UnityEngine;

namespace SpaceFab.Fabrication.Microgames
{
    public class SputterPatternData : MonoBehaviour
    {
        public SputterBoxCollider[] Colliders;
        public SpriteRenderer ProjectilePrefab;
        public int m_FilledSlots, m_TotalSlots;
        public bool CompletelyFilled => m_FilledSlots == m_TotalSlots;

        private void Start()
        {
            m_TotalSlots = 0;
            foreach (var collider in Colliders)
            {
                m_TotalSlots += collider.GenerateSlot(ProjectilePrefab.bounds.size.x);
            }
        }
    }

}