using BeauPools;
using BeauUtil;
using FieldDay;
using FieldDay.Assets;
using FieldDay.Components;
using FieldDay.Scenes;
using FieldDay.SharedState;
using SpaceFab.Materials;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace SpaceFab.Supply {
    public sealed class SupplyRoutePools : SharedStateComponent, IScenePreload {
        public SupplyRouteRenderer LinePrefab;
        public SupplyRouteFragmentRenderer FragmentPrefab;

        public PrefabPool<SupplyRouteRenderer> LinePool;
        public PrefabPool<SupplyRouteFragmentRenderer> FragmentPool;

        IEnumerator<WorkSlicer.Result?> IScenePreload.Preload() {
            Transform poolRoot = SceneUtils.GetLocalPool(gameObject.scene);

            LinePool = new PrefabPool<SupplyRouteRenderer>(SupplyRouteData.MaxShips, LinePrefab, poolRoot, null, false, true);
            FragmentPool = new PrefabPool<SupplyRouteFragmentRenderer>(SupplyRouteData.MaxNodes - 1, FragmentPrefab, poolRoot, null, false, true);

            yield return null;

            LinePool.Prewarm();

            yield return null;
            
            FragmentPool.Prewarm();
        }
    }
}