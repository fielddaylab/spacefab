using FieldDay.Components;
using SpaceFab.Fabrication.Stations;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace SpaceFab.Fabrication.Microgames
{
    /// <summary>
    /// Photoresist station microgame: "Spin-Coat." A dropper above the wafer moves left-right;
    /// players press Activate to drop photoresist as close to the wafer's center as possible.
    /// Precision is distance-from-center.
    ///
    /// Unity-side handle for the IMicrogame interface; logic and data live in
    /// ResistMicrogameState / ResistMicrogameUtility / ResistMicrogameSystem.
    /// </summary>
    public class ResistMicrogame : BatchedComponent, IMicrogame
    {
        [SerializeField] private GameObject m_ResistGameUI;
        [SerializeField] private Transform m_DropperTransform;
        [SerializeField] private float dropperAmplitude = 10, dropperSpeed = 10;


        public void Update()
        {
            Vector3 dropperPosition = m_DropperTransform.position;

            dropperPosition.x = dropperAmplitude * Mathf.Sin(Time.time * dropperSpeed); // sin curve for now, maybe change?
            
            m_DropperTransform.position = dropperPosition;
        }

        public bool CanActivateNow() => ResistMicrogameUtility.CanActivate();
        public void OnEnterBegin() => ResistMicrogameUtility.EnterBegin();
        public void OnEnterComplete() => ResistMicrogameUtility.EnterComplete();
        public void OnExitBegin(bool completedNormally) => ResistMicrogameUtility.ExitBegin(completedNormally);
        public bool IsProcessAnimationComplete() => ResistMicrogameUtility.IsProcessAnimationComplete();
        public void OnExitComplete() => ResistMicrogameUtility.ExitComplete();
    }
}
