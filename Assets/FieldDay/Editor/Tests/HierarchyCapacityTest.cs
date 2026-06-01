using System.IO;
using BeauUtil;
using FieldDay.Data;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace FieldDay.Editor.Tests {
    static public class HierarchyCapacityTest {
        [MenuItem("Field Day/Testing/Hierarchy Capacity Test")]
        static private void Test() {
            GameObject[] rootGOs = SceneManager.GetActiveScene().GetRootGameObjects();
            foreach(var obj in rootGOs) {
                Transform t = obj.transform;
                Debug.LogFormat("Transform '{0}' has hierarchy count {1}/{2}", obj.name, t.hierarchyCount, t.hierarchyCapacity);
            }
        }
    }
}