using System;
using System.Collections;
using UnityEngine;

namespace SpaceFab.Fabrication.Microgames
{
    public class SputterPatternData : MonoBehaviour
    {
        public SputterBoxCollider[] Colliders;
        public int m_FilledSlots, m_TotalSlots;
        public bool CompletelyFilled => m_FilledSlots == m_TotalSlots && m_FilledSlots > 0;

        public void SetPatternData(float size)
        {
            m_FilledSlots = 0;
            m_TotalSlots = 0;

            foreach (var collider in Colliders)
            {
                m_TotalSlots += collider.GenerateSlot(size);
            }
        }
    }
}