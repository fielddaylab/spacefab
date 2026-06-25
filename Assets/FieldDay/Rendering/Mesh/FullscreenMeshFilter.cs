using BeauUtil;
using System;
using System.Runtime.InteropServices;
using UnityEngine;
using UnityEngine.Rendering;
using BeauUtil.Debugger;


#if UNITY_EDITOR
using UnityEditor;
#endif // UNITY_EDITOR

namespace FieldDay.Rendering {
    [RequireComponent(typeof(MeshFilter))]
    [ExecuteAlways]
    public sealed class FullscreenMeshFilter : MonoBehaviour {
        private void OnEnable() {
            GetComponent<MeshFilter>().sharedMesh = FullscreenMesh.CreateMesh();
        }
    }
}