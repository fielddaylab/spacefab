using BeauRoutine;
using BeauUtil;
using BeauUtil.Debugger;
using EasyAssetStreaming;
using FieldDay;
using FieldDay.Components;
using FieldDay.SharedState;
using ScriptableBake;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace SpaceFab.Overarching
{
    public class OverarchingRenderPlane : MonoBehaviour, IBaked {
        public float Distance;

        [HideInInspector] public Transform[] Children;
        [HideInInspector] public StreamingQuadTexture[] Streamed;

#if UNITY_EDITOR

        int IBaked.Order { get { return 0; } }

        bool IBaked.Bake(BakeFlags flags, BakeContext context) {
            using (var children = Positioning.QueryActiveChildren(transform)) {
                Children = children.ToArray();
            }
            Streamed = GetComponentsInChildren<StreamingQuadTexture>(false);

            foreach(var streamed in Streamed) {
                streamed.enabled = false;
            }

            Baking.FlattenHierarchy(transform, FlattenFlags.DestroyInactive);
            
            foreach(var child in Children) {
                child.gameObject.SetActive(false);
            }

            return true;
        }

#endif // UNITY_EDITOR
    }
}