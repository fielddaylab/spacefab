using BeauPools;
using BeauUtil;
using BeauUtil.UI;
using FieldDay;
using FieldDay.Scenes;
using FieldDay.SharedState;
using SpaceFab.UI;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace SpaceFab.Supply {
    /// <summary>
    /// Holds the shopping list's UI element refs plus the pool of rows it
    /// draws from. One row per material requirement in the current contract;
    /// rebuilt by ShoppingListLoadUtility whenever ShoppingListState is dirty.
    /// Pattern mirrors WikiChipPools: nested SerializablePool subclass +
    /// Active list + Prewarm in IScenePreload.
    /// </summary>
    public class ShoppingListLayoutState : SharedStateComponent, IScenePreload {
        [Serializable] public sealed class ShoppingListRowPool : SerializablePool<ShoppingListRow> { }

        [Header("UI")]
        // Parent the rows are laid out under.
        public RectTransform RowsContainer;

        // Panel rect resized vertically to fit the generated rows.
        public RectTransform PanelRect;

        // confirm button
        public AutoSizedButton ConfirmButton;

        public ShoppingListRowPool Pool;

        // Currently-allocated rows, grown/shrunk on rebuild. The load
        // utility iterates this to know what to free before re-loading.
        [NonSerialized] public List<ShoppingListRow> ActiveRows;

        // Contract the rows were last built for; lets a rebuild detect a
        // contract change without re-reading every requirement each frame.
        [NonSerialized] public StringHash32 LastContractId;

        IEnumerator<WorkSlicer.Result?> IScenePreload.Preload() {
            Pool.Prewarm();
            ActiveRows = new List<ShoppingListRow>(4);

            // ConfirmButton.Button.onClick.AddListener(() => {
            //     Find.Panel<SupplyResultPanel>().Show();
            // });
            
            return null;
        }
    }
}
