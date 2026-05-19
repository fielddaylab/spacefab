using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace SpaceFab.Fabrication.Microgames
{
    public class IonPatternData : MonoBehaviour
    {
        public IonFillRenderer[] IonFillRenderers;

        private int m_TotalPoints, m_FilledPoints;

        public bool CompletelyFilled => m_TotalPoints == m_FilledPoints;

        public void SetupRenderers(float density, float fillRadius)
        {
            foreach (var i in IonFillRenderers)
            {
                m_TotalPoints += i.Setup(density, fillRadius);
                i.IsRendering = true;
            }
        }

        public void ProcessWork()
        {
            m_FilledPoints = 0;
            
            foreach (var i in IonFillRenderers)
            {
                m_FilledPoints += i.ProcessWork();
            }

            Debug.Log("Total points:" + m_TotalPoints + "Filled points: " + m_FilledPoints);
        }

        // decouple from process work as this must be called during exit
        public void PerformRendering()
        {
            foreach (var i in IonFillRenderers)
            {
                i.PerformRendering();
            }
        }
    }
}