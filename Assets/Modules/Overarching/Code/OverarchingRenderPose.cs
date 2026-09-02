using BeauRoutine;
using BeauUtil;
using BeauUtil.Debugger;
using FieldDay;
using FieldDay.Components;
using FieldDay.Scripting;
using FieldDay.SharedState;
using ScriptableBake;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace SpaceFab.Overarching
{
    public class OverarchingRenderPose : ScriptActorComponent, IBaked {
        public OverarchingRenderPlane[] Planes;
        public ActiveGroup Activate;
        [Range(0, 1)] public float Padding;
        public Vector3 DriftMin;
        public Vector3 DriftMax;
        public float DriftSpeed;

        [Header("Camera Attributes")]
        public float NearPlane;
        public float FarPlane;
        public float FOV;
        public bool IsOrtho;
        public float OrthoSize;

#if UNITY_EDITOR

        int IBaked.Order { get { return -1; } }

        bool IBaked.Bake(BakeFlags flags, BakeContext context) {
            Activate.SetActive(false);
            foreach(var plane in Planes) {
                plane.transform.SetParent(transform, false);
                plane.transform.localPosition = new Vector3(0, 0, plane.Distance);

                float scale = plane.Distance * (1 + Padding);
                plane.transform.localScale = new Vector3(scale, scale, scale);
                plane.transform.SetParent(null);
            }
            return true;
        }

#endif // UNITY_EDITOR
    }

    static public partial class OverarchingRenderUtility {

        /// <summary>
        /// Preloads the assets for the given render plane.
        /// </summary>
        static public void PreloadPose(OverarchingRenderPose pose) {
            Assert.NotNullOrDestroyed(pose);

            foreach (var plane in pose.Planes) {
                foreach (var streamedQuad in plane.Streamed) {
                    streamedQuad.Preload();
                }
            }
        }

        /// <summary>
        /// Returns if the assets for the given render plane have been loaded.
        /// </summary>
        static public bool IsPoseLoaded(OverarchingRenderPose pose) {
            Assert.NotNullOrDestroyed(pose);

            foreach (var plane in pose.Planes) {
                foreach (var streamedQuad in plane.Streamed) {
                    if (!streamedQuad.IsLoaded()) {
                        return false;
                    }
                }
            }

            return true;
        }

        static public bool SwitchPose(OverarchingCamera camera, OverarchingRenderPose pose) {
            Assert.NotNullOrDestroyed(camera);
            Assert.NotNullOrDestroyed(pose);

            if (camera.CurrentPose == pose) {
                return false;
            }

            if (camera.CurrentPose != null) {
                DeactivatePose(camera.CurrentPose);
            }

            camera.CurrentPose = pose;
            ActivatePose(pose);

            pose.transform.GetPositionAndRotation(out Vector3 pos, out Quaternion rot);
            camera.Root.SetPositionAndRotation(pos, rot);

            camera.Camera.orthographic = pose.IsOrtho;
            camera.Camera.orthographicSize = pose.OrthoSize;
            camera.Camera.fieldOfView = pose.FOV;
            camera.Camera.nearClipPlane = pose.NearPlane;
            camera.Camera.farClipPlane = pose.FarPlane;

            return true;
        }

        /// <summary>
        /// Activates all assets for the given pose.
        /// </summary>
        static public void ActivatePose(OverarchingRenderPose pose) {
            Assert.NotNullOrDestroyed(pose);

            foreach (var plane in pose.Planes) {
                foreach (var child in plane.Children) {
                    child.gameObject.SetActive(true);
                }
            }
            pose.Activate.SetActive(true);
        }

        /// <summary>
        /// Deactivates all assets for the given pose.
        /// </summary>
        static public void DeactivatePose(OverarchingRenderPose pose) {
            Assert.NotNullOrDestroyed(pose);

            pose.Activate.SetActive(false);
            foreach(var plane in pose.Planes) {
                foreach(var child in plane.Children) {
                    child.gameObject.SetActive(false);
                }
            }
        }
    }
}