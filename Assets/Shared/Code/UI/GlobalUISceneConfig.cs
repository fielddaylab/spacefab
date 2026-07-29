using BeauUtil;
using FieldDay;
using FieldDay.Scenes;
using FieldDay.SharedState;
using SpaceFab.UI;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace SpaceFab
{
    /// <summary>
    /// Menu to return from Minigame to Overarching scene
    /// </summary>
    public class GlobalUISceneConfig : SharedStateComponent
    {
        public SceneReference ReturnScene;
        public bool DisplayWiki;
        public bool DisplayHelper;
    }
}
