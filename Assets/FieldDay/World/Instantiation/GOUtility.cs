using FieldDay.Collections;
using UnityEngine;

namespace FieldDay.World {
    static public partial class GOUtility {
        /// <summary>
        /// Retrieves a list of all clones of the given GameObject, and itself.
        /// </summary>
        static public TempReferenceBuffer<GameObject> SelfAndClones(GameObject go) {
            if (!go) {
                return default;
            }

            TempReferenceBuffer<GameObject> buffer = TempReferenceBuffer<GameObject>.Create();
            buffer.Add(go);

            if (go.TryGetComponent(out CloneList list)) {
                for (int i = 0; i < list.Clones.Length; i++) {
                    buffer.Add(list.Clones[i]);
                }
            }

            return buffer;
        }

        /// <summary>
        /// Retrieves a list of all clones of the given component's GameObject, and itself.
        /// </summary>
        static public TempReferenceBuffer<T> SelfAndClones<T>(T component) where T : Component {
            if (!component) {
                return default;
            }

            TempReferenceBuffer<T> buffer = TempReferenceBuffer<T>.Create();
            buffer.Add(component);

            if (component.TryGetComponent(out CloneList list)) {
                for (int i = 0; i < list.Clones.Length; i++) {
                    buffer.Add(list.Clones[i].GetComponent<T>());
                }
            }

            return buffer;
        }
    }
}