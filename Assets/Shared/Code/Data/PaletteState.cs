using FieldDay;
using FieldDay.SharedState;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Spacefab
{

    [Serializable]
    public struct GamePalette {
        public Color Money;
        public Color Time;
        public Color Metal;
        public Color Semiconductor;
        public Color Insulator;
        public Color NType;
        public Color PType;
        public Color Risk;
    }

    public class PaletteState : SharedStateComponent, IRegistrationCallbacks
    {
        public GamePalette MainPalette;
        public GamePalette CurrPalette { get; private set; }

        public void OnDeregister()
        {
        }

        public void OnRegister()
        {
            CurrPalette = MainPalette;
        }
    }
}