using System.Collections;
using System.Collections.Generic;
using FieldDay;
using UnityEngine;

public sealed class SpacefabGame : Game {
    static public new EventDispatcher<EvtArgs> Events { get; private set; }

    [InvokePreBoot]
    static private void OnPreBoot()
    {
        Events = new EventDispatcher<EvtArgs>();
        SetEventDispatcher(Events);
    }

    [InvokeOnBoot]
    static private void OnBoot() {
    }
}
