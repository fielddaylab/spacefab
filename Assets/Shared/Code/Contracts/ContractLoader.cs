using System.Collections.Generic;
using BeauUtil;
using FieldDay;
using FieldDay.Scenes;
using UnityEngine;

namespace SpaceFab {
    [PreloadOrder(-99)]
    public sealed class ContractLoader : MonoBehaviour, IScenePreload {
        public IEnumerator<WorkSlicer.Result?> Preload() {
            Find.State(out ChapterState chapterState, out ContractState contractState);

            ContractUtility.LoadContractData(contractState, ChapterUtility.SelectedContractId(chapterState));
            while (contractState.LoadRoutine) {
                yield return WorkSlicer.Result.HaltForFrame;
            }
        }
    }
}