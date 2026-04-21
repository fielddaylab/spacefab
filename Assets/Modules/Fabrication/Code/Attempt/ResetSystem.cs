using FieldDay.Systems;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace SpaceFab.Fabrication
{
    /// <summary>
    /// Manages relevant state for resetting an attempt.
    /// Clear wafer state, reset timer, etc.
    /// </summary>
    public class ResetSystem : SharedStateSystemBehaviour<WaferState>
    {
		static private void ProcessWork(float deltaTime) {
			GetDependencies();
		}

		protected override unsafe delegate*<float, void> GetDelegate() {
			return &ProcessWork;
		}
	}
}