using BeauUtil;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace SpaceFab.Research
{
    public static class ResearchConsts
    {

    }

    public static class ResearchScriptTriggers
    {
        public static readonly StringHash32 OnSetupComplete = "OnSetupComplete";
        public static readonly StringHash32 OnSampleLifted = "OnSampleLifted";
        public static readonly StringHash32 OnSlotFilled = "OnSlotFilled";
        public static readonly StringHash32 OnVoltageIncreased = "OnVoltageIncreased";
        public static readonly StringHash32 OnVoltageDecreased = "OnVoltageDecreased";
        public static readonly StringHash32 OnObservationPickerOpened = "OnObservationPickerOpened";
        public static readonly StringHash32 OnObservationAdded = "OnObservationAdded";
        public static readonly StringHash32 OnHypothesisSubmitted = "OnHypothesisSubmitted";
    }
}
