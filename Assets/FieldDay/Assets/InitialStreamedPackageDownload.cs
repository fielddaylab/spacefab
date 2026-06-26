using BeauUtil;
using FieldDay.Scenes;
using System.Collections;
using UnityEngine;

namespace FieldDay.Assets {
    [DefaultExecutionOrder(ExecutionOrder.Max)]
    public sealed class InitialStreamedPackageDownload : MonoBehaviour {
        [StreamedPackId] public StringHash32 PackId;

        private IEnumerator Start() {
            while (!Game.Assets.IsReadyToStream()) {
                yield return null;
            }
            Game.Assets.LoadStreamedPackage(PackId);
        }
    }
}