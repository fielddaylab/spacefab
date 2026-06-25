using BeauUtil;
using FieldDay;
using FieldDay.Rendering;
using UnityEngine;

namespace SpaceFab {
    [ExecuteAlways]
    public sealed class PaperOverlay : MonoBehaviour, ICameraPostRenderCallback {
        public Material PaperMaterial;
        
        private void OnEnable() {
            CameraHelper.AddOnPostRender(this, -1000);
        }

        private void OnDisable() {
            CameraHelper.RemoveOnPostRender(this);
        }

        public void OnCameraPostRender(Camera inCamera, CameraCallbackSource inSource) {
            if (!CameraUtility.IsGameCamera(inCamera) || CameraUtility.IsOverlayCamera(inCamera)) {
                return;
            }

            FullscreenRender.PushImmediate();
            PaperMaterial.SetPass(0);
            FullscreenRender.ImmTri();
            FullscreenRender.PopImmediate();
        }
    }
}