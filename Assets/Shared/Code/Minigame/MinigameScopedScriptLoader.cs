using BeauUtil;
using FieldDay;
using FieldDay.Scenes;
using FieldDay.Scripting;
using SpaceFab.Overarching;
using System.Collections.Generic;
using UnityEngine;

namespace SpaceFab {
    /// <summary>
    /// Registers the subset of every active ScopedMinigameScripts manifest's bindings whose
    /// AllowedIn contains this minigame's id, holding them for the duration of the minigame
    /// visit and unloading them on scene unload. Sits alongside the always-on
    /// FieldDay.Scripting.ScriptLoader pattern; per-minigame scripts live in scope manifests
    /// instead of in that loader's Scripts[].
    ///
    /// Aggregates across all loaded scope manifests (one per host scene — typically a chapter
    /// scene and/or a contract scene), so multiple persistent additive scenes can contribute
    /// per-minigame bindings simultaneously.
    ///
    /// Loads enqueued on Preload() are awaited by ScriptDatabase's existing scene-load dependency,
    /// so registered nodes are in the trigger buckets before OnSceneReady fires and before
    /// MinigameLoadExitSystem dispatches the OnMinigameLoad Leaf trigger.
    /// </summary>
    [PreloadOrder(1000)]
    public sealed class MinigameScopedScriptLoader : MonoBehaviour, IScenePreload, ISceneUnloadHandler {
        public MinigameId ThisMinigame;

        [System.NonSerialized] private List<UniqueId16> m_LoadHandles;

        IEnumerator<WorkSlicer.Result?> IScenePreload.Preload() {
            var providers = Find.Components<ScopedMinigameScripts>();
            if (providers.Count == 0) {
                return null;
            }

            for (int p = 0; p < providers.Count; p++) {
                MinigameScriptBinding[] bindings = providers[p].Bindings;
                if (bindings == null) {
                    continue;
                }
                for (int i = 0; i < bindings.Length; i++) {
                    if (BindingMatches(bindings[i], ThisMinigame)) {
                        if (m_LoadHandles == null) {
                            m_LoadHandles = new List<UniqueId16>(8);
                        }
                        m_LoadHandles.Add(ScriptDBUtility.Load(bindings[i].Script));
                    }
                }
            }

            return null;
        }

        void ISceneUnloadHandler.OnSceneUnload(SceneBinding inScene, object inContext) {
            if (m_LoadHandles == null) {
                return;
            }

            for (int i = 0; i < m_LoadHandles.Count; i++) {
                ScriptDBUtility.Unload(m_LoadHandles[i]);
            }
            m_LoadHandles = null;
        }

        private static bool BindingMatches(in MinigameScriptBinding binding, MinigameId minigame) {
            if (binding.Script == null || binding.AllowedIn == null) {
                return false;
            }
            for (int i = 0; i < binding.AllowedIn.Length; i++) {
                if (binding.AllowedIn[i] == minigame) {
                    return true;
                }
            }
            return false;
        }
    }
}
