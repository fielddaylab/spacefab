using BeauUtil;
using BeauUtil.Debugger;
using FieldDay.Scenes;
using ScriptableBake;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace FieldDay.World {
    [DisallowMultipleComponent]
    [AddComponentMenu("")]
    internal sealed class CloneList : MonoBehaviour {
        public GameObject[] Clones;
    }
}