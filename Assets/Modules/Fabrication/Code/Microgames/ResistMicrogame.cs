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

        public bool CanActivateNow()
        {
            // TODO: gate based on sequence / wafer state. Default true.
            return true;
        }

        public void OnEnterBegin()
        {
            // TODO: play intro; spawn dropper; begin left-right sweep.
        }

        public void OnEnterComplete()
        {
            // TODO: start accepting Activate-press input.
        }

        // On normal completion, compute precision and commit it to the wafer at the current step.
        // On cancel, nothing is recorded.
        public void OnExitBegin(bool completedNormally)
        {
            // TODO: freeze dropper.
            if (!completedNormally) { return; }

            MicrogameUtility.CommitStepPrecision(ComputePrecision());
        }

        // TODO: track process animation state (parallel or sequential) and return true once the
        // animation has finished playing. Scaffold returns true so the exit gate doesn't stall
        // before per-microgame animations are authored.
        public bool IsProcessAnimationComplete()
        {
            return true;
        }

        public void OnExitComplete()
        {
            // TODO: tear down dropper UI; return to idle.
        }

        // Spin-Coat-specific precision math: distance between drop position and wafer center.
        // Scaffold returns 0.
        private float ComputePrecision()
        {
            // TODO: precision = 1 - (abs(dropX - centerX) / maxOffset), clamped to [0,1].
            return 0f;
        }
    }
}
