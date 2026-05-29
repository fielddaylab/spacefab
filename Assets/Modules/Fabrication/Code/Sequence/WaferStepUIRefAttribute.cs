using System;
using System.Diagnostics;
using UnityEngine;

namespace SpaceFab.Fabrication.Sequence
{
    /// <summary>
    /// Marks a SerializedHash32 field as referencing an entry id inside the singleton
    /// WaferStepUILookup global asset. The inspector renders a dropdown of available ids
    /// (plus a "&lt;none&gt;" choice for optional fields). Pairs with
    /// WaferStepUIRefDrawer under Editor/.
    /// </summary>
    [Conditional("UNITY_EDITOR")]
    public class WaferStepUIRefAttribute : PropertyAttribute
    {
        public WaferStepUIRefAttribute()
        {
            order = -10;
        }
    }
}
