using BeauUtil.Debugger;
using UnityEngine;

namespace FieldDay.UI.Widgets {
    public sealed class GuiCounterSegmented : GuiCounter.Style {
        public GameObject[] EnabledObjects;
        public GameObject[] DisabledObjects;

        public override void Populate(in int data, GuiWidgetUpdateFlags flags) {
            Assert.True(data >= 0 && data <= EnabledObjects.Length, "Not enough objects set up for enabled state");
            for(int i = 0; i < EnabledObjects.Length; i++) {
                EnabledObjects[i].SetActive(data > i);
            }

            if (DisabledObjects.Length > 0) {
                Assert.True(data <= DisabledObjects.Length, "Not enough objects set up for disabled state");
                for(int i = 0; i < DisabledObjects.Length; i++) {
                    DisabledObjects[i].SetActive(data <= i);
                }
            }
        }
    }
}