using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace SpaceFab.Design
{
    public class SuiteRowV2 : MonoBehaviour {
        [Header("Icons")]
        public Image ResultIcon;

        [Header("Progress Bar")]
        public Graphic LeftProgress;
        public Graphic RightProgress;

        [Header("Inputs and Outputs")]
        public SuiteColV2[] Inputs;
        public SuiteColV2[] Outputs;
    }
}
