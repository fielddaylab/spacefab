
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
    public class QuickToolbar : BaseGuiModule, IRegistrationCallbacks
    {
        #region Inspector

        public Canvas Canvas;
        public BaseRaycasterInputLayer InputLayer;
        
        [Header("Buttons")]
        public GuiButton HelperButton;
        public GuiButton WikiButton;

        #endregion // Inspector

        public void OnDeregister() {
            Game.Scenes.OnMainSceneLateEnable.Deregister(OnSceneLateEnable);
        }

        public void OnRegister() {
            Game.Scenes.OnMainSceneLateEnable.Register(OnSceneLateEnable);
        }

        private void OnSceneLateEnable() {
            bool visible = false;
            if (Game.SharedState.TryGet(out GlobalUISceneConfig config)) {
                HelperButton.gameObject.SetActive(config.DisplayHelper);
                WikiButton.gameObject.SetActive(config.DisplayWiki);
                visible = config.DisplayHelper | config.DisplayWiki;
            }

            Canvas.enabled = visible;
            InputLayer.SetInputOverride(visible ? null : false);
        }
    }
}
