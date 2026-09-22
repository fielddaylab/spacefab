using BeauUtil;
using FieldDay;
using FieldDay.Debugging;
using FieldDay.HID;
using FieldDay.Scenes;
using UnityEngine;

namespace SpaceFab.Overarching
{
    public class OverarchingDebugCamera : MonoBehaviour, IDevModeOnly {
        private void Awake() {
            GameLoop.OnDebugUpdate.Register(DebugUpdate);
        }

        private void OnDestroy() {
            GameLoop.OnDebugUpdate.Deregister(DebugUpdate);
        }

        static private void DebugUpdate() {
            if (DebugInput.IsDown(InputModifierKeys.Shift)) {
                if (DebugInput.IsPressed(KeyCode.Alpha1)) {
                    TrySnapCamera(0);
                } else if (DebugInput.IsPressed(KeyCode.Alpha2)) {
                    TrySnapCamera(1);
                } else if (DebugInput.IsPressed(KeyCode.Alpha3)) {
                    TrySnapCamera(2);
                } else if (DebugInput.IsPressed(KeyCode.Alpha4)) {
                    TrySnapCamera(3);
                } else if (DebugInput.IsPressed(KeyCode.Alpha5)) {
                    TrySnapCamera(4);
                } else if (DebugInput.IsPressed(KeyCode.Alpha6)) {
                    TrySnapCamera(6);
                }
            }
        }

        static private void TrySnapCamera(int index) {
            var components = Find.Components<OverarchingRenderPose>();
            if (index < components.Count) {
                OverarchingRenderPose pose = components[index];
                OverarchingRenderUtility.SwitchPose(Find.State<OverarchingCamera>(), pose);
                DebugDraw.AddLogText(string.Format("[Overarching] Switched to camera pose '{0}'", pose.name), Color.yellow, 1);
            } else {
                DebugDraw.AddLogText(string.Format("[Overarching] No camera pose with index {0}", index), ColorBank.PaleVioletRed, 1);
            }
        }
    }
}