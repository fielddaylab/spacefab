using BeauUtil;
using FieldDay;
using FieldDay.Scenes;
using FieldDay.Scripting;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace SpaceFab {
    [PreloadOrder(1000)]
    public sealed class ScopedScriptLoader : MonoBehaviour, IScenePreload, ISceneUnloadHandler {
        public ScopedScriptsAsset.Mask SceneMask;

        [NonSerialized] private RingBuffer<UniqueId16> m_LoadHandles;

        IEnumerator<WorkSlicer.Result?> IScenePreload.Preload() {
            var providers = Find.NamedAssets<ScopedScriptsAsset>();
            if (providers.Count == 0) {
                return null;
            }

            foreach(var provider in providers) {
                ScopedScriptsAsset.Binding[] bindings = provider.Bindings;
                for(int i = 0; i < bindings.Length; i++) {
                    if ((bindings[i].Scope & SceneMask) == 0) {
                        continue;
                    }

                    if (m_LoadHandles == null) {
                        m_LoadHandles = new RingBuffer<UniqueId16>(4, RingBufferMode.Expand);
                    }
                    m_LoadHandles.PushBack(ScriptDBUtility.Load(bindings[i].Script));
                }
            }

            return null;
        }

        void ISceneUnloadHandler.OnSceneUnload(SceneBinding inScene, object inContext) {
            if (m_LoadHandles == null) {
                return;
            }

            while(m_LoadHandles.TryPopBack(out UniqueId16 handle)) {
                ScriptDBUtility.Unload(handle);
            }

            m_LoadHandles = null;
        }
    }
}
