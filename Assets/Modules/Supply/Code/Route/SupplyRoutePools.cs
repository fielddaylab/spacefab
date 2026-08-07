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
        public SupplyRouteDistanceMarker DistanceMarkerPrefab;

        public PrefabPool<SupplyRouteRenderer> LinePool;
        public PrefabPool<SupplyRouteFragmentRenderer> FragmentPool;
        public PrefabPool<SupplyRouteDistanceMarker> MarkerPool;

        IEnumerator<WorkSlicer.Result?> IScenePreload.Preload() {
            Transform poolRoot = SceneUtils.GetLocalPool(gameObject.scene);

            LinePool = new PrefabPool<SupplyRouteRenderer>(SupplyRouteData.MaxShips, LinePrefab, poolRoot, null, false, true);
            FragmentPool = new PrefabPool<SupplyRouteFragmentRenderer>(SupplyRouteData.MaxNodes - 1, FragmentPrefab, poolRoot, null, false, true);
            MarkerPool = new PrefabPool<SupplyRouteDistanceMarker>(64, DistanceMarkerPrefab, poolRoot, null, false, false);

            yield return null;

            LinePool.Prewarm();

            yield return null;
            
            FragmentPool.Prewarm();

            yield return null;

            MarkerPool.Prewarm();
        }
    }
}