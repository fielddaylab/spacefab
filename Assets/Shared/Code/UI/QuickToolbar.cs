
using TMPro;
using System;
using BeauRoutine;
using FieldDay;
using UnityEngine;
using FieldDay.UI;
using FieldDay.Audio;
using FieldDay.UI.Widgets;
using SpaceFab.UI;

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
            WikiButton.OnClick.RemoveAllListeners();
        }

        public void OnRegister() {
            Game.Scenes.OnMainSceneLateEnable.Register(OnSceneLateEnable);
        }

        private void OnSceneLateEnable() {
            bool visible = false;
            if (Game.SharedState.TryGet(out GlobalUISceneConfig config)) {
                HelperButton.gameObject.SetActive(config.DisplayHelper);
                WikiButton.gameObject.SetActive(config.DisplayWiki);
                if (config.DisplayWiki) {
                    WikiButton.OnClick.AddListener(OnCollapsedWikiClicked);
                    LoadWikiTabs(config);
                }
                visible = config.DisplayHelper | config.DisplayWiki;
            }

            Canvas.enabled = visible;
            InputLayer.SetInputOverride(visible ? null : false);
        }

        // Hands this scene's authored tab set to the shared wiki prefab. No-op when the wiki prefab
        // isn't loaded — a scene may display the toolbar button without shipping the panel.
        private void LoadWikiTabs(GlobalUISceneConfig config) {

            WikiUtility.LoadTabs(Find.State<WikiState>(), Find.State<WikiLayoutState>().WikiContent, config.WikiTabs);
        }

        #region // Handlers
        
        private void OnCollapsedWikiClicked() {
            WikiUtility.ToggleWikiOpen(Find.State<WikiState>());
        }

        #endregion // Handlers
    }
}
