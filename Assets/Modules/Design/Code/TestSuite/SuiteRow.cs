using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace SpaceFab.Design
{
    public class SuiteRow : MonoBehaviour
    {
        public SuiteRunRowButton RunButton;
        public HorizontalLayoutGroup HorizontalLayout;
        [HideInInspector] public SuiteCol[] Cols;
    }
}
