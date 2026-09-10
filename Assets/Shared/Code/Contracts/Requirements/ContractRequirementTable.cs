using System;
using BeauUtil;
using FieldDay;
using FieldDay.Collections;
using FieldDay.UI;
using FieldDay.UI.Widgets;
using SpaceFab.Materials;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace SpaceFab {
    public sealed class ContractRequirementTable : MonoBehaviour {
        public struct PropertyLabelList {
            public unsafe fixed byte Properties[4];
            public int Count;
        }

        public struct MaterialRow {
            public ContractRequirementRow Row;
            public OffsetLengthU16 ColumnRange;
            public PropertyLabelList Properties;
        }
        
        public struct MaterialColumn {
            public StringHash32 MaterialId;
            public StringHash32 DopantN;
            public StringHash32 DopantP;
        }
        
        public ContractRequirementRow[] DefaultRows;
        public ContractRequirementRow ExtendedRow;

        [Header("Layout")]
        public LayoutSizeGroup Sizer;
        public LayoutOptions Spacing = LayoutOptions.Size(4, 1);

        [Header("Assets")]
        public Sprite UnfilledSlotIcon;
        public Sprite FilledSlotIcon;
        public Color32 RowTextDefaultColor;
        public Color32 RowTextFilledColor;
        public Color32 RowBackgroundDefaultColor;
        public Color32 RowBackgroundFilledColor;
        public float SubLabelOffsetY = -6;

        [NonSerialized] public WorkList<MaterialRow> Rows = new WorkList<MaterialRow>(4);
        [NonSerialized] public WorkList<MaterialColumn> Columns = new WorkList<MaterialColumn>(24);
    }

    static public partial class ContractUIUtility {
        static public void InitializeRequirementList(ContractRequirementTable table, ChapterDef chapter, ContractDef contract) {
            
        }
    }
}