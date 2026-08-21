using BeauUtil.Debugger;
using FieldDay;
using FieldDay.Music;
using FieldDay.Scripting;
using FieldDay.Systems;
using FieldDay.UI.Widgets;
using SpaceFab.Design.Visuals;
using SpaceFab.Save;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace SpaceFab.Design
{
    public sealed class SimTableLayout : MonoBehaviour
    {
        public SuiteHeader[] Inputs;
        public SuiteHeader[] Outputs;
        public SuiteRowV2[] Rows;

        [Header("Interactions")]
        public GuiButton TestButton;
    }
}
