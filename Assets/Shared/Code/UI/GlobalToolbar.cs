
using TMPro;
using System;
using BeauRoutine;
using FieldDay;
using UnityEngine;
using FieldDay.UI;
using FieldDay.Audio;
using FieldDay.UI.Widgets;

namespace SpaceFab
{
    public class GlobalToolbar : BaseGuiModule, IRegistrationCallbacks
    {
        #region Inspector

        public BaseRaycasterInputLayer InputLayer;
        
        [Header("Buttons")]
        public GuiButton ReturnButton;
        public GuiButton PauseButton;

        #endregion // Inspector

        public void OnDeregister() {
            throw new NotImplementedException();
        }

        public void OnRegister() {
            throw new NotImplementedException();
        }
    }
}
