using FieldDay.Systems;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace SpaceFab.Design
{
    /// <summary>
    /// Manages running through a sequence of tests in the test suite.
    /// Delegates to SimulateSingleTestSystem for each item in the multi-test sequence.
    /// </summary>
    public class SimulateMultiTestSystem : SharedStateSystemBehaviour<SimulateUIState>
    {

    }
}