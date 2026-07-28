using BeauRoutine;
using BeauUtil;
using BeauUtil.Debugger;
using FieldDay;
using FieldDay.Animation;
using FieldDay.Collections;
using FieldDay.Mathematics;
using FieldDay.SharedState;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using UnityEngine;

namespace SpaceFab.Comic
{
    public class ComicCameraState : SharedStateComponent, IRegistrationCallbacks
    {
        public Camera Camera;
        
        [NonSerialized] public Transform Position;
        [NonSerialized] public Color32 BaseColor;
        [NonSerialized] public Color32 AnimationColor;
        [NonSerialized] public Color32 FinalColor;

        [NonSerialized] public ComicMoveAnimation MoveAnimator;
        [NonSerialized] public AnimHandle CameraTransition;

        void IRegistrationCallbacks.OnDeregister() {
            Game.Animation.CancelAnimation(ref CameraTransition);
        }

        void IRegistrationCallbacks.OnRegister() {
            Camera = Game.Rendering.PrimaryCamera;
            Position = Camera.transform;

            BaseColor = Camera.backgroundColor;
            FinalColor = BaseColor;

            MoveAnimator = new ComicMoveAnimation();
        }

        #region Animations

        public class ComicMoveAnimation : LiteAnimator<ComicCameraState> {
            public ComicCameraPose StartPose;
            public ComicCameraPose EndPose;

            public override void InitAnimation(ComicCameraState target, ref LiteAnimatorState state) {
                StartPose = ComicCameraPose.Extract(target);

                int cameraIndex = state.Registers.A.Int32();
                EndPose = ComicCameraPose.Calculate(ComicsUtility.Manifest.Cameras[cameraIndex]);
            }

            public override void ResetAnimation(ComicCameraState target, ref LiteAnimatorState state) {
            }

            public override void UpdateAnimation(ComicCameraState target, ref LiteAnimatorState state, float deltaTime) {
                float tween = state.Easing.Evaluate(state.PercentProgress);
                ComicCameraPose newPose;
                ComicCameraPose.Interpolate(out newPose, StartPose, EndPose, tween);
                ComicCameraPose.Apply(target, newPose);
            }
        }

        #endregion // Animations
    }

    public struct ComicCameraPose {
        public Vector2 Position;
        public float Rotation;
        public float Height;
        public Color32 BackgroundColor;

        static public ComicCameraPose Extract(ComicCameraState camState) {
            ComicCameraPose pose;
            pose.Position = camState.Position.localPosition;
            pose.Rotation = camState.Position.localEulerAngles.z;
            pose.Height = camState.Camera.orthographicSize * 2;
            pose.BackgroundColor = camState.BaseColor;
            return pose;
        }

        static public ComicCameraPose Calculate(in CameraData packedData) {
            ComicCameraPose pose;
            pose.Position = ComicsUtility.UnpackPoint(packedData.Position);
            pose.Rotation = ComicsUtility.UnpackDegrees(packedData.PackedRotation);
            pose.Height = FixedPoint.Q11_4.ToFloat(packedData.PackedClipHeight);
            pose.BackgroundColor = ComicsUtility.UnpackColor565(packedData.PackedBackgroundColor);
            return pose;
        }

        static public void Interpolate(out ComicCameraPose result, in ComicCameraPose start, in ComicCameraPose end, float t) {
            result.Position = Vector2.LerpUnclamped(start.Position, end.Position, t);
            result.Rotation = t * MathUtils.DegreeAngleDifference(start.Rotation, end.Rotation) + start.Rotation;
            result.Height = Mathf.LerpUnclamped(start.Height, end.Height, t);
            result.BackgroundColor = Color.LerpUnclamped(start.BackgroundColor, end.BackgroundColor, t);
        }

        static public void Apply(ComicCameraState camState, ComicCameraPose pose) {
            Vector3 newPos = pose.Position;
            newPos.z = camState.Position.localPosition.z;
            Quaternion rotation = Quaternion.Euler(0, 0, pose.Rotation);
            camState.Position.SetLocalPositionAndRotation(newPos, rotation);
            camState.BaseColor = pose.BackgroundColor;
            camState.Camera.orthographicSize = pose.Height / 2;
        }
    }

    static public partial class ComicsUtility {
        static public void SnapCamera(int cameraIndex) {
            Find.State(out ComicCameraState camState);
            ComicSequenceManifest manifest = Manifest;
            ComicCameraPose newPose = ComicCameraPose.Calculate(manifest.Cameras[cameraIndex]);
            Game.Animation.CancelAnimation(ref camState.CameraTransition);
            ComicCameraPose.Apply(camState, newPose);
        }

        static public void SnapCamera(StringHash32 cameraId) {
            SnapCamera(FindCameraWithId(cameraId));
        }

        static public AnimHandle PanCamera(int cameraIndex, float duration, Curve easing) {
            if (duration <= 0) {
                SnapCamera(cameraIndex);
                return default;
            }

            Find.State(out ComicCameraState camState);
            Game.Animation.CancelAnimation(ref camState.CameraTransition);

            LiteAnimatorState animParams = new LiteAnimatorState();
            animParams.ResetTime(duration);
            animParams.Easing = easing;
            animParams.Registers.A.Int32() = cameraIndex;
            camState.CameraTransition = Game.Animation.AddLiteAnimator(camState.MoveAnimator, camState, animParams, GameLoopPhase.Update);
            return camState.CameraTransition;
        }

        static public AnimHandle PanCamera(StringHash32 cameraId, float duration, Curve easing) {
            return PanCamera(FindCameraWithId(cameraId), duration, easing);
        }

        static private int FindCameraWithId(StringHash32 cameraId) {
            Assert.False(cameraId.IsEmpty, "Cannot retrieve camera with empty name");
            ComicDisplayState displayState = Find.State<ComicDisplayState>();
            ComicSequenceManifest manifest = Manifest;
            if (displayState.CurrentPageIndex >= 0) {
                var pageRange = manifest.Pages[displayState.CurrentPageIndex].Cameras;
                for(int pageSearch = pageRange.Offset; pageSearch < pageRange.End; pageSearch++) {
                    if (manifest.Cameras[pageSearch].Id == cameraId) {
                        return pageSearch;
                    }
                }
            }

            for(int i = 0, len = manifest.Cameras.Length; i < len; i++) {
                if (manifest.Cameras[i].Id == cameraId) {
                    return i;
                }
            }

            Assert.Fail("No camera with name '{0}' present in comic!", cameraId);
            return -1;
        }
    }
}