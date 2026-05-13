using BeauUtil;
using FieldDay.Debugging;
using FieldDay.Scenes;
using ScriptableBake;
using System;
using UnityEngine;

namespace SpaceFab.Comic {
    public sealed class ComicCameraNode : MonoBehaviour, IEditModeOnly {
        [Range(0.1f, 100)]
        public float ClipHeight = 25;

#if UNITY_EDITOR

        private void OnDrawGizmos() {
            Gizmos.color = Color.yellow.WithAlpha(0.3f);
            Vector3 size = new Vector3(ClipHeight * 4 / 3, ClipHeight, 0);
            Gizmos.matrix = transform.localToWorldMatrix;
            Gizmos.DrawWireCube(default, size);
        }

        private void OnDrawGizmosSelected() {
            if (!DebugFlags.IsSelected(gameObject)) {
                return;
            }

            Gizmos.color = Color.yellow.WithAlpha(0.3f);
            Vector3 size = new Vector3(ClipHeight * 4 / 3, ClipHeight, 0);
            Gizmos.matrix = transform.localToWorldMatrix;
            Gizmos.DrawCube(default, size);
        }

#endif // UNITY_EDITOR
    }

    [Serializable]
    public struct CameraData {
        public StringHash32 Id;
        public PackedPoint Position;
        public short PackedRotation;
        public short PackedClipHeight;
    }
}