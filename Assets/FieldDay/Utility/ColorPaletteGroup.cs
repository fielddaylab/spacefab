using UnityEngine;

namespace FieldDay {
    public sealed class ColorPaletteGroup : MonoBehaviour, IColorPaletteTint {
        public ColorPaletteTargetSet4 TargetSet;

        public void SetTint(ColorPalette2F palette) {
            ColorPalette.Apply(palette, TargetSet);
        }

        public void SetTint(ColorPalette4F palette) {
            ColorPalette.Apply(palette, TargetSet);
        }
    }
}