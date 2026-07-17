using BeauUtil;
using BeauUtil.Debugger;
using FieldDay.Scenes;
using ScriptableBake;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace FieldDay.World {
    [PreloadOrder(-5000)]
    public sealed class PreloadRuntimeClones : MonoBehaviour, IScenePreload, IBaked {
        [SceneReferenceOnly] public GameObject Source;
        public uint CloneCount = 4;

        public IEnumerator<WorkSlicer.Result?> Preload() {
            if (CloneCount > 0) {
                Assert.NotNullOrDestroyed(Source, "Cannot clone null object");
                GameObject[] clones = new GameObject[CloneCount];
                for(int i = 0; i < clones.Length; i++) {
                    clones[i] = Instantiate(Source, Source.transform.parent);
                    yield return null;
                }

                CloneList tracker = Source.EnsureComponent<CloneList>();
                tracker.Clones = clones;
            }
        }

#if UNITY_EDITOR
        int IBaked.Order { get { return -1000; } }

        bool IBaked.Bake(BakeFlags flags, BakeContext context) {
            if (Source == null) {
                throw new BakeException("Cannot clone null object");
            }
            if (Source == gameObject || transform.IsChildOf(Source.transform)) {
                throw new BakeException("Cannot clone GameObject '{0}' from '{1}' - self or parent", Source.name, name);
            }
            if (SceneUtils.Editor.HasAnyScenePipelineStagesInHierarchy(Source)) {
                throw new BakeException("Cannot clone GameObject '{0}' with scene pipeline stage components", Source.name);
            }
            return false;
        }
#endif // UNITY_EDITOR
    }
}